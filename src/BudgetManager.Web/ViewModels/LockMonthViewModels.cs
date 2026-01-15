namespace BudgetManager.Web.ViewModels;

public class LockMonthViewModel
{
    public int Id { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM yyyy");
    public DateTime LockedAt { get; set; }
    public string? LockedByUserName { get; set; }
    public int TransactionCount { get; set; }
    public decimal TotalExpenses { get; set; }
}

public class LockMonthListViewModel
{
    public IEnumerable<LockMonthViewModel> LockedMonths { get; set; } = new List<LockMonthViewModel>();
    public IEnumerable<UnlockedMonthViewModel> UnlockedMonths { get; set; } = new List<UnlockedMonthViewModel>();
}

public class UnlockedMonthViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM yyyy");
    public int TransactionCount { get; set; }
    public decimal TotalExpenses { get; set; }
    public bool CanLock { get; set; }
}

public class LockMonthActionViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string Action { get; set; } = "Lock"; // Lock or Unlock
}
