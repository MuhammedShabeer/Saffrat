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
    public class PartnerDashboardController : BaseController
    {
        private readonly RestaurantDBContext _dbContext;
        private readonly IAccountingEngine _accountingEngine;

        public PartnerDashboardController(RestaurantDBContext dbContext, IAccountingEngine accountingEngine,
            ILanguageService languageService, ILocalizationService localizationService)
            : base(languageService, localizationService)
        {
            _dbContext = dbContext;
            _accountingEngine = accountingEngine;
        }

        public async Task<IActionResult> Index()
        {
            var partners = await _dbContext.Partners
                .Include(p => p.GLAccount)
                .Include(p => p.PartnerTransactions)
                .ToListAsync();

            ViewBag.TotalEquity = partners.Sum(p => p.GLAccount?.CurrentBalance ?? 0);

            // For the dashboard charts
            ViewBag.PartnerNames = partners.Select(p => p.Name).ToList();
            ViewBag.PartnerBalances = partners.Select(p => p.GLAccount?.CurrentBalance ?? 0).ToList();

            return View(partners);
        }

        [HttpGet]
        public IActionResult AddPartner() => View();

        [HttpPost]
        public async Task<IActionResult> AddPartner(Partner partner)
        {
            var response = new Dictionary<string, string>();
            try
            {
                var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
                partner.CreatedAt = CurrentDateTime();
                partner.CreatedBy = userName;

                // Create GL Account for Partner
                var glAccount = new GLAccount
                {
                    AccountCode = $"300{partner.Id + 1}{DateTime.Now.Millisecond % 100}",
                    AccountName = $"Equity: {partner.Name}",
                    Category = (int)AccountCategory.Equity,
                    Type = (int)AccountType.OwnerEquity,
                    IsActive = true,
                    CurrentBalance = 0
                };
                _dbContext.GLAccounts.Add(glAccount);
                await _dbContext.SaveChangesAsync();

                partner.GLAccountId = glAccount.Id;
                _dbContext.Partners.Add(partner);
                await _dbContext.SaveChangesAsync();

                response.Add("status", "success");
                response.Add("message", "Partner added successfully.");
            }
            catch (Exception ex)
            {
                response.Add("status", "error");
                response.Add("message", "Error: " + ex.Message);
            }
            return Json(response);
        }

        [HttpGet]
        public async Task<IActionResult> ManageTransactions(int id)
        {
            var partner = await _dbContext.Partners
                .Include(p => p.PartnerTransactions)
                .Include(p => p.GLAccount)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (partner == null) return NotFound();
            return View(partner);
        }

        [HttpPost]
        public async Task<IActionResult> AddTransaction(PartnerTransaction trans)
        {
            var response = new Dictionary<string, string>();
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
                trans.CreatedAt = CurrentDateTime();
                trans.CreatedBy = userName;

                var partner = await _dbContext.Partners.FindAsync(trans.PartnerId);
                if (partner == null) throw new Exception("Partner not found.");

                _dbContext.PartnerTransactions.Add(trans);
                await _dbContext.SaveChangesAsync();

                // 1. Create Journal Entry
                var journalEntry = new JournalEntry
                {
                    ReferenceNumber = $"PART-{CurrentDateTime():yyyyMMddHHmmss}",
                    Description = $"{trans.Type} for {partner.Name}: {trans.Note}",
                    EntryDate = trans.EntryDate,
                    SourceDocumentType = "PartnerTransaction",
                    SourceDocumentId = trans.Id,
                    IsPosted = false,
                    CreatedAt = CurrentDateTime(),
                    LedgerEntries = new List<LedgerEntry>()
                };

                var cashAccount = await _dbContext.GLAccounts.FirstOrDefaultAsync(a => a.Type == (int)AccountType.CashAndBank);
                if (cashAccount == null) throw new Exception("Cash account not found.");

                if (trans.Type == "Investment")
                {
                    // Debit Cash, Credit Partner Equity
                    journalEntry.LedgerEntries.Add(new LedgerEntry { Description = journalEntry.Description, Debit = trans.Amount, Credit = 0, GLAccountId = cashAccount.Id });
                    journalEntry.LedgerEntries.Add(new LedgerEntry { Description = journalEntry.Description, Debit = 0, Credit = trans.Amount, GLAccountId = (int)partner.GLAccountId });
                }
                else // Withdrawal or ProfitDistribution
                {
                    // Debit Partner Equity, Credit Cash
                    journalEntry.LedgerEntries.Add(new LedgerEntry { Description = journalEntry.Description, Debit = trans.Amount, Credit = 0, GLAccountId = (int)partner.GLAccountId });
                    journalEntry.LedgerEntries.Add(new LedgerEntry { Description = journalEntry.Description, Debit = 0, Credit = trans.Amount, GLAccountId = cashAccount.Id });
                }

                await _accountingEngine.PostJournalEntryAsync(journalEntry);

                trans.JournalEntryId = journalEntry.Id;
                _dbContext.PartnerTransactions.Update(trans);
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
    }
}
