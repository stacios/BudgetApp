using BudgetManager.Web.Models;

namespace BudgetManager.Web.Services.Interfaces;

public interface ILockingService
{
    Task<bool> IsMonthLockedAsync(int year, int month);
    
    Task<LockedMonth?> GetLockedMonthAsync(int year, int month);
    
    Task<IEnumerable<LockedMonth>> GetAllLockedMonthsAsync();
    
    Task<(bool Success, string Message)> LockMonthAsync(int year, int month, string userId);
    
    Task<(bool Success, string Message)> UnlockMonthAsync(int year, int month, string userId);
    
    Task<bool> CanEditTransactionAsync(Transaction transaction);
    
    Task<bool> CanDeleteTransactionAsync(Transaction transaction);
}
