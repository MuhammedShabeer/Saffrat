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
    public class StockAdjustmentController : BaseController
    {
        private readonly RestaurantDBContext _dbContext;
        private readonly IAccountingEngine _accountingEngine;

        public StockAdjustmentController(RestaurantDBContext dbContext, IAccountingEngine accountingEngine,
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

            var adjustments = await _dbContext.StockAdjustments
                .Include(s => s.IngredientItem)
                .Where(s => s.EntryDate >= from && s.EntryDate <= to)
                .OrderByDescending(s => s.EntryDate)
                .ToListAsync();

            return View(adjustments);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Ingredients = await _dbContext.IngredientItems.ToListAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(StockAdjustment model)
        {
            var response = new Dictionary<string, string>();
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
                model.CreatedAt = CurrentDateTime();
                model.CreatedBy = userName;

                var item = await _dbContext.IngredientItems.FindAsync(model.IngredientItemId);
                if (item == null) throw new Exception("Ingredient item not found.");

                decimal adjustmentAmount = model.Quantity * item.Price; // Use current price for valuation

                // 1. Create Journal Entry
                var journalEntry = new JournalEntry
                {
                    ReferenceNumber = $"STOCK-{CurrentDateTime():yyyyMMddHHmmss}",
                    Description = $"{model.Type}: {item.ItemName} - {model.Reason}",
                    EntryDate = model.EntryDate,
                    SourceDocumentType = "StockAdjustment",
                    IsPosted = false,
                    LedgerEntries = new List<LedgerEntry>()
                };

                // Accounts
                int inventoryAccId = await GetOrCreateGLAccountAsync("Inventory", AccountType.Inventory, AccountCategory.Asset);
                int wastageAccId = await GetOrCreateGLAccountAsync("Stock Wastage / Loss", AccountType.CostOfGoodsSold, AccountCategory.Expense);

                if (model.Type == "Addition")
                {
                    item.Quantity += model.Quantity;
                    // Debit Inventory, Credit Wastage/Adjustment Account (or Gain)
                    journalEntry.LedgerEntries.Add(new LedgerEntry { Description = journalEntry.Description, Debit = adjustmentAmount, Credit = 0, GLAccountId = inventoryAccId });
                    journalEntry.LedgerEntries.Add(new LedgerEntry { Description = journalEntry.Description, Debit = 0, Credit = adjustmentAmount, GLAccountId = wastageAccId });
                }
                else // Subtraction or Wastage
                {
                    item.Quantity -= model.Quantity;
                    // Debit Wastage/Loss, Credit Inventory
                    journalEntry.LedgerEntries.Add(new LedgerEntry { Description = journalEntry.Description, Debit = adjustmentAmount, Credit = 0, GLAccountId = wastageAccId });
                    journalEntry.LedgerEntries.Add(new LedgerEntry { Description = journalEntry.Description, Debit = 0, Credit = adjustmentAmount, GLAccountId = inventoryAccId });
                }

                _dbContext.IngredientItems.Update(item);
                await _accountingEngine.PostJournalEntryAsync(journalEntry);

                model.JournalEntryId = journalEntry.Id;
                _dbContext.StockAdjustments.Add(model);
                await _dbContext.SaveChangesAsync();

                await transaction.CommitAsync();
                response.Add("status", "success");
                response.Add("message", "Stock adjusted successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                response.Add("status", "error");
                response.Add("message", "Error: " + ex.Message);
            }

            return Json(response);
        }

        private async Task<int> GetOrCreateGLAccountAsync(string name, AccountType type, AccountCategory category)
        {
            var acc = await _dbContext.GLAccounts.FirstOrDefaultAsync(a => a.Type == (int)type);
            if (acc == null)
            {
                acc = new GLAccount
                {
                    AccountCode = $"{(int)category + 1}00{DateTime.Now.Millisecond}",
                    AccountName = name,
                    Category = (int)category,
                    Type = (int)type,
                    IsActive = true
                };
                _dbContext.GLAccounts.Add(acc);
                await _dbContext.SaveChangesAsync();
            }
            return acc.Id;
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var response = new Dictionary<string, string>();
            var entry = await _dbContext.StockAdjustments.FindAsync(id);
            if (entry == null) return Json(new { status = "error", message = "Not found" });

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var item = await _dbContext.IngredientItems.FindAsync(entry.IngredientItemId);
                if (item != null)
                {
                    // Reverse quantity change
                    if (entry.Type == "Addition") item.Quantity -= entry.Quantity;
                    else item.Quantity += entry.Quantity;
                    _dbContext.IngredientItems.Update(item);
                }

                if (entry.JournalEntryId != null)
                {
                    var journal = await _dbContext.JournalEntries
                        .Include(j => j.LedgerEntries)
                        .FirstOrDefaultAsync(j => j.Id == entry.JournalEntryId);

                    if (journal != null)
                    {
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

                _dbContext.StockAdjustments.Remove(entry);
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
