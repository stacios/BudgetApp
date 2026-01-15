using BudgetManager.Web.Models;

namespace BudgetManager.Web.Services.Interfaces;

public interface IAccountService
{
    Task<IEnumerable<Account>> GetAllAccountsAsync();
    
    Task<IEnumerable<Account>> GetActiveAccountsAsync();
    
    Task<Account?> GetAccountByIdAsync(int id);
    
    Task<Account?> GetAccountByNameAsync(string name);
    
    Task<Account> CreateAccountAsync(Account account, string? userId = null);
    
    Task<Account> UpdateAccountAsync(Account account, string? userId = null);
    
    Task<(bool Success, string Message)> DeleteAccountAsync(int id, string? userId = null);
}
