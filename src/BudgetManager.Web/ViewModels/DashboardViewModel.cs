using BudgetManager.Web.Services.Interfaces;

namespace BudgetManager.Web.ViewModels;

public class DashboardViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM yyyy");
    
    // Summary stats
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetChange => TotalIncome - TotalExpenses;
    
    // Budget overview
    public decimal TotalBudget { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal Remaining => TotalBudget - TotalSpent;
    public decimal PercentUsed => TotalBudget > 0 ? (TotalSpent / TotalBudget) * 100 : 0;
    public BudgetStatus OverallStatus { get; set; }
    
    // Pacing
    public decimal ExpectedSpentToDate { get; set; }
    public decimal SafeToSpendToday { get; set; }
    public int DaysRemaining { get; set; }
    
    // Category breakdowns
    public IEnumerable<BudgetSummaryViewModel> TopCategories { get; set; } = new List<BudgetSummaryViewModel>();
    public IEnumerable<BudgetSummaryViewModel> OverBudgetCategories { get; set; } = new List<BudgetSummaryViewModel>();
    
    // Recent transactions
    public IEnumerable<TransactionViewModel> RecentTransactions { get; set; } = new List<TransactionViewModel>();
    
    // Top expenses
    public IEnumerable<TopExpenseViewModel> TopExpenses { get; set; } = new List<TopExpenseViewModel>();
    
    // Alerts
    public bool IsMonthLocked { get; set; }
    public int UncategorizedCount { get; set; }
    
    public string StatusClass => OverallStatus switch
    {
        BudgetStatus.OK => "success",
        BudgetStatus.WATCH => "warning",
        BudgetStatus.OVER => "danger",
        _ => "secondary"
    };
}
