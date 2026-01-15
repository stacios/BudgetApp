using BudgetManager.Web.Models;
using BudgetManager.Web.ViewModels;

namespace BudgetManager.Web.Services.Interfaces;

public interface IBudgetService
{
    Task<IEnumerable<MonthlyBudget>> GetBudgetsForMonthAsync(int year, int month);
    
    Task<MonthlyBudget?> GetBudgetAsync(int id);
    
    Task<MonthlyBudget?> GetBudgetAsync(int year, int month, int categoryId);
    
    Task<MonthlyBudget> CreateOrUpdateBudgetAsync(int year, int month, int categoryId, decimal amount, string? userId = null);
    
    Task DeleteBudgetAsync(int id, string? userId = null);
    
    Task<IEnumerable<BudgetSummaryViewModel>> GetBudgetSummaryAsync(int year, int month);
    
    Task<BudgetPacingViewModel> GetBudgetPacingAsync(int year, int month);
    
    Task CopyBudgetsFromPreviousMonthAsync(int year, int month, string? userId = null);
}

public enum BudgetStatus
{
    OK,
    WATCH,
    OVER
}
