using BudgetManager.Web.Services.Interfaces;

namespace BudgetManager.Web.ViewModels;

public class BudgetSummaryViewModel
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal BudgetAmount { get; set; }
    public decimal SpentAmount { get; set; }
    public decimal Remaining { get; set; }
    public decimal ExpectedToDate { get; set; }
    public decimal SafeToSpendToday { get; set; }
    public BudgetStatus Status { get; set; }
    public decimal PercentUsed { get; set; }
    
    public string StatusClass => Status switch
    {
        BudgetStatus.OK => "success",
        BudgetStatus.WATCH => "warning",
        BudgetStatus.OVER => "danger",
        _ => "secondary"
    };
    
    public string StatusText => Status switch
    {
        BudgetStatus.OK => "On Track",
        BudgetStatus.WATCH => "Watch",
        BudgetStatus.OVER => "Over Budget",
        _ => "Unknown"
    };
}

public class BudgetPacingViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalBudget { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal TotalRemaining { get; set; }
    public decimal ExpectedToDate { get; set; }
    public decimal SafeToSpendToday { get; set; }
    public int DaysInMonth { get; set; }
    public int CurrentDay { get; set; }
    public int RemainingDays { get; set; }
    public BudgetStatus OverallStatus { get; set; }
    public IEnumerable<BudgetSummaryViewModel> CategorySummaries { get; set; } = new List<BudgetSummaryViewModel>();
    
    public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM yyyy");
    
    public decimal PercentOfMonthElapsed => DaysInMonth > 0 ? (decimal)CurrentDay / DaysInMonth * 100 : 0;
    
    public decimal PercentSpent => TotalBudget > 0 ? TotalSpent / TotalBudget * 100 : 0;
}

public class BudgetEditViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public IEnumerable<BudgetCategoryEditViewModel> Categories { get; set; } = new List<BudgetCategoryEditViewModel>();
    
    public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM yyyy");
}

public class BudgetCategoryEditViewModel
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal BudgetAmount { get; set; }
    public decimal? PreviousMonthBudget { get; set; }
    public decimal? PreviousMonthSpent { get; set; }
}

public class MonthSelectorViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public IEnumerable<(int Year, int Month, string Name)> AvailableMonths { get; set; } 
        = new List<(int, int, string)>();
}
