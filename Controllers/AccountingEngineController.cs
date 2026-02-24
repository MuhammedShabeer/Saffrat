using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Saffrat.Models;
using Saffrat.Models.AccountingEngine;
using Saffrat.Services;
using Saffrat.Services.AccountingEngine;

namespace Saffrat.Controllers
{
    [Authorize(Roles = "admin")]
    public class AccountingEngineController : BaseController
    {
        private readonly RestaurantDBContext _dbContext;
        private readonly IAccountingEngine _accountingEngine;

        public AccountingEngineController(RestaurantDBContext dbContext, IAccountingEngine accountingEngine,
            ILanguageService languageService, ILocalizationService localizationService)
        : base(languageService, localizationService)
        {
            _dbContext = dbContext;
            _accountingEngine = accountingEngine;
        }

        [HttpGet]
        public async Task<IActionResult> ChartOfAccounts()
        {
            // Fetch directly from DB via DbSet or Set<T>
            var accounts = await _dbContext.Set<GLAccount>().ToListAsync();
            return View(accounts);
        }

        [HttpGet]
        public async Task<IActionResult> JournalEntries()
        {
            var entries = await _dbContext.Set<JournalEntry>()
                .Include(j => j.LedgerEntries)
                .ThenInclude(l => l.GLAccount)
                .OrderByDescending(j => j.EntryDate)
                .ToListAsync();
            return View(entries);
        }

        [HttpGet]
        public async Task<IActionResult> BalanceSheet(DateTime? asOfDate)
        {
            var date = asOfDate ?? DateTime.Today;
            ViewBag.Date = date.ToString("yyyy-MM-dd");
            var report = await _accountingEngine.GenerateBalanceSheetAsync(date);
            return View(report);
        }

        [HttpGet]
        public async Task<IActionResult> ProfitAndLoss(DateTime? start, DateTime? end)
        {
            var startDate = start ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var endDate = end ?? DateTime.Today;
            ViewBag.Start = startDate.ToString("yyyy-MM-dd");
            ViewBag.End = endDate.ToString("yyyy-MM-dd");

            var report = await _accountingEngine.GenerateProfitAndLossAsync(startDate, endDate);
            return View(report);
        }

        [HttpGet]
        public async Task<IActionResult> Invoices()
        {
            var invoices = await _dbContext.Set<Invoice>().ToListAsync();
            return View(invoices);
        }

        [HttpGet]
        public async Task<IActionResult> Bills()
        {
            var bills = await _dbContext.Set<Bill>().ToListAsync();
            return View(bills);
        }

        [HttpPost]
        public async Task<IActionResult> RunDailyClose(DateTime closeDate)
        {
            try
            {
                var entry = await _accountingEngine.PerformDailyCloseAsync(closeDate);
                return Json(new { status = "success", message = "Daily Close Journal generated!" });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }
        // --- DATA ENTRY FLOWS ---

        [HttpGet]
        public IActionResult AddAccount()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddAccount(GLAccount account)
        {
            if (ModelState.IsValid)
            {
                account.IsActive = true;
                account.CurrentBalance = 0;
                _dbContext.Set<GLAccount>().Add(account);
                await _dbContext.SaveChangesAsync();
                return RedirectToAction(nameof(ChartOfAccounts));
            }
            return View(account);
        }

        [HttpGet]
        public async Task<IActionResult> ManualJournal()
        {
            ViewBag.Accounts = await _dbContext.Set<GLAccount>().Where(a => a.IsActive).ToListAsync();
            return View(new JournalEntry { EntryDate = DateTime.Today });
        }

        [HttpPost]
        public async Task<IActionResult> ManualJournal(JournalEntry entry)
        {
            if (ModelState.IsValid)
            {
                entry.SourceDocumentType = "ManualJournal";
                entry.CreatedAt = DateTime.Now;
                entry.IsPosted = false;

                try
                {
                    // Clean up empty lines
                    entry.LedgerEntries = entry.LedgerEntries.Where(l => l.GLAccountId > 0 && (l.Debit > 0 || l.Credit > 0)).ToList();

                    await _accountingEngine.PostJournalEntryAsync(entry);
                    return RedirectToAction(nameof(JournalEntries));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }
            ViewBag.Accounts = await _dbContext.Set<GLAccount>().Where(a => a.IsActive).ToListAsync();
            return View(entry);
        }
    }
}
