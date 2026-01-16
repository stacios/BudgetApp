namespace BudgetManager.Web.ViewModels;

public class BudgetVsActualReportViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM yyyy");
    public IEnumerable<BudgetVsActualCategoryViewModel> Categories { get; set; } = new List<BudgetVsActualCategoryViewModel>();
    public decimal TotalBudget { get; set; }
    public decimal TotalActual { get; set; }
    public decimal TotalVariance => TotalBudget - TotalActual;
}

public class BudgetVsActualCategoryViewModel
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal Budget { get; set; }
    public decimal Actual { get; set; }
    public decimal Variance => Budget - Actual;
    public decimal PercentUsed => Budget > 0 ? (Actual / Budget) * 100 : 0;
}

public class MonthOverMonthReportViewModel
{
    public int Year { get; set; }
    public IEnumerable<MonthOverMonthDataPoint> DataPoints { get; set; } = new List<MonthOverMonthDataPoint>();
    public IEnumerable<string> Categories { get; set; } = new List<string>();
}

public class MonthOverMonthDataPoint
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName => new DateTime(Year, Month, 1).ToString("MMM");
    public string FullMonthName => new DateTime(Year, Month, 1).ToString("MMM yyyy");
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetChange => TotalIncome - TotalExpenses;
    public Dictionary<string, decimal> CategoryTotals { get; set; } = new();
}

public class TopExpensesReportViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM yyyy");
    public int TopCount { get; set; } = 10;
    public IEnumerable<TopExpenseViewModel> Expenses { get; set; } = new List<TopExpenseViewModel>();
    public decimal TotalAmount { get; set; }
}

public class ReportFilterViewModel
{
    public int Year { get; set; } = DateTime.Now.Year;
    public int Month { get; set; } = DateTime.Now.Month;
    public string ReportType { get; set; } = "BudgetVsActual";
    public int? CategoryId { get; set; }
    public int? AccountId { get; set; }
}

public class ChartDataViewModel
{
    public List<string> Labels { get; set; } = new();
    public List<ChartDataset> Datasets { get; set; } = new();
}

public class ChartDataset
{
    public string Label { get; set; } = string.Empty;
    public List<decimal> Data { get; set; } = new();
    public string BackgroundColor { get; set; } = string.Empty;
    public string BorderColor { get; set; } = string.Empty;
}

// Sankey Chart Data Models
public class SankeyChartData
{
    public List<SankeyNode> Nodes { get; set; } = new();
    public List<SankeyLink> Links { get; set; } = new();
}

public class SankeyNode
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}

public class SankeyLink
{
    public string Source { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Color { get; set; } = string.Empty;
}

// Daily Spending Trend Data
public class DailySpendingDataPoint
{
    public DateTime Date { get; set; }
    public string DateLabel { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public Dictionary<string, decimal> CategoryBreakdown { get; set; } = new();
}

public class DailySpendingTrendData
{
    public List<string> Labels { get; set; } = new();
    public List<decimal> DailyTotals { get; set; } = new();
    public decimal DailyTarget { get; set; }
    public decimal AverageSpending { get; set; }
    public List<string> Categories { get; set; } = new();
    public Dictionary<string, List<decimal>> CategoryData { get; set; } = new();
}

// Spending Calendar Data
public class SpendingCalendarData
{
    public int Year { get; set; }
    public int Month { get; set; }
    public List<CalendarDayData> Days { get; set; } = new();
    public decimal MaxDailySpend { get; set; }
}

public class CalendarDayData
{
    public int Day { get; set; }
    public DateTime Date { get; set; }
    public decimal TotalSpent { get; set; }
    public int TransactionCount { get; set; }
    public string IntensityClass { get; set; } = string.Empty;
}
