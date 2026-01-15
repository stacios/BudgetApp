using Microsoft.EntityFrameworkCore;
using BudgetManager.Web.Data;
using BudgetManager.Web.Models;
using BudgetManager.Web.Services.Interfaces;
using BudgetManager.Web.ViewModels;

namespace BudgetManager.Web.Services;

public class BudgetService : IBudgetService
{
    private readonly ApplicationDbContext _context;
    private readonly IActivityLogService _activityLogService;
    
    public BudgetService(ApplicationDbContext context, IActivityLogService activityLogService)
    {
        _context = context;
        _activityLogService = activityLogService;
    }
    
    public async Task<IEnumerable<MonthlyBudget>> GetBudgetsForMonthAsync(int year, int month)
    {
        return await _context.MonthlyBudgets
            .Include(b => b.Category)
            .Where(b => b.Year == year && b.Month == month)
            .OrderBy(b => b.Category!.Name)
            .ToListAsync();
    }
    
    public async Task<MonthlyBudget?> GetBudgetAsync(int id)
    {
        return await _context.MonthlyBudgets
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Id == id);
    }
    
    public async Task<MonthlyBudget?> GetBudgetAsync(int year, int month, int categoryId)
    {
        return await _context.MonthlyBudgets
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Year == year && b.Month == month && b.CategoryId == categoryId);
    }
    
    public async Task<MonthlyBudget> CreateOrUpdateBudgetAsync(int year, int month, int categoryId, decimal amount, string? userId = null)
    {
        var existing = await GetBudgetAsync(year, month, categoryId);
        
        if (existing != null)
        {
            var oldAmount = existing.BudgetAmount;
            existing.BudgetAmount = amount;
            existing.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            
            await _activityLogService.LogAsync(
                "MonthlyBudget",
                existing.Id,
                "Update",
                $"Updated budget for category {categoryId} ({year}/{month}): {oldAmount:C} → {amount:C}",
                new { OldAmount = oldAmount },
                new { NewAmount = amount },
                userId);
            
            return existing;
        }
        
        var budget = new MonthlyBudget
        {
            Year = year,
            Month = month,
            CategoryId = categoryId,
            BudgetAmount = amount,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.MonthlyBudgets.Add(budget);
        await _context.SaveChangesAsync();
        
        await _activityLogService.LogAsync(
            "MonthlyBudget",
            budget.Id,
            "Create",
            $"Created budget for category {categoryId} ({year}/{month}): {amount:C}",
            null,
            new { Year = year, Month = month, CategoryId = categoryId, Amount = amount },
            userId);
        
        return budget;
    }
    
    public async Task DeleteBudgetAsync(int id, string? userId = null)
    {
        var budget = await _context.MonthlyBudgets.FindAsync(id);
        if (budget == null)
        {
            throw new InvalidOperationException($"Budget {id} not found.");
        }
        
        var oldValues = new
        {
            budget.Year,
            budget.Month,
            budget.CategoryId,
            budget.BudgetAmount
        };
        
        _context.MonthlyBudgets.Remove(budget);
        await _context.SaveChangesAsync();
        
        await _activityLogService.LogAsync(
            "MonthlyBudget",
            id,
            "Delete",
            $"Deleted budget for category {budget.CategoryId} ({budget.Year}/{budget.Month})",
            oldValues,
            null,
            userId);
    }
    
    public async Task<IEnumerable<BudgetSummaryViewModel>> GetBudgetSummaryAsync(int year, int month)
    {
        var categories = await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
        
        var budgets = await GetBudgetsForMonthAsync(year, month);
        var budgetDict = budgets.ToDictionary(b => b.CategoryId, b => b.BudgetAmount);
        
        var spent = await _context.Transactions
            .Where(t => t.Date.Year == year && t.Date.Month == month && t.Amount < 0)
            .GroupBy(t => t.CategoryId)
            .Select(g => new { CategoryId = g.Key, Total = g.Sum(t => -t.Amount) })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Total);
        
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var currentDay = DateTime.Now.Year == year && DateTime.Now.Month == month
            ? DateTime.Now.Day
            : daysInMonth;
        
        var summaries = new List<BudgetSummaryViewModel>();
        
        foreach (var category in categories)
        {
            var budgetAmount = budgetDict.TryGetValue(category.Id, out var b) ? b : 0;
            var spentAmount = spent.TryGetValue(category.Id, out var s) ? s : 0;
            var remaining = budgetAmount - spentAmount;
            
            // Calculate expected spend to date (prorated)
            var expectedToDate = budgetAmount * currentDay / daysInMonth;
            
            // Calculate safe to spend today
            var remainingDays = daysInMonth - currentDay + 1;
            var safeToSpendToday = remainingDays > 0 ? remaining / remainingDays : 0;
            
            // Determine status
            BudgetStatus status;
            if (spentAmount > budgetAmount && budgetAmount > 0)
            {
                status = BudgetStatus.OVER;
            }
            else if (spentAmount > expectedToDate && budgetAmount > 0)
            {
                status = BudgetStatus.WATCH;
            }
            else
            {
                status = BudgetStatus.OK;
            }
            
            summaries.Add(new BudgetSummaryViewModel
            {
                CategoryId = category.Id,
                CategoryName = category.Name,
                BudgetAmount = budgetAmount,
                SpentAmount = spentAmount,
                Remaining = remaining,
                ExpectedToDate = expectedToDate,
                SafeToSpendToday = Math.Max(0, safeToSpendToday),
                Status = status,
                PercentUsed = budgetAmount > 0 ? (spentAmount / budgetAmount) * 100 : 0
            });
        }
        
        return summaries;
    }
    
    public async Task<BudgetPacingViewModel> GetBudgetPacingAsync(int year, int month)
    {
        var summaries = await GetBudgetSummaryAsync(year, month);
        var summaryList = summaries.ToList();
        
        var totalBudget = summaryList.Sum(s => s.BudgetAmount);
        var totalSpent = summaryList.Sum(s => s.SpentAmount);
        var totalRemaining = totalBudget - totalSpent;
        
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var currentDay = DateTime.Now.Year == year && DateTime.Now.Month == month
            ? DateTime.Now.Day
            : daysInMonth;
        
        var expectedToDate = totalBudget * currentDay / daysInMonth;
        var remainingDays = daysInMonth - currentDay + 1;
        var safeToSpendToday = remainingDays > 0 ? totalRemaining / remainingDays : 0;
        
        BudgetStatus overallStatus;
        if (totalSpent > totalBudget && totalBudget > 0)
        {
            overallStatus = BudgetStatus.OVER;
        }
        else if (totalSpent > expectedToDate && totalBudget > 0)
        {
            overallStatus = BudgetStatus.WATCH;
        }
        else
        {
            overallStatus = BudgetStatus.OK;
        }
        
        return new BudgetPacingViewModel
        {
            Year = year,
            Month = month,
            TotalBudget = totalBudget,
            TotalSpent = totalSpent,
            TotalRemaining = totalRemaining,
            ExpectedToDate = expectedToDate,
            SafeToSpendToday = Math.Max(0, safeToSpendToday),
            DaysInMonth = daysInMonth,
            CurrentDay = currentDay,
            RemainingDays = remainingDays,
            OverallStatus = overallStatus,
            CategorySummaries = summaryList
        };
    }
    
    public async Task CopyBudgetsFromPreviousMonthAsync(int year, int month, string? userId = null)
    {
        // Calculate previous month
        var prevMonth = month == 1 ? 12 : month - 1;
        var prevYear = month == 1 ? year - 1 : year;
        
        var previousBudgets = await GetBudgetsForMonthAsync(prevYear, prevMonth);
        var currentBudgets = await GetBudgetsForMonthAsync(year, month);
        var currentCategoryIds = currentBudgets.Select(b => b.CategoryId).ToHashSet();
        
        var copiedCount = 0;
        
        foreach (var prevBudget in previousBudgets)
        {
            if (!currentCategoryIds.Contains(prevBudget.CategoryId))
            {
                await CreateOrUpdateBudgetAsync(year, month, prevBudget.CategoryId, prevBudget.BudgetAmount, userId);
                copiedCount++;
            }
        }
        
        if (copiedCount > 0)
        {
            await _activityLogService.LogAsync(
                "MonthlyBudget",
                null,
                "Copy",
                $"Copied {copiedCount} budgets from {prevMonth}/{prevYear} to {month}/{year}",
                null,
                new { SourceMonth = prevMonth, SourceYear = prevYear, TargetMonth = month, TargetYear = year, Count = copiedCount },
                userId);
        }
    }
}
