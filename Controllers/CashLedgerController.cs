using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Saffrat.Models;
using Saffrat.Models.AccountingEngine;
using Saffrat.Services;
using Saffrat.Services.AccountingEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Saffrat.Controllers
{
    [Authorize(Roles = "admin")]
    public class CashLedgerController : BaseController
    {
        private readonly RestaurantDBContext _dbContext;
        private readonly IAccountingEngine _accountingEngine;

        public CashLedgerController(RestaurantDBContext dbContext, IAccountingEngine accountingEngine,
            ILanguageService languageService, ILocalizationService localizationService)
            : base(languageService, localizationService)
        {
            _dbContext = dbContext;
            _accountingEngine = accountingEngine;
        }

        public async Task<IActionResult> Index(DateTime? start, DateTime? end)
        {
            var from = StartOfDay(start);
            var to = EndOfDay(end);

            ViewBag.start = from.ToString("yyyy-MM-dd");
            ViewBag.end = to.ToString("yyyy-MM-dd");

            var entries = await _dbContext.CashLedgers
                .Include(c => c.GLAccount)
                .Where(c => c.EntryDate >= from && c.EntryDate <= to)
                .OrderByDescending(c => c.EntryDate)
                .ToListAsync();

            return View(entries);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Accounts = await _dbContext.GLAccounts
                .Where(a => a.IsActive)
                .ToListAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(CashLedger model)
        {
            var response = new Dictionary<string, string>();
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
                model.CreatedAt = CurrentDateTime();
                model.CreatedBy = userName;

                _dbContext.CashLedgers.Add(model);
                await _dbContext.SaveChangesAsync();

                // 1. Create Journal Entry for Accounting integration
                var journalEntry = new JournalEntry
                {
                    ReferenceNumber = $"CASH-{CurrentDateTime():yyyyMMddHHmmss}",
                    Description = model.Description,
                    EntryDate = model.EntryDate,
                    SourceDocumentType = "CashLedger",
                    SourceDocumentId = model.Id,
                    IsPosted = false,
                    CreatedAt = CurrentDateTime(),
                    LedgerEntries = new List<LedgerEntry>()
                };

                // Get Cash Account
                var cashAccount = await _dbContext.GLAccounts.FirstOrDefaultAsync(a => a.Type == (int)AccountType.CashAndBank);
                if (cashAccount == null)
                {
                    // Create if not exists (fallback)
                    cashAccount = new GLAccount
                    {
                        AccountCode = "1000",
                        AccountName = "Cash",
                        Category = (int)AccountCategory.Asset,
                        Type = (int)AccountType.CashAndBank,
                        IsActive = true
                    };
                    _dbContext.GLAccounts.Add(cashAccount);
                    await _dbContext.SaveChangesAsync();
                }

                if (model.Type == "Income")
                {
                    // Debit Cash, Credit the Offset Account
                    journalEntry.LedgerEntries.Add(new LedgerEntry { Description = model.Description, Debit = model.Amount, Credit = 0, GLAccountId = cashAccount.Id });
                    journalEntry.LedgerEntries.Add(new LedgerEntry { Description = model.Description, Debit = 0, Credit = model.Amount, GLAccountId = (int)model.GLAccountId });
                }
                else // Expense
                {
                    // Debit the Offset Account, Credit Cash
                    journalEntry.LedgerEntries.Add(new LedgerEntry { Description = model.Description, Debit = model.Amount, Credit = 0, GLAccountId = (int)model.GLAccountId });
                    journalEntry.LedgerEntries.Add(new LedgerEntry { Description = model.Description, Debit = 0, Credit = model.Amount, GLAccountId = cashAccount.Id });
                }

                // Post via Accounting Engine
                await _accountingEngine.PostJournalEntryAsync(journalEntry);

                model.JournalEntryId = journalEntry.Id;
                _dbContext.CashLedgers.Update(model);
                await _dbContext.SaveChangesAsync();

                await transaction.CommitAsync();
                response.Add("status", "success");
                response.Add("message", "Transaction recorded successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                response.Add("status", "error");
                response.Add("message", "Error: " + ex.Message);
            }

            return Json(response);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var response = new Dictionary<string, string>();
            var entry = await _dbContext.CashLedgers.FindAsync(id);
            if (entry == null) return Json(new { status = "error", message = "Not found" });

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                if (entry.JournalEntryId != null)
                {
                    var journal = await _dbContext.JournalEntries
                        .Include(j => j.LedgerEntries)
                        .FirstOrDefaultAsync(j => j.Id == entry.JournalEntryId);

                    if (journal != null)
                    {
                        // Reverse balances
                        foreach (var le in journal.LedgerEntries)
                        {
                            var acct = await _dbContext.GLAccounts.FindAsync(le.GLAccountId);
                            if (acct != null)
                            {
                                if (acct.Category == (int)AccountCategory.Asset || acct.Category == (int)AccountCategory.Expense)
                                    acct.CurrentBalance -= (le.Debit - le.Credit);
                                else
                                    acct.CurrentBalance -= (le.Credit - le.Debit);
                            }
                        }
                        _dbContext.JournalEntries.Remove(journal);
                    }
                }

                _dbContext.CashLedgers.Remove(entry);
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                response.Add("status", "success");
                response.Add("message", "Deleted successfully");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                response.Add("status", "error");
                response.Add("message", "Error: " + ex.Message);
            }
            return Json(response);
        }
    }
}
