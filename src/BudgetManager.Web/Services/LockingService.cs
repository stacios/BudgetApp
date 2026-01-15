using Microsoft.EntityFrameworkCore;
using BudgetManager.Web.Data;
using BudgetManager.Web.Models;
using BudgetManager.Web.Services.Interfaces;

namespace BudgetManager.Web.Services;

public class LockingService : ILockingService
{
    private readonly ApplicationDbContext _context;
    private readonly IActivityLogService _activityLogService;
    
    public LockingService(ApplicationDbContext context, IActivityLogService activityLogService)
    {
        _context = context;
        _activityLogService = activityLogService;
    }
    
    public async Task<bool> IsMonthLockedAsync(int year, int month)
    {
        return await _context.LockedMonths
            .AnyAsync(lm => lm.Year == year && lm.Month == month);
    }
    
    public async Task<LockedMonth?> GetLockedMonthAsync(int year, int month)
    {
        return await _context.LockedMonths
            .Include(lm => lm.LockedByUser)
            .FirstOrDefaultAsync(lm => lm.Year == year && lm.Month == month);
    }
    
    public async Task<IEnumerable<LockedMonth>> GetAllLockedMonthsAsync()
    {
        return await _context.LockedMonths
            .Include(lm => lm.LockedByUser)
            .OrderByDescending(lm => lm.Year)
            .ThenByDescending(lm => lm.Month)
            .ToListAsync();
    }
    
    public async Task<(bool Success, string Message)> LockMonthAsync(int year, int month, string userId)
    {
        if (await IsMonthLockedAsync(year, month))
        {
            return (false, $"{GetMonthName(month)} {year} is already locked.");
        }
        
        var lockedMonth = new LockedMonth
        {
            Year = year,
            Month = month,
            LockedByUserId = userId,
            LockedAt = DateTime.UtcNow
        };
        
        _context.LockedMonths.Add(lockedMonth);
        await _context.SaveChangesAsync();
        
        await _activityLogService.LogAsync(
            "LockedMonth",
            lockedMonth.Id,
            "Lock",
            $"Locked {GetMonthName(month)} {year}",
            null,
            new { Year = year, Month = month },
            userId);
        
        return (true, $"{GetMonthName(month)} {year} has been locked.");
    }
    
    public async Task<(bool Success, string Message)> UnlockMonthAsync(int year, int month, string userId)
    {
        var lockedMonth = await GetLockedMonthAsync(year, month);
        
        if (lockedMonth == null)
        {
            return (false, $"{GetMonthName(month)} {year} is not locked.");
        }
        
        _context.LockedMonths.Remove(lockedMonth);
        await _context.SaveChangesAsync();
        
        await _activityLogService.LogAsync(
            "LockedMonth",
            lockedMonth.Id,
            "Unlock",
            $"Unlocked {GetMonthName(month)} {year}",
            new { Year = year, Month = month },
            null,
            userId);
        
        return (true, $"{GetMonthName(month)} {year} has been unlocked.");
    }
    
    public async Task<bool> CanEditTransactionAsync(Transaction transaction)
    {
        // Adjustments can always be edited
        if (transaction.IsAdjustment)
        {
            return true;
        }
        
        var isLocked = await IsMonthLockedAsync(transaction.Date.Year, transaction.Date.Month);
        return !isLocked;
    }
    
    public async Task<bool> CanDeleteTransactionAsync(Transaction transaction)
    {
        // Adjustments can always be deleted
        if (transaction.IsAdjustment)
        {
            return true;
        }
        
        var isLocked = await IsMonthLockedAsync(transaction.Date.Year, transaction.Date.Month);
        return !isLocked;
    }
    
    private static string GetMonthName(int month)
    {
        return new DateTime(2000, month, 1).ToString("MMMM");
    }
}
