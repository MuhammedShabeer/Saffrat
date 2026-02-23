using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Saffrat.Models;
using Saffrat.Services;
using System.Security.Claims;

namespace Saffrat.Controllers
{
    public class AccountingController : BaseController
    {
        private readonly ILogger<AccountingController> _logger;
        private readonly RestaurantDBContext _dbContext;
        private readonly ITransactionService _transactionService;
        private readonly AppSetting setting;

        public AccountingController(ILogger<AccountingController> logger, RestaurantDBContext dbContext, ITransactionService transactionService,
            ILanguageService languageService, ILocalizationService localizationService)
        : base(languageService, localizationService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _transactionService = transactionService;

            setting = _dbContext.AppSettings.FirstOrDefault(x => x.Id == 1);
        }

        /*
         * Accounts Views
         */

        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Accounts()
        {
            var accounts = await _dbContext.Accounts.ToListAsync();
            return View(accounts);
        }

        /*
         * Accounts APIs
         */

        [HttpGet]
        [Authorize(Roles = "admin")]
        public JsonResult AccountDetail(int? Id)
        {
            try
            {
                var row = _dbContext.Accounts.FirstOrDefault(x => x.Id == Id);
                if (row != null)
                {
                    return Json(new
                    {
                        data = row,
                        status = "success",
                        message = "success"
                    });
                }
                else
                {
                    return Json(new
                    {
                        status = "error",
                        message = "Account not exist."
                    });
                }
            }
            catch
            {
                return Json(new
                {
                    status = "error",
                    message = "Something went wrong."
                });
            }
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> SaveAccount(Account account)
        {
            var response = new Dictionary<string, string>();
            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (ModelState.IsValid)
            {
                using var transaction = _dbContext.Database.BeginTransaction();
                try
                {
                    account.UpdatedAt = CurrentDateTime();
                    account.UpdatedBy = userName;
                    if (account.Id > 0)
                    {
                        var acc = _dbContext.Accounts.FirstOrDefault(x => x.Id == account.Id);
                        if (acc != null)
                        {
                            acc.AccountName = account.AccountName;
                            acc.AccountNumber = account.AccountNumber;
                            acc.Note = account.Note;
                            acc.AccountGroup = account.AccountGroup;
                            acc.AccountType = account.AccountType;
                            acc.ParentAccountId = account.ParentAccountId;
                        }
                        _dbContext.Accounts.Update(acc);
                        _dbContext.SaveChanges();
                        await transaction.CommitAsync();
                    }
                    else
                    {
                        account.Credit = account.Balance;
                        account.Debit = 0;
                        _dbContext.Accounts.Add(account);
                        _dbContext.SaveChanges();

                        if (account.Balance > 0)
                        {
                            Deposit deposit = new()
                            {
                                AccountId = Convert.ToInt32(account.Id),
                                Note = "Initial Deposit",
                                Amount = account.Balance,
                                DepositDate = CurrentDateTime(),
                                UpdatedAt = CurrentDateTime(),
                                UpdatedBy = userName
                            };

                            _dbContext.Deposits.Add(deposit);
                            await _dbContext.SaveChangesAsync();

                            // Find or Create Capital Account for Opening Balance Double Entry
                            var capitalAccount = _dbContext.Accounts.FirstOrDefault(x => x.AccountName == "Capital Account" && (x.AccountGroup == "Equity" || x.AccountGroup == "Owner Equity"));
                            int capitalAccountId = 0;
                            if (capitalAccount != null)
                            {
                                capitalAccountId = Convert.ToInt32(capitalAccount.Id);
                            }
                            else
                            {
                                var newCapAccount = new Account
                                {
                                    AccountName = "Capital Account",
                                    AccountNumber = "3000",
                                    AccountGroup = "Equity",
                                    AccountType = "Equity",
                                    Credit = 0,
                                    Debit = 0,
                                    Balance = 0,
                                    UpdatedAt = CurrentDateTime()
                                };
                                _dbContext.Accounts.Add(newCapAccount);
                                _dbContext.SaveChanges();
                                capitalAccountId = Convert.ToInt32(newCapAccount.Id);
                            }

                            Transaction creditEquityTrans = new()
                            {
                                AccountId = capitalAccountId,
                                TransactionReference = "deposit-" + deposit.Id,
                                TransactionType = "deposit",
                                Description = "Opening Balance",
                                Credit = account.Balance,
                                Debit = 0,
                                Amount = account.Balance,
                                Date = CurrentDateTime(),
                            };

                            Transaction debitAssetTrans = new()
                            {
                                AccountId = Convert.ToInt32(account.Id),
                                TransactionReference = "deposit-" + deposit.Id,
                                TransactionType = "deposit",
                                Description = "Opening Balance",
                                Credit = 0,
                                Debit = account.Balance,
                                Amount = account.Balance,
                                Date = CurrentDateTime(),
                            };

                            _ = await _transactionService.AddDoubleEntryTransaction(debitAssetTrans, creditEquityTrans);
                        }

                        await transaction.CommitAsync();
                    }

                    response.Add("status", "success");
                    response.Add("message", "success");
                }
                catch
                {
                    await transaction.RollbackAsync();
                    response.Add("status", "error");
                    response.Add("message", "Error while saving account.");
                }
            }
            else
            {
                response.Add("status", "error");
                response.Add("message", "Enter required fields.");
            }
            return Json(response);
        }

        [HttpDelete]
        [Authorize(Roles = "admin")]
        public async Task<JsonResult> DeleteAccount(int? Id)
        {
            var results = new Dictionary<string, string>();
            var existing = await _dbContext.Accounts.FindAsync(Id);

            if (existing != null)
            {
                try
                {
                    if (existing.Id == setting.SaleAccount)
                    {
                        results.Add("status", "error");
                        results.Add("message", "You can't delete default sale account.");
                    }
                    else if (existing.Id == setting.PurchaseAccount)
                    {
                        results.Add("status", "error");
                        results.Add("message", "You can't delete default purchase account.");
                    }
                    else if (existing.Id == setting.PayrollAccount)
                    {
                        results.Add("status", "error");
                        results.Add("message", "You can't delete default payroll account.");
                    }
                    else
                    {
                        _dbContext.Accounts.Remove(existing);
                        await _dbContext.SaveChangesAsync();

                        results.Add("status", "success");
                        results.Add("message", "success");
                    }
                }
                catch
                {
                    results.Add("status", "error");
                    results.Add("message", "Error while deleting account.");
                }
            }
            else
            {
                results.Add("status", "error");
                results.Add("message", "Account not exist.");
            }

            return Json(results);
        }


        /*
         * Transactions
         */

        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Transactions(int? account, DateTime? start, DateTime? end)
        {
            DateTime from = StartOfDay(start);
            DateTime to = EndOfDay(end);

            ViewBag.account = account;
            ViewBag.start = from.ToString("yyyy-MM-dd");
            ViewBag.end = to.ToString("yyyy-MM-dd"); ;
            ViewBag.balance = 0;
            ViewBag.accounts = await _dbContext.Accounts.ToListAsync();

            var transactions = new List<Transaction>();

            if (account > 0)
            {
                transactions = await _dbContext.Transactions
                .Where(x => x.Date >= from && x.Date <= to && x.AccountId == account)
                .Include(x => x.Account)
                .ToListAsync();

                decimal balance = _dbContext.Transactions.Where(x => x.Date < from && x.AccountId == account).Sum(x => x.Credit - x.Debit);
                ViewBag.balance = balance;
            }

            return View(transactions);
        }

        /*
         * Expense Views
         */

        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Expenses(int? account, DateTime? start, DateTime? end)
        {
            DateTime from = StartOfDay(start);
            DateTime to = EndOfDay(end);

            ViewBag.start = from.ToString("yyyy-MM-dd");
            ViewBag.end = to.ToString("yyyy-MM-dd"); ;

            var expenses = await _dbContext.Expenses
                .Where(e => e.ExpenseDate >= from && e.ExpenseDate <= to)
                .Include(x => x.Account)
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            if (account > 0)
                expenses = expenses.Where(e => e.AccountId == account).ToList();

            ViewBag.accounts = GetAccounts();
            ViewBag.account = account;

            return View(expenses);
        }

        /*
         * Expense APIs
         */
        [HttpGet]
        [Authorize(Roles = "admin")]
        public JsonResult ExpenseDetail(int? Id)
        {
            try
            {
                var row = _dbContext.Expenses.FirstOrDefault(x => x.Id == Id);
                if (row != null)
                {
                    return Json(new
                    {
                        data = row,
                        status = "success",
                        message = "success"
                    });
                }
                else
                {
                    return Json(new
                    {
                        status = "error",
                        message = "Account not exist."
                    });
                }
            }
            catch
            {
                return Json(new
                {
                    status = "error",
                    message = "Something went wrong."
                });
            }
        }
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> SaveExpense(Expense expense)
        {
            var response = new Dictionary<string, string>();
            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (ModelState.IsValid)
            {
                try
                {
                    expense.UpdatedBy = userName;
                    expense.UpdatedAt = CurrentDateTime();
                    var expenseAccountName = string.IsNullOrEmpty(expense.Category) ? "Direct Expenses" : expense.Category;
                    var expenseAccount = _dbContext.Accounts.FirstOrDefault(x => x.AccountName == expenseAccountName && (x.AccountGroup == "Expenses" || x.AccountGroup == "Expense"));
                    int expenseAccountId = 0;
                    if (expenseAccount != null)
                    {
                        expenseAccountId = Convert.ToInt32(expenseAccount.Id);
                    }
                    else
                    {
                        var newExpAccount = new Account
                        {
                            AccountName = expenseAccountName,
                            AccountNumber = "5000",
                            AccountGroup = "Expenses",
                            AccountType = "Expense",
                            Credit = 0,
                            Debit = 0,
                            Balance = 0,
                            UpdatedAt = CurrentDateTime()
                        };
                        _dbContext.Accounts.Add(newExpAccount);
                        _dbContext.SaveChangesAsync().Wait();
                        expenseAccountId = Convert.ToInt32(newExpAccount.Id);
                    }

                    if (expense.Id > 0)
                    {
                        _dbContext.Expenses.Update(expense);
                        await _dbContext.SaveChangesAsync();

                        // Remove old transactions then insert the new double entry
                        await _transactionService.DeleteTransactionsByReference("expense-" + expense.Id);

                        Transaction debitExpenseTrans = new()
                        {
                            AccountId = expenseAccountId,
                            TransactionReference = "expense-" + expense.Id,
                            TransactionType = "expense",
                            Description = expense.Note,
                            Credit = 0,
                            Debit = expense.Amount,
                            Amount = expense.Amount,
                            Date = expense.ExpenseDate,
                        };

                        Transaction creditAssetTrans = new()
                        {
                            AccountId = expense.AccountId,
                            TransactionReference = "expense-" + expense.Id,
                            TransactionType = "expense",
                            Description = "Payment for " + expense.Note,
                            Credit = expense.Amount,
                            Debit = 0,
                            Amount = expense.Amount,
                            Date = expense.ExpenseDate,
                        };

                        _ = await _transactionService.AddDoubleEntryTransaction(debitExpenseTrans, creditAssetTrans);
                    }
                    else
                    {
                        _dbContext.Expenses.Add(expense);
                        await _dbContext.SaveChangesAsync();

                        Transaction debitExpenseTrans = new()
                        {
                            AccountId = expenseAccountId,
                            TransactionReference = "expense-" + expense.Id,
                            TransactionType = "expense",
                            Description = expense.Note,
                            Credit = 0,
                            Debit = expense.Amount,
                            Amount = expense.Amount,
                            Date = expense.ExpenseDate,
                        };

                        Transaction creditAssetTrans = new()
                        {
                            AccountId = expense.AccountId,
                            TransactionReference = "expense-" + expense.Id,
                            TransactionType = "expense",
                            Description = "Payment for " + expense.Note,
                            Credit = expense.Amount,
                            Debit = 0,
                            Amount = expense.Amount,
                            Date = expense.ExpenseDate,
                        };

                        _ = await _transactionService.AddDoubleEntryTransaction(debitExpenseTrans, creditAssetTrans);
                    }

                    response.Add("status", "success");
                    response.Add("message", "success");
                }
                catch
                {
                    response.Add("status", "error");
                    response.Add("message", "Error while saving expense.");
                }
            }
            else
            {
                response.Add("status", "error");
                response.Add("message", "Enter required fields." + expense.Amount);
            }
            return Json(response);
        }

        [HttpDelete]
        [Authorize(Roles = "admin")]
        public async Task<JsonResult> DeleteExpense(int? Id)
        {
            var results = new Dictionary<string, string>();

            var existing = _dbContext.Expenses.FirstOrDefault(x => x.Id == Id);

            if (existing != null)
            {
                try
                {
                    _dbContext.Expenses.Remove(existing);
                    await _dbContext.SaveChangesAsync();

                    _ = await _transactionService.DeleteTransactionsByReference("expense-" + existing.Id);

                    results.Add("status", "success");
                    results.Add("message", "success");

                }
                catch
                {
                    results.Add("status", "error");
                    results.Add("message", "Error while deleting expense.");
                }
            }
            else
            {
                results.Add("status", "error");
                results.Add("message", "Expense not exist.");
            }

            return Json(results);
        }

        /*
         * Transfer Views
         */

        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Transfers(DateTime? start, DateTime? end)
        {
            DateTime from = StartOfDay(start);
            DateTime to = EndOfDay(end);

            ViewBag.start = from.ToString("yyyy-MM-dd");
            ViewBag.end = to.ToString("yyyy-MM-dd"); ;

            var transfers = await _dbContext.AccountMoneyTransfers
                .Where(e => e.TransferDate >= from && e.TransferDate <= to)
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            ViewBag.accounts = GetAccounts();

            return View(transfers);
        }

        /*
         * Transfer APIs
         */
        [HttpGet]
        [Authorize(Roles = "admin")]
        public JsonResult TransferDetail(int? Id)
        {
            try
            {
                var row = _dbContext.AccountMoneyTransfers.FirstOrDefault(x => x.Id == Id);
                if (row != null)
                {
                    return Json(new
                    {
                        data = row,
                        status = "success",
                        message = "success"
                    });
                }
                else
                {
                    return Json(new
                    {
                        status = "error",
                        message = "Account not exist."
                    });
                }
            }
            catch
            {
                return Json(new
                {
                    status = "error",
                    message = "Something went wrong."
                });
            }
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> SaveTransfer(AccountMoneyTransfer transfer)
        {
            var response = new Dictionary<string, string>();
            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (ModelState.IsValid)
            {
                var fromAccount = _dbContext.Accounts.FirstOrDefault(x => x.AccountName == transfer.FromAccount);
                var toAccount = _dbContext.Accounts.FirstOrDefault(x => x.AccountName == transfer.ToAccount);
                if (fromAccount == null)
                {
                    response.Add("status", "error");
                    response.Add("message", "Please select from account.");
                }
                else if (toAccount == null)
                {
                    response.Add("status", "error");
                    response.Add("message", "Please select to account.");
                }
                else if (fromAccount == toAccount)
                {
                    response.Add("message", "Please select different accounts.");
                }
                else
                {
                    try
                    {
                        transfer.UpdatedBy = userName;
                        transfer.UpdatedAt = CurrentDateTime();
                        if (transfer.Id > 0)
                        {
                            _dbContext.AccountMoneyTransfers.Update(transfer);
                            await _dbContext.SaveChangesAsync();

                            var statements = _dbContext.Transactions.Where(x => x.TransactionReference == "transfer-" + transfer.Id).ToList();

                            foreach (var item in statements)
                            {
                                Transaction statement = new();
                                if (item.Credit > 0)
                                {
                                    statement.Id = item.Id;
                                    statement.AccountId = Convert.ToInt32(toAccount.Id);
                                    statement.TransactionReference = "transfer-" + transfer.Id;
                                    statement.TransactionType = "transfer";
                                    statement.Description = transfer.Note;
                                    statement.Credit = transfer.Amount;
                                    statement.Debit = 0;
                                    statement.Amount = transfer.Amount;
                                    statement.Date = transfer.TransferDate;
                                }
                                else
                                {
                                    statement.Id = item.Id;
                                    statement.AccountId = Convert.ToInt32(fromAccount.Id);
                                    statement.TransactionReference = "transfer-" + transfer.Id;
                                    statement.TransactionType = "transfer";
                                    statement.Description = transfer.Note;
                                    statement.Credit = 0;
                                    statement.Debit = transfer.Amount;
                                    statement.Amount = transfer.Amount;
                                    statement.Date = transfer.TransferDate;
                                }
                                _ = await _transactionService.UpdateTransaction(statement, statement.Id);
                            }
                        }
                        else
                        {
                            _dbContext.AccountMoneyTransfers.Add(transfer);
                            await _dbContext.SaveChangesAsync();

                            Transaction debitAssetTrans = new()
                            {
                                AccountId = Convert.ToInt32(fromAccount.Id),
                                TransactionReference = "transfer-" + transfer.Id,
                                TransactionType = "transfer",
                                Description = "Transfer to " + toAccount.AccountName + ": " + transfer.Note,
                                Credit = 0,
                                Debit = transfer.Amount,
                                Amount = transfer.Amount,
                                Date = transfer.TransferDate,
                            };

                            Transaction creditAssetTrans = new()
                            {
                                AccountId = Convert.ToInt32(toAccount.Id),
                                TransactionReference = "transfer-" + transfer.Id,
                                TransactionType = "transfer",
                                Description = "Transfer from " + fromAccount.AccountName + ": " + transfer.Note,
                                Credit = transfer.Amount,
                                Debit = 0,
                                Amount = transfer.Amount,
                                Date = transfer.TransferDate,
                            };


                            _ = await _transactionService.AddDoubleEntryTransaction(debitAssetTrans, creditAssetTrans);
                        }

                        response.Add("status", "success");
                        response.Add("message", "success");
                    }
                    catch
                    {
                        response.Add("status", "error");
                        response.Add("message", "Error while saving transfer.");
                    }
                }
            }
            else
            {
                response.Add("status", "error");
                response.Add("message", "Enter required fields.");
            }
            return Json(response);
        }

        [HttpDelete]
        [Authorize(Roles = "admin")]
        public async Task<JsonResult> DeleteTransfer(int? Id)
        {
            var results = new Dictionary<string, string>();

            var existing = _dbContext.AccountMoneyTransfers.FirstOrDefault(x => x.Id == Id);

            if (existing != null)
            {
                try
                {
                    _dbContext.AccountMoneyTransfers.Remove(existing);
                    _ = await _transactionService.DeleteTransactionsByReference("transfer-" + existing.Id);
                    await _dbContext.SaveChangesAsync();

                    results.Add("status", "success");
                    results.Add("message", "success");

                }
                catch
                {
                    results.Add("status", "error");
                    results.Add("message", "Error while deleting transfer.");
                }
            }
            else
            {
                results.Add("status", "error");
                results.Add("message", "Transfer not exist.");
            }

            return Json(results);
        }

        /*
         * Deposit Views
         */

        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Deposits(int? account, DateTime? start, DateTime? end)
        {
            DateTime from = StartOfDay(start);
            DateTime to = EndOfDay(end);

            ViewBag.start = from.ToString("yyyy-MM-dd");
            ViewBag.end = to.ToString("yyyy-MM-dd"); ;

            var deposits = await _dbContext.Deposits
                .Where(e => e.DepositDate >= from && e.DepositDate <= to)
                .Include(x => x.Account)
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            if (account > 0)
                deposits = deposits.Where(e => e.AccountId == account).ToList();

            ViewBag.accounts = GetAccounts();
            ViewBag.account = account;

            return View(deposits);
        }

        /*
         * Deposit APIs
         */
        [HttpGet]
        [Authorize(Roles = "admin")]
        public JsonResult DepositDetail(int? Id)
        {
            try
            {
                var row = _dbContext.Deposits.FirstOrDefault(x => x.Id == Id);
                if (row != null)
                {
                    return Json(new
                    {
                        data = row,
                        status = "success",
                        message = "success"
                    });
                }
                else
                {
                    return Json(new
                    {
                        status = "error",
                        message = "Account not exist."
                    });
                }
            }
            catch
            {
                return Json(new
                {
                    status = "error",
                    message = "Something went wrong."
                });
            }
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> SaveDeposit(Deposit deposit)
        {
            var response = new Dictionary<string, string>();
            var userName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (ModelState.IsValid)
            {
                try
                {
                    deposit.UpdatedBy = userName;
                    deposit.UpdatedAt = CurrentDateTime();
                    if (deposit.Id > 0)
                    {
                        _dbContext.Deposits.Update(deposit);
                        await _dbContext.SaveChangesAsync();

                        Transaction statement = new()
                        {
                            AccountId = deposit.AccountId,
                            TransactionReference = "deposit-" + deposit.Id,
                            TransactionType = "deposit",
                            Description = deposit.Note,
                            Credit = deposit.Amount,
                            Debit = 0,
                            Amount = deposit.Amount,
                            Date = deposit.DepositDate,
                        };

                        _ = await _transactionService.UpdateTransaction(statement);
                    }
                    else
                    {
                        _dbContext.Deposits.Add(deposit);
                        await _dbContext.SaveChangesAsync();

                        // Find or Create Capital Account for matching Double Entry
                        var capitalAccount = _dbContext.Accounts.FirstOrDefault(x => x.AccountName == "Capital Account" && (x.AccountGroup == "Equity" || x.AccountGroup == "Owner Equity"));
                        int capitalAccountId = 0;
                        if (capitalAccount != null)
                        {
                            capitalAccountId = Convert.ToInt32(capitalAccount.Id);
                        }
                        else
                        {
                            var newCapAccount = new Account
                            {
                                AccountName = "Capital Account",
                                AccountNumber = "3000",
                                AccountGroup = "Equity",
                                AccountType = "Equity",
                                Credit = 0,
                                Debit = 0,
                                Balance = 0,
                                UpdatedAt = CurrentDateTime()
                            };
                            _dbContext.Accounts.Add(newCapAccount);
                            _dbContext.SaveChanges();
                            capitalAccountId = Convert.ToInt32(newCapAccount.Id);
                        }

                        Transaction debitEquityTrans = new()
                        {
                            AccountId = capitalAccountId,
                            TransactionReference = "deposit-" + deposit.Id,
                            TransactionType = "deposit",
                            Description = deposit.Note,
                            Credit = 0,
                            Debit = deposit.Amount,
                            Amount = deposit.Amount,
                            Date = deposit.DepositDate,
                        };

                        Transaction creditAssetTrans = new()
                        {
                            AccountId = deposit.AccountId,
                            TransactionReference = "deposit-" + deposit.Id,
                            TransactionType = "deposit",
                            Description = deposit.Note,
                            Credit = deposit.Amount,
                            Debit = 0,
                            Amount = deposit.Amount,
                            Date = deposit.DepositDate,
                        };

                        _ = await _transactionService.AddDoubleEntryTransaction(debitEquityTrans, creditAssetTrans);
                    }

                    response.Add("status", "success");
                    response.Add("message", "success");
                }
                catch
                {
                    response.Add("status", "error");
                    response.Add("message", "Error while saving deposit.");
                }
            }
            else
            {
                response.Add("status", "error");
                response.Add("message", "Enter required fields.");
            }
            return Json(response);
        }

        [HttpDelete]
        [Authorize(Roles = "admin")]
        public async Task<JsonResult> DeleteDeposit(int? Id)
        {
            var results = new Dictionary<string, string>();

            var existing = _dbContext.Deposits.FirstOrDefault(x => x.Id == Id);

            if (existing != null)
            {
                try
                {
                    _dbContext.Deposits.Remove(existing);
                    await _dbContext.SaveChangesAsync();

                    _ = await _transactionService.DeleteTransactionsByReference("deposit-" + existing.Id);

                    results.Add("status", "success");
                    results.Add("message", "success");

                }
                catch
                {
                    results.Add("status", "error");
                    results.Add("message", "Error while deleting deposit.");
                }
            }
            else
            {
                results.Add("status", "error");
                results.Add("message", "Deposit not exist.");
            }

            return Json(results);
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> SearchExpenseCategories(string q)
        {
            var query = _dbContext.Expenses.Where(e => !string.IsNullOrEmpty(e.Category));
            if (!string.IsNullOrEmpty(q))
            {
                query = query.Where(e => e.Category.Contains(q));
            }
            var categories = await query.Select(e => e.Category).Distinct().Take(20).ToListAsync();
            var results = categories.Select(c => new { id = c, text = c });
            return Json(new { results = results });
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> SearchExpenseItems(string q)
        {
            var query = _dbContext.Expenses.Where(e => !string.IsNullOrEmpty(e.Item));
            if (!string.IsNullOrEmpty(q))
            {
                query = query.Where(e => e.Item.Contains(q));
            }
            var items = await query.Select(e => e.Item).Distinct().Take(20).ToListAsync();
            var results = items.Select(i => new { id = i, text = i });
            return Json(new { results = results });
        }

        /*
         * Private Functions
         */
        private Dictionary<int, string> GetAccounts()
        {
            Dictionary<int, string> accounts = _dbContext.Accounts
                .Select(t => new
                {
                    t.Id,
                    t.AccountName
                }).ToDictionary(t => Convert.ToInt32(t.Id), t => t.AccountName);
            return accounts;
        }
    }
}