using BudgetManager.Web.Models;
using BudgetManager.Web.ViewModels;

namespace BudgetManager.Web.Services.Interfaces;

public interface ITransactionService
{
    Task<IEnumerable<Transaction>> GetTransactionsAsync(TransactionFilterViewModel? filter = null);
    
    Task<Transaction?> GetTransactionByIdAsync(int id);
    
    Task<(bool Success, string Message, Transaction? Transaction)> CreateTransactionAsync(Transaction transaction, string? userId = null);
    
    Task<(bool Success, string Message)> UpdateTransactionAsync(Transaction transaction, string? userId = null);
    
    Task<(bool Success, string Message)> DeleteTransactionAsync(int id, string? userId = null);
    
    Task<decimal> GetTotalSpentAsync(int year, int month, int? categoryId = null, int? accountId = null);
    
    Task<IEnumerable<Transaction>> GetTransactionsForMonthAsync(int year, int month);
    
    Task<IEnumerable<TopExpenseViewModel>> GetTopExpensesAsync(int year, int month, int count = 10);
    
    Task<int> GetTransactionCountAsync(TransactionFilterViewModel? filter = null);
}
