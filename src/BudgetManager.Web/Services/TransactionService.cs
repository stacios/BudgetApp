using Microsoft.EntityFrameworkCore;
using BudgetManager.Web.Data;
using BudgetManager.Web.Models;
using BudgetManager.Web.Services.Interfaces;
using BudgetManager.Web.ViewModels;

namespace BudgetManager.Web.Services;

public class TransactionService : ITransactionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILockingService _lockingService;
    private readonly IActivityLogService _activityLogService;
    
    public TransactionService(
        ApplicationDbContext context, 
        ILockingService lockingService,
        IActivityLogService activityLogService)
    {
        _context = context;
        _lockingService = lockingService;
        _activityLogService = activityLogService;
    }
    
    public async Task<IEnumerable<Transaction>> GetTransactionsAsync(TransactionFilterViewModel? filter = null)
    {
        var query = _context.Transactions
            .Include(t => t.Category)
            .Include(t => t.Account)
            .AsQueryable();
        
        if (filter != null)
        {
            if (filter.StartDate.HasValue)
            {
                query = query.Where(t => t.Date >= filter.StartDate.Value);
            }
            
            if (filter.EndDate.HasValue)
            {
                query = query.Where(t => t.Date <= filter.EndDate.Value);
            }
            
            if (filter.CategoryId.HasValue)
            {
                query = query.Where(t => t.CategoryId == filter.CategoryId.Value);
            }
            
            if (filter.AccountId.HasValue)
            {
                query = query.Where(t => t.AccountId == filter.AccountId.Value);
            }
            
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.ToLower();
                query = query.Where(t => t.Description.ToLower().Contains(term) ||
                                        (t.Notes != null && t.Notes.ToLower().Contains(term)));
            }
            
            if (filter.MinAmount.HasValue)
            {
                // Filter by absolute value: amount >= min OR amount <= -min
                var minVal = filter.MinAmount.Value;
                query = query.Where(t => t.Amount >= minVal || t.Amount <= -minVal);
            }
            
            if (filter.MaxAmount.HasValue)
            {
                // Filter by absolute value: amount <= max AND amount >= -max
                var maxVal = filter.MaxAmount.Value;
                query = query.Where(t => t.Amount <= maxVal && t.Amount >= -maxVal);
            }
            
            if (filter.IsAdjustment.HasValue)
            {
                query = query.Where(t => t.IsAdjustment == filter.IsAdjustment.Value);
            }
        }
        
        return await query
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedAt)
            .Skip((filter?.Page ?? 1 - 1) * (filter?.PageSize ?? 50))
            .Take(filter?.PageSize ?? 50)
            .ToListAsync();
    }
    
    public async Task<Transaction?> GetTransactionByIdAsync(int id)
    {
        return await _context.Transactions
            .Include(t => t.Category)
            .Include(t => t.Account)
            .FirstOrDefaultAsync(t => t.Id == id);
    }
    
    public async Task<(bool Success, string Message, Transaction? Transaction)> CreateTransactionAsync(
        Transaction transaction, string? userId = null)
    {
        // Check if month is locked (adjustments are always allowed)
        if (!transaction.IsAdjustment)
        {
            var isLocked = await _lockingService.IsMonthLockedAsync(
                transaction.Date.Year, transaction.Date.Month);
            
            if (isLocked)
            {
                return (false, "Cannot create transaction in a locked month. Mark as adjustment if needed.", null);
            }
        }
        
        transaction.CreatedAt = DateTime.UtcNow;
        transaction.UserId = userId;
        
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();
        
        await _activityLogService.LogAsync(
            "Transaction",
            transaction.Id,
            "Create",
            $"Created transaction: {transaction.Description} ({transaction.Amount:C})",
            null,
            new { transaction.Date, transaction.Description, transaction.Amount, transaction.CategoryId, transaction.AccountId },
            userId);
        
        return (true, "Transaction created successfully.", transaction);
    }
    
    public async Task<(bool Success, string Message)> UpdateTransactionAsync(Transaction transaction, string? userId = null)
    {
        var existing = await _context.Transactions.FindAsync(transaction.Id);
        if (existing == null)
        {
            return (false, "Transaction not found.");
        }
        
        // Check if can edit (adjustments bypass lock)
        if (!await _lockingService.CanEditTransactionAsync(existing))
        {
            return (false, "Cannot edit transaction in a locked month.");
        }
        
        var oldValues = new
        {
            existing.Date,
            existing.Description,
            existing.Amount,
            existing.CategoryId,
            existing.AccountId,
            existing.Notes,
            existing.IsAdjustment
        };
        
        existing.Date = transaction.Date;
        existing.Description = transaction.Description;
        existing.Amount = transaction.Amount;
        existing.CategoryId = transaction.CategoryId;
        existing.AccountId = transaction.AccountId;
        existing.Notes = transaction.Notes;
        existing.IsAdjustment = transaction.IsAdjustment;
        existing.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        await _activityLogService.LogAsync(
            "Transaction",
            transaction.Id,
            "Update",
            $"Updated transaction: {transaction.Description}",
            oldValues,
            new { transaction.Date, transaction.Description, transaction.Amount, transaction.CategoryId, transaction.AccountId, transaction.Notes, transaction.IsAdjustment },
            userId);
        
        return (true, "Transaction updated successfully.");
    }
    
    public async Task<(bool Success, string Message)> DeleteTransactionAsync(int id, string? userId = null)
    {
        var transaction = await _context.Transactions.FindAsync(id);
        if (transaction == null)
        {
            return (false, "Transaction not found.");
        }
        
        // Check if can delete (adjustments bypass lock)
        if (!await _lockingService.CanDeleteTransactionAsync(transaction))
        {
            return (false, "Cannot delete transaction in a locked month.");
        }
        
        var oldValues = new
        {
            transaction.Date,
            transaction.Description,
            transaction.Amount,
            transaction.CategoryId,
            transaction.AccountId
        };
        
        _context.Transactions.Remove(transaction);
        await _context.SaveChangesAsync();
        
        await _activityLogService.LogAsync(
            "Transaction",
            id,
            "Delete",
            $"Deleted transaction: {transaction.Description} ({transaction.Amount:C})",
            oldValues,
            null,
            userId);
        
        return (true, "Transaction deleted successfully.");
    }
    
    public async Task<decimal> GetTotalSpentAsync(int year, int month, int? categoryId = null, int? accountId = null)
    {
        var query = _context.Transactions
            .Where(t => t.Date.Year == year && t.Date.Month == month);
        
        if (categoryId.HasValue)
        {
            query = query.Where(t => t.CategoryId == categoryId.Value);
        }
        
        if (accountId.HasValue)
        {
            query = query.Where(t => t.AccountId == accountId.Value);
        }
        
        // Sum negative amounts (expenses) as positive value
        var total = await query
            .Where(t => t.Amount < 0)
            .SumAsync(t => -t.Amount);
        
        return total;
    }
    
    public async Task<IEnumerable<Transaction>> GetTransactionsForMonthAsync(int year, int month)
    {
        return await _context.Transactions
            .Include(t => t.Category)
            .Include(t => t.Account)
            .Where(t => t.Date.Year == year && t.Date.Month == month)
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync();
    }
    
    public async Task<IEnumerable<TopExpenseViewModel>> GetTopExpensesAsync(int year, int month, int count = 10)
    {
        return await _context.Transactions
            .Include(t => t.Category)
            .Where(t => t.Date.Year == year && t.Date.Month == month && t.Amount < 0)
            .OrderBy(t => t.Amount) // Most negative first
            .Take(count)
            .Select(t => new TopExpenseViewModel
            {
                Date = t.Date,
                Description = t.Description,
                Amount = -t.Amount,
                CategoryName = t.Category != null ? t.Category.Name : "Uncategorized"
            })
            .ToListAsync();
    }
    
    public async Task<int> GetTransactionCountAsync(TransactionFilterViewModel? filter = null)
    {
        var query = _context.Transactions.AsQueryable();
        
        if (filter != null)
        {
            if (filter.StartDate.HasValue)
            {
                query = query.Where(t => t.Date >= filter.StartDate.Value);
            }
            
            if (filter.EndDate.HasValue)
            {
                query = query.Where(t => t.Date <= filter.EndDate.Value);
            }
            
            if (filter.CategoryId.HasValue)
            {
                query = query.Where(t => t.CategoryId == filter.CategoryId.Value);
            }
            
            if (filter.AccountId.HasValue)
            {
                query = query.Where(t => t.AccountId == filter.AccountId.Value);
            }
        }
        
        return await query.CountAsync();
    }
}
