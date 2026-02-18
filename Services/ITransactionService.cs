using Saffrat.Models;

namespace Saffrat.Services
{
    public interface ITransactionService
    {
        Task<bool> AddTransaction(Transaction transaction);
        Task<bool> UpdateTransaction(Transaction transaction);
        Task<bool> UpdateTransaction(Transaction transaction, int Id);
        Task<bool> DeleteTransaction(Transaction trans);
    }
}
