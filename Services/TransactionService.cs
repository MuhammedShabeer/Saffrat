using Saffrat.Models;

namespace Saffrat.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly RestaurantDBContext _dbContext;

        public TransactionService(RestaurantDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Add a transaction and update the associated account balance.
        public async Task<bool> AddTransaction(Transaction transaction)
        {
            // Begin a database transaction.
            using var t = _dbContext.Database.BeginTransaction();
            try
            {
                // Add the transaction to the database.
                _dbContext.Transactions.Add(transaction);

                // Find the associated account and update its balance.
                var account = _dbContext.Accounts.FirstOrDefault(x => x.Id == transaction.AccountId);
                if (account != null)
                {
                    // Update account balance.
                    account.Credit += transaction.Credit;
                    account.Debit += transaction.Debit;
                    account.Balance = account.Credit - account.Debit;
                    _dbContext.Accounts.Update(account);
                }

                // Save changes to the database and commit the transaction.
                await _dbContext.SaveChangesAsync();
                await t.CommitAsync();
                return true;
            }
            catch
            {
                // Rollback the transaction on failure and return false.
                await t.RollbackAsync();
                return false;
            }
        }

        public async Task<bool> AddDoubleEntryTransaction(Transaction debitTrans, Transaction creditTrans)
        {
            // Begin a database transaction.
            using var t = _dbContext.Database.BeginTransaction();
            try
            {
                // Add the transactions to the database.
                _dbContext.Transactions.Add(debitTrans);
                _dbContext.Transactions.Add(creditTrans);

                // Find the associated accounts and update balances.
                var debitAccount = _dbContext.Accounts.FirstOrDefault(x => x.Id == debitTrans.AccountId);
                if (debitAccount != null)
                {
                    // Update account balance (Debits increase assets/expenses, decrease liabilities/equity/revenue, but Saffrat balances might be naive Credit - Debit. We will adjust based on Saffrat logic: Balance = Credit - Debit )
                    debitAccount.Credit += debitTrans.Credit;
                    debitAccount.Debit += debitTrans.Debit;
                    debitAccount.Balance = debitAccount.Credit - debitAccount.Debit;
                    _dbContext.Accounts.Update(debitAccount);
                }

                var creditAccount = _dbContext.Accounts.FirstOrDefault(x => x.Id == creditTrans.AccountId);
                if (creditAccount != null)
                {
                    creditAccount.Credit += creditTrans.Credit;
                    creditAccount.Debit += creditTrans.Debit;
                    creditAccount.Balance = creditAccount.Credit - creditAccount.Debit;
                    _dbContext.Accounts.Update(creditAccount);
                }

                // Save changes to the database and commit the transaction.
                await _dbContext.SaveChangesAsync();
                await t.CommitAsync();
                return true;
            }
            catch
            {
                // Rollback the transaction on failure and return false.
                await t.RollbackAsync();
                return false;
            }
        }

        // Update a transaction and its associated account balance.
        public async Task<bool> UpdateTransaction(Transaction transaction)
        {
            // Begin a database transaction.
            using var t = _dbContext.Database.BeginTransaction();
            try
            {
                // Find the existing transaction.
                var trans = _dbContext.Transactions.FirstOrDefault(x => x.TransactionReference == transaction.TransactionReference);
                if (trans != null)
                {
                    // Find and update the associated account balance.
                    var account = _dbContext.Accounts.FirstOrDefault(x => x.Id == trans.AccountId);
                    if (account != null)
                    {
                        // Adjust the old account balance.
                        account.Credit -= trans.Credit;
                        account.Debit -= trans.Debit;
                        account.Balance = account.Credit - account.Debit;

                        // If the account has changed, update it in the database.
                        if (trans.AccountId != transaction.AccountId)
                        {
                            _dbContext.Accounts.Update(account);
                            await _dbContext.SaveChangesAsync();
                            account = _dbContext.Accounts.FirstOrDefault(x => x.Id == transaction.AccountId);
                        }

                        // Update the transaction details.
                        trans.Credit = transaction.Credit;
                        trans.Debit = transaction.Debit;
                        trans.Description = transaction.Description;
                        trans.TransactionType = transaction.TransactionType;
                        trans.Date = transaction.Date;

                        // Update the transaction in the database.
                        _dbContext.Transactions.Update(trans);

                        // Update the new account balance.
                        account.Credit += trans.Credit;
                        account.Debit += trans.Debit;
                        account.Balance = account.Credit - account.Debit;

                        // Update the account in the database.
                        _dbContext.Accounts.Update(account);

                        // Save changes to the database and commit the transaction.
                        await _dbContext.SaveChangesAsync();
                        await t.CommitAsync();
                        return true;
                    }
                    return false;
                }
                return false;
            }
            catch
            {
                // Rollback the transaction on failure and return false.
                await t.RollbackAsync();
                return false;
            }
        }

        // Update a transaction by ID and its associated account balance.
        public async Task<bool> UpdateTransaction(Transaction transaction, int Id)
        {
            // Begin a database transaction.
            using var t = _dbContext.Database.BeginTransaction();
            try
            {
                // Find the existing transaction by ID.
                var trans = _dbContext.Transactions.FirstOrDefault(x => x.Id == Id);
                if (trans != null)
                {
                    // Find and update the associated account balance.
                    var account = _dbContext.Accounts.FirstOrDefault(x => x.Id == trans.AccountId);
                    if (account != null)
                    {
                        // Adjust the old account balance.
                        account.Credit -= trans.Credit;
                        account.Debit -= trans.Debit;
                        account.Balance = account.Credit - account.Debit;

                        // If the account has changed, update it in the database.
                        if (trans.AccountId != transaction.AccountId)
                        {
                            _dbContext.Accounts.Update(account);
                            await _dbContext.SaveChangesAsync();
                            account = _dbContext.Accounts.FirstOrDefault(x => x.Id == transaction.AccountId);
                        }

                        // Update the transaction details.
                        trans.Credit = transaction.Credit;
                        trans.Debit = transaction.Debit;
                        trans.Description = transaction.Description;
                        trans.TransactionType = transaction.TransactionType;
                        trans.Date = transaction.Date;

                        // Update the transaction in the database.
                        _dbContext.Transactions.Update(trans);

                        // Update the new account balance.
                        account.Credit += trans.Credit;
                        account.Debit += trans.Debit;
                        account.Balance = account.Credit - account.Debit;

                        // Update the account in the database.
                        _dbContext.Accounts.Update(account);

                        // Save changes to the database and commit the transaction.
                        await _dbContext.SaveChangesAsync();
                        await t.CommitAsync();
                        return true;
                    }
                    return false;
                }
                return false;
            }
            catch
            {
                // Rollback the transaction on failure and return false.
                await t.RollbackAsync();
                return false;
            }
        }

        // Delete a transaction and update the associated account balance.
        public async Task<bool> DeleteTransaction(Transaction trans)
        {
            // Begin a database transaction.
            using var t = _dbContext.Database.BeginTransaction();
            try
            {
                if (trans != null)
                {
                    // Find and update the associated account balance.
                    var account = _dbContext.Accounts.FirstOrDefault(x => x.Id == trans.AccountId);
                    if (account != null)
                    {
                        // Adjust the account balance.
                        account.Credit -= trans.Credit;
                        account.Debit -= trans.Debit;
                        account.Balance = account.Credit - account.Debit;

                        // Remove the transaction from the database.
                        _dbContext.Transactions.Remove(trans);

                        // Update the account in the database.
                        _dbContext.Accounts.Update(account);

                        // Save changes to the database and commit the transaction.
                        await _dbContext.SaveChangesAsync();
                        await t.CommitAsync();
                        return true;
                    }
                    return false;
                }
                return false;
            }
            catch
            {
                // Rollback the transaction on failure and return false.
                await t.RollbackAsync();
                return false;
            }
        }

        public async Task<bool> DeleteTransactionsByReference(string reference)
        {
            // Begin a database transaction.
            using var t = _dbContext.Database.BeginTransaction();
            try
            {
                var transactions = _dbContext.Transactions.Where(x => x.TransactionReference == reference).ToList();
                foreach (var trans in transactions)
                {
                    // Find and update the associated account balance.
                    var account = _dbContext.Accounts.FirstOrDefault(x => x.Id == trans.AccountId);
                    if (account != null)
                    {
                        // Reverse the balance
                        account.Credit -= trans.Credit;
                        account.Debit -= trans.Debit;
                        account.Balance = account.Credit - account.Debit;

                        // Remove the transaction
                        _dbContext.Transactions.Remove(trans);
                        _dbContext.Accounts.Update(account);
                    }
                }

                // Save and commit
                await _dbContext.SaveChangesAsync();
                await t.CommitAsync();
                return true;
            }
            catch
            {
                // Rollback the transaction on failure and return false.
                await t.RollbackAsync();
                return false;
            }
        }
    }
}
