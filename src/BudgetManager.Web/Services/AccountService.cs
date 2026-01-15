using Microsoft.EntityFrameworkCore;
using BudgetManager.Web.Data;
using BudgetManager.Web.Models;
using BudgetManager.Web.Services.Interfaces;

namespace BudgetManager.Web.Services;

public class AccountService : IAccountService
{
    private readonly ApplicationDbContext _context;
    private readonly IActivityLogService _activityLogService;
    
    public AccountService(ApplicationDbContext context, IActivityLogService activityLogService)
    {
        _context = context;
        _activityLogService = activityLogService;
    }
    
    public async Task<IEnumerable<Account>> GetAllAccountsAsync()
    {
        return await _context.Accounts
            .OrderBy(a => a.Name)
            .ToListAsync();
    }
    
    public async Task<IEnumerable<Account>> GetActiveAccountsAsync()
    {
        return await _context.Accounts
            .Where(a => a.IsActive)
            .OrderBy(a => a.Name)
            .ToListAsync();
    }
    
    public async Task<Account?> GetAccountByIdAsync(int id)
    {
        return await _context.Accounts.FindAsync(id);
    }
    
    public async Task<Account?> GetAccountByNameAsync(string name)
    {
        return await _context.Accounts
            .FirstOrDefaultAsync(a => a.Name.ToLower() == name.ToLower());
    }
    
    public async Task<Account> CreateAccountAsync(Account account, string? userId = null)
    {
        account.CreatedAt = DateTime.UtcNow;
        
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();
        
        await _activityLogService.LogAsync(
            "Account",
            account.Id,
            "Create",
            $"Created account: {account.Name} ({account.Type})",
            null,
            new { account.Name, account.Type, account.IsActive },
            userId);
        
        return account;
    }
    
    public async Task<Account> UpdateAccountAsync(Account account, string? userId = null)
    {
        var existing = await _context.Accounts.FindAsync(account.Id);
        if (existing == null)
        {
            throw new InvalidOperationException($"Account {account.Id} not found.");
        }
        
        var oldValues = new
        {
            existing.Name,
            existing.Type,
            existing.IsActive
        };
        
        existing.Name = account.Name;
        existing.Type = account.Type;
        existing.IsActive = account.IsActive;
        
        await _context.SaveChangesAsync();
        
        await _activityLogService.LogAsync(
            "Account",
            account.Id,
            "Update",
            $"Updated account: {account.Name}",
            oldValues,
            new { account.Name, account.Type, account.IsActive },
            userId);
        
        return existing;
    }
    
    public async Task<(bool Success, string Message)> DeleteAccountAsync(int id, string? userId = null)
    {
        var account = await _context.Accounts
            .Include(a => a.Transactions)
            .FirstOrDefaultAsync(a => a.Id == id);
        
        if (account == null)
        {
            return (false, "Account not found.");
        }
        
        if (account.Transactions.Any())
        {
            return (false, "Cannot delete account with existing transactions. Consider deactivating it instead.");
        }
        
        var oldValues = new
        {
            account.Name,
            account.Type
        };
        
        _context.Accounts.Remove(account);
        await _context.SaveChangesAsync();
        
        await _activityLogService.LogAsync(
            "Account",
            id,
            "Delete",
            $"Deleted account: {account.Name}",
            oldValues,
            null,
            userId);
        
        return (true, "Account deleted successfully.");
    }
}
