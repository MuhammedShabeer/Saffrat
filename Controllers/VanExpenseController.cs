using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Saffrat.Models;
using Saffrat.Models.AccountingEngine;
using Saffrat.Services.AccountingEngine;
using Saffrat.Services;
using System;
using System.Threading.Tasks;

namespace Saffrat.Controllers
{
    [Authorize(Roles = "admin,staff")]
    public class VanExpenseController : BaseController
    {
        private readonly RestaurantDBContext _dbContext;
        private readonly IAccountingEngine _accountingEngine;

        public VanExpenseController(RestaurantDBContext dbContext, IAccountingEngine accountingEngine,
            ILanguageService languageService, ILocalizationService localizationService, IDateTimeService dateTimeService)
            : base(languageService, localizationService, dateTimeService)
        {
            _dbContext = dbContext;
            _accountingEngine = accountingEngine;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SaveExpense(decimal amount, string description)
        {
            try
            {
                if (amount <= 0)
                    return Json(new { status = "error", message = "Amount must be greater than zero." });

                if (string.IsNullOrEmpty(description))
                    return Json(new { status = "error", message = "Description is required." });

                int mainCashAccountId = 18;
                int vanExpenseAccountId = 31;

                var now = CurrentDateTime();
                var journal = new JournalEntry
                {
                    EntryDate = now,
                    ReferenceNumber = "VAN-EXP-" + now.Ticks.ToString().Substring(10),
                    Description = description,
                    SourceDocumentType = "VanExpense",
                    SourceDocumentId = 0,
                    CreatedAt = now
                };

                // Debit Expense (increase expense), Credit Cash (decrease asset)
                journal.LedgerEntries.Add(new LedgerEntry { GLAccountId = vanExpenseAccountId, Description = description, Debit = amount, Credit = 0 });
                journal.LedgerEntries.Add(new LedgerEntry { GLAccountId = mainCashAccountId, Description = description, Debit = 0, Credit = amount });

                await _accountingEngine.PostJournalEntryAsync(journal);

                return Json(new { status = "success", message = "Expense recorded successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }
    }
}
