using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Saffrat.Helpers;
using Saffrat.Models;
using Saffrat.Models.AccountingEngine;
using Saffrat.Services;
using Saffrat.Services.AccountingEngine;
using System.Security.Claims;

namespace Saffrat.Controllers
{
    public class PurchaseController : BaseController
    {
        private readonly ILogger<PurchaseController> _logger;
        private readonly RestaurantDBContext _dbContext;
        private readonly IAccountingEngine _accountingEngine;


        public PurchaseController(ILogger<PurchaseController> logger, RestaurantDBContext dbContext,
            ILanguageService languageService, ILocalizationService localizationService,
            IAccountingEngine accountingEngine, IDateTimeService dateTimeService)
        : base(languageService, localizationService, dateTimeService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _accountingEngine = accountingEngine;
        }

        /*
         * Purchase Views
         */

        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public IActionResult AddPurchase()
        {
            ViewBag.suppliers = this.GetSuppliers();
            ViewBag.paymentMethods = this.GetPaymentMethods();
            ViewBag.ingredients = this.GetIngredients();
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public IActionResult EditPurchase(int? Id)
        {
            var purchase = _dbContext.Purchases
                .Where(x => x.Id == Id)
                .OrderByDescending(x => x.Id)
                .Include(x => x.Supplier).FirstOrDefault();
            if (purchase != null)
            {
                purchase.PurchaseDetails = _dbContext.PurchaseDetails
                .Where(x => x.PurchaseId == purchase.Id)
                .Include(x => x.IngredientItem).ToList();

                ViewBag.suppliers = this.GetSuppliers();
                ViewBag.paymentMethods = this.GetPaymentMethods();
                ViewBag.ingredients = this.GetIngredients();
                return View(purchase);
            }
            return NotFound();
        }

        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public IActionResult PurchaseHistory(int? supplier, DateTime? start, DateTime? end, string status)
        {
            DateTime from = StartOfDay(start);
            DateTime to = EndOfDay(end);

            ViewBag.start = from.ToString("yyyy-MM-dd");
            ViewBag.end = to.ToString("yyyy-MM-dd");
            ViewBag.status = status;
            ViewBag.supplier = supplier;
            ViewBag.suppliers = this.GetSuppliers();

            var purchases = _dbContext.Purchases
                .Where(x => x.PurchaseDate >= from && x.PurchaseDate <= to)
                .OrderByDescending(x => x.Id)
                .Include(x => x.Supplier).ToList();
            if (supplier != null)
                purchases = purchases.Where(x => x.SupplierId == supplier).ToList();

            if (status == "paid")
                purchases = purchases.Where(x => x.DueAmount == 0).ToList();

            if (status == "unpaid")
                purchases = purchases.Where(x => x.DueAmount > 0).ToList();

            return View(purchases);
        }

        /*
         * Purchase APIs
         */

        [HttpPost]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> AddPurchase(Purchase purchase, int?[] ItemId, decimal?[] ItemQuantity, decimal?[] ItemRate)
        {
            var response = new Dictionary<string, string>();
            using var transaction = _dbContext.Database.BeginTransaction();
            try
            {
                var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (ModelState.IsValid)
                {
                    purchase.UpdatedAt = CurrentDateTime();
                    purchase.UpdatedBy = userName;

                    if (ItemId.Length > 0 && ItemId.Length == ItemQuantity.Length && ItemId.Length == ItemRate.Length)
                    {
                        decimal total = 0;
                        for (int i = 0; i < ItemId.Length; i++)
                        {
                            var item = _dbContext.IngredientItems.FirstOrDefault(x => x.Id == ItemId[i]);
                            if (item != null)
                            {
                                decimal quantity = Convert.ToDecimal(ItemQuantity[i]);
                                decimal rate = Convert.ToDecimal(ItemRate[i]);
                                decimal subtotaltotal = quantity * rate;
                                total += subtotaltotal;
                                item.Quantity += quantity;
                                _dbContext.IngredientItems.Update(item);
                                _dbContext.SaveChanges();
                            }
                        }

                        if (purchase.PaidAmount < 0 || purchase.PaidAmount > total)
                        {
                            response.Add("status", "error");
                            response.Add("message", "Please enter valid amount.");
                        }
                        else
                        {
                            purchase.TotalAmount = total;
                            purchase.DueAmount = total - purchase.PaidAmount;

                            _dbContext.Purchases.Add(purchase);
                            _dbContext.SaveChanges();

                            for (int i = 0; i < ItemId.Length; i++)
                            {
                                decimal quantity = Convert.ToDecimal(ItemQuantity[i]);
                                decimal rate = Convert.ToDecimal(ItemRate[i]);

                                PurchaseDetail purchaseDetail = new();
                                purchaseDetail.PurchaseId = Convert.ToInt32(purchase.Id);
                                purchaseDetail.IngredientItemId = Convert.ToInt32(ItemId[i]);
                                purchaseDetail.PurchasePrice = rate;
                                purchaseDetail.Quantity = quantity;

                                _dbContext.PurchaseDetails.Add(purchaseDetail);
                            }

                            await _dbContext.SaveChangesAsync();
                            await transaction.CommitAsync();

                            // Double-Entry Accounting Engine: Log Purchase
                            int purchasesAccountId = 0;
                            var purchasesAccount = _dbContext.GLAccounts.FirstOrDefault(x => x.AccountName == "Purchases" || x.Category == (int)AccountCategory.Expense);
                            if (purchasesAccount != null)
                            {
                                purchasesAccountId = Convert.ToInt32(purchasesAccount.Id);
                            }
                            else
                            {
                                var newPurAccount = new GLAccount
                                {
                                    AccountCode = "5000",
                                    AccountName = "Purchases",
                                    Category = (int)AccountCategory.Expense, // Expense
                                    Type = (int)AccountType.FoodCost,    // FoodCost (or generic Cost of Goods Sold)
                                    CurrentBalance = 0,
                                    IsActive = true
                                };
                                _dbContext.GLAccounts.Add(newPurAccount);
                                await _dbContext.SaveChangesAsync();
                                purchasesAccountId = newPurAccount.Id;
                            }

                             // Resolve Bank Account from Payment Method
                             int bankAccId = Convert.ToInt32(GetSetting.PurchaseAccount);
                             var pMethod = _dbContext.PaymentMethods.FirstOrDefault(pm => pm.Title == purchase.PaymentType);
                             if (pMethod != null && pMethod.GLAccountId.HasValue)
                             {
                                 bankAccId = pMethod.GLAccountId.Value;
                             }

                             JournalEntry purchaseJournal = new JournalEntry
                             {
                                 ReferenceNumber = "PUR-" + purchase.Id,
                                 Description = "Inventory Purchase " + purchase.InvoiceNo,
                                 EntryDate = CurrentDateTime(),
                                 IsPosted = false,
                                 SourceDocumentType = "purchase",
                                 SourceDocumentId = purchase.Id,
                                 CreatedAt = CurrentDateTime(),
                                 LedgerEntries = new List<LedgerEntry>()
                             };

                             purchaseJournal.LedgerEntries.Add(new LedgerEntry
                             {
                                 GLAccountId = purchasesAccountId,
                                 Description = purchase.Description,
                                 Debit = purchase.TotalAmount,
                                 Credit = 0
                             });

                             purchaseJournal.LedgerEntries.Add(new LedgerEntry
                             {
                                 GLAccountId = bankAccId,
                                 Description = purchase.Description,
                                 Debit = 0,
                                 Credit = purchase.TotalAmount
                             });

                             // Use Accounting Engine to post and update balances
                             await _accountingEngine.PostJournalEntryAsync(purchaseJournal);

                            response.Add("status", "success");
                            response.Add("message", "success");
                        }
                    }
                    else
                    {
                        response.Add("status", "error");
                        response.Add("message", "Please add item in cart.");
                    }
                }
                else
                {
                    response.Add("status", "error");
                    response.Add("message", "Enter required fields.");
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                response.Add("status", "error");
                response.Add("message", "Something went wrong." + ex.Message);
            }
            return Json(response);
        }

        [HttpPut]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> UpdatePurchase(Purchase pur, int?[] ItemId, decimal?[] ItemQuantity, decimal?[] ItemRate)
        {
            var response = new Dictionary<string, string>();
            using var transaction = _dbContext.Database.BeginTransaction();
            try
            {
                var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var purchase = _dbContext.Purchases.FirstOrDefault(x => x.Id == pur.Id);
                if (ModelState.IsValid && purchase != null)
                {

                    if (ItemId.Length > 0 && ItemId.Length == ItemQuantity.Length && ItemId.Length == ItemRate.Length)
                    {
                        decimal total = 0;
                        for (int i = 0; i < ItemId.Length; i++)
                        {
                            var item = _dbContext.IngredientItems.FirstOrDefault(x => x.Id == ItemId[i]);
                            var purchaseDetail = _dbContext.PurchaseDetails.FirstOrDefault(x => x.PurchaseId == purchase.Id && x.IngredientItemId == ItemId[i]);
                            if (item != null && purchaseDetail != null)
                            {
                                decimal quantity = Convert.ToDecimal(ItemQuantity[i]);
                                decimal rate = Convert.ToDecimal(ItemRate[i]);
                                decimal subtotaltotal = quantity * rate;
                                total += subtotaltotal;
                                item.Quantity -= purchaseDetail.Quantity;
                                item.Quantity += quantity;
                                _dbContext.IngredientItems.Update(item);

                                purchaseDetail.PurchasePrice = rate;
                                purchaseDetail.Quantity = quantity;

                                _dbContext.PurchaseDetails.Update(purchaseDetail);
                            }
                        }
                        if (pur.PaidAmount < 0 || pur.PaidAmount > total)
                        {
                            response.Add("status", "error");
                            response.Add("message", "Please enter valid amount.");
                        }
                        else
                        {
                            purchase.TotalAmount = total;
                            purchase.DueAmount = purchase.TotalAmount - pur.PaidAmount;
                            purchase.PaidAmount = pur.PaidAmount;
                            purchase.InvoiceNo = pur.InvoiceNo;
                            purchase.SupplierId = pur.SupplierId;
                            purchase.Description = pur.Description;
                            purchase.PurchaseDate = pur.PurchaseDate;


                            _dbContext.Purchases.Update(purchase);
                            await _dbContext.SaveChangesAsync();
                            await transaction.CommitAsync();

                            // Double-Entry Accounting Engine: Log Update Purchase
                            int purchasesAccountId = 0;
                            var purchasesAccount = _dbContext.GLAccounts.FirstOrDefault(x => x.AccountName == "Purchases" || x.Category == (int)AccountCategory.Expense);
                            if (purchasesAccount != null)
                            {
                                purchasesAccountId = Convert.ToInt32(purchasesAccount.Id);
                            }
                            else
                            {
                                var newPurAccount = new GLAccount
                                {
                                    AccountCode = "5000",
                                    AccountName = "Purchases",
                                    Category = (int)AccountCategory.Expense, // Expense
                                    Type = (int)AccountType.FoodCost,    // FoodCost
                                    CurrentBalance = 0,
                                    IsActive = true
                                };
                                _dbContext.GLAccounts.Add(newPurAccount);
                                await _dbContext.SaveChangesAsync();
                                purchasesAccountId = newPurAccount.Id;
                            }

                            // Clean up existing journal entries and update balances
                            var oldJournals = _dbContext.JournalEntries.Where(x => x.SourceDocumentType == "purchase" && x.SourceDocumentId == purchase.Id).ToList();
                            foreach (var oldJournal in oldJournals)
                            {
                                await _accountingEngine.ReverseJournalEntryAsync(oldJournal.Id);
                            }

                            JournalEntry purchaseJournal = new JournalEntry
                            {
                                ReferenceNumber = "PUR-" + purchase.Id,
                                Description = "Inventory Purchase " + purchase.InvoiceNo,
                                EntryDate = CurrentDateTime(),
                                IsPosted = false,
                                SourceDocumentType = "purchase",
                                SourceDocumentId = purchase.Id,
                                CreatedAt = CurrentDateTime(),
                                LedgerEntries = new List<LedgerEntry>()
                            };

                            purchaseJournal.LedgerEntries.Add(new LedgerEntry
                            {
                                GLAccountId = purchasesAccountId,
                                Description = purchase.Description,
                                Debit = purchase.TotalAmount,
                                Credit = 0
                            });

                            purchaseJournal.LedgerEntries.Add(new LedgerEntry
                            {
                                GLAccountId = Convert.ToInt32(GetSetting.PurchaseAccount),
                                Description = purchase.Description,
                                Debit = 0,
                                Credit = purchase.TotalAmount
                            });

                            await _accountingEngine.PostJournalEntryAsync(purchaseJournal);

                            response.Add("status", "success");
                            response.Add("message", "success");
                        }
                    }
                    else
                    {
                        response.Add("status", "error");
                        response.Add("message", "Please add items in cart.");
                    }
                }
                else
                {
                    response.Add("status", "error");
                    response.Add("message", "Enter required fields.");
                }
            }
            catch
            {
                await transaction.RollbackAsync();
                response.Add("status", "error");
                response.Add("message", "Something went wrong.");
            }
            return Json(response);
        }

        [HttpDelete]
        [Authorize(Roles = "admin,staff")]
        public async Task<JsonResult> DeletePurchase(int? Id)
        {
            var response = new Dictionary<string, string>();
            var existing = await _dbContext.Purchases.FindAsync(Id);
            if (existing != null)
            {
                using var transaction = _dbContext.Database.BeginTransaction();
                try
                {
                    var purchaseDetails = _dbContext.PurchaseDetails
                        .Where(x => x.PurchaseId == existing.Id)
                        .Include(x => x.IngredientItem).ToList();
                    foreach (var item in purchaseDetails)
                    {
                        var ingredientItem = item.IngredientItem;
                        ingredientItem.Quantity -= item.Quantity;
                        _dbContext.IngredientItems.Update(ingredientItem);
                    }

                    var existingJournals = _dbContext.JournalEntries.Where(x => x.SourceDocumentType == "purchase" && x.SourceDocumentId == existing.Id).ToList();
                    foreach (var journal in existingJournals)
                    {
                        await _accountingEngine.ReverseJournalEntryAsync(journal.Id);
                    }

                    _dbContext.Purchases.Remove(existing);
                    await _dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();

                    response.Add("status", "success");
                    response.Add("message", "success");
                }
                catch
                {
                    await transaction.RollbackAsync();
                    response.Add("status", "error");
                    response.Add("message", "Something went wrong.");
                }
            }
            else
            {
                response.Add("status", "error");
                response.Add("message", "Purchase not exist.");
            }

            return Json(response);
        }

        [HttpPut]
        [Authorize(Roles = "admin,staff")]
        public async Task<JsonResult> PayDue(int? Id)
        {
            var response = new Dictionary<string, string>();
            var existing = await _dbContext.Purchases.FindAsync(Id);
            if (existing != null)
            {
                try
                {
                    existing.PaidAmount = existing.TotalAmount;
                    existing.DueAmount = 0;
                    _dbContext.Purchases.Update(existing);
                    await _dbContext.SaveChangesAsync();

                    response.Add("status", "success");
                    response.Add("message", "success");
                }
                catch
                {
                    response.Add("status", "error");
                    response.Add("message", "Something went wrong.");
                }
            }
            else
            {
                response.Add("status", "error");
                response.Add("message", "Purchase not exist.");
            }

            return Json(response);
        }

        /*
         * Class Private Functions
         */
        private Dictionary<int, string> GetIngredients()
        {
            Dictionary<int, string> suppliers = _dbContext.IngredientItems
                .Select(t => new
                {
                    t.Id,
                    t.ItemName
                }).ToDictionary(t => Convert.ToInt32(t.Id), t => t.ItemName);
            return suppliers;
        }

        private Dictionary<int, string> GetSuppliers()
        {
            Dictionary<int, string> suppliers = _dbContext.Suppliers
                .Select(t => new
                {
                    t.Id,
                    t.SupplierName
                }).ToDictionary(t => Convert.ToInt32(t.Id), t => t.SupplierName);
            return suppliers;
        }

        private Dictionary<int, string> GetPaymentMethods()
        {
            Dictionary<int, string> methods = _dbContext.PaymentMethods
                .Select(t => new
                {
                    t.Id,
                    t.Title
                }).ToDictionary(t => Convert.ToInt32(t.Id), t => t.Title);
            return methods;
        }
    }
}