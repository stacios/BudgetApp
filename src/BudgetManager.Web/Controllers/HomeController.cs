using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BudgetManager.Web.Data;
using BudgetManager.Web.Services.Interfaces;
using BudgetManager.Web.ViewModels;

namespace BudgetManager.Web.Controllers;

[AllowAnonymous]
public class HomeController : Controller
{
    private readonly IBudgetService _budgetService;
    private readonly ITransactionService _transactionService;
    private readonly ILockingService _lockingService;
    private readonly ApplicationDbContext _context;
    
    public HomeController(
        IBudgetService budgetService,
        ITransactionService transactionService,
        ILockingService lockingService,
        ApplicationDbContext context)
    {
        _budgetService = budgetService;
        _transactionService = transactionService;
        _lockingService = lockingService;
        _context = context;
    }
    
    public async Task<IActionResult> Index(int? year, int? month)
    {
        var targetYear = year ?? DateTime.Now.Year;
        var targetMonth = month ?? DateTime.Now.Month;
        
        var pacing = await _budgetService.GetBudgetPacingAsync(targetYear, targetMonth);
        var topExpenses = await _transactionService.GetTopExpensesAsync(targetYear, targetMonth, 5);
        var isLocked = await _lockingService.IsMonthLockedAsync(targetYear, targetMonth);
        
        // Get recent transactions
        var recentTransactions = await _context.Transactions
            .Include(t => t.Category)
            .Include(t => t.Account)
            .Where(t => t.Date.Year == targetYear && t.Date.Month == targetMonth)
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedAt)
            .Take(10)
            .Select(t => new TransactionViewModel
            {
                Id = t.Id,
                Date = t.Date,
                Description = t.Description,
                Amount = t.Amount,
                CategoryName = t.Category != null ? t.Category.Name : "Uncategorized",
                AccountName = t.Account != null ? t.Account.Name : "Unknown",
                IsAdjustment = t.IsAdjustment
            })
            .ToListAsync();
        
        // Get total income and expenses
        var transactions = await _context.Transactions
            .Where(t => t.Date.Year == targetYear && t.Date.Month == targetMonth)
            .ToListAsync();
        
        var totalIncome = transactions.Where(t => t.Amount > 0).Sum(t => t.Amount);
        var totalExpenses = transactions.Where(t => t.Amount < 0).Sum(t => -t.Amount);
        
        // Get uncategorized count
        var uncategorizedId = await _context.Categories
            .Where(c => c.Name == "Uncategorized")
            .Select(c => c.Id)
            .FirstOrDefaultAsync();
        
        var uncategorizedCount = uncategorizedId > 0
            ? await _context.Transactions.CountAsync(t => t.CategoryId == uncategorizedId)
            : 0;
        
        var viewModel = new DashboardViewModel
        {
            Year = targetYear,
            Month = targetMonth,
            TotalIncome = totalIncome,
            TotalExpenses = totalExpenses,
            TotalBudget = pacing.TotalBudget,
            TotalSpent = pacing.TotalSpent,
            ExpectedSpentToDate = pacing.ExpectedToDate,
            SafeToSpendToday = pacing.SafeToSpendToday,
            DaysRemaining = pacing.RemainingDays,
            OverallStatus = pacing.OverallStatus,
            TopCategories = pacing.CategorySummaries.OrderByDescending(c => c.SpentAmount).Take(5),
            OverBudgetCategories = pacing.CategorySummaries.Where(c => c.Status == BudgetStatus.OVER),
            RecentTransactions = recentTransactions,
            TopExpenses = topExpenses,
            IsMonthLocked = isLocked,
            UncategorizedCount = uncategorizedCount
        };
        
        return View(viewModel);
    }
    
    public IActionResult Privacy()
    {
        return View();
    }
    
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
    
    // ========================================================================
    // Chart API Endpoints (from data_viz.ipynb visualizations)
    // ========================================================================
    
    /// <summary>
    /// Spending by Category - Horizontal Bar Chart Data
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSpendingByCategory(int? year, int? month)
    {
        var targetYear = year ?? DateTime.Now.Year;
        var targetMonth = month ?? DateTime.Now.Month;
        
        var categorySpending = await _context.Transactions
            .Include(t => t.Category)
            .Where(t => t.Date.Year == targetYear && t.Date.Month == targetMonth && t.Amount < 0)
            .GroupBy(t => t.Category != null ? t.Category.Name : "Uncategorized")
            .Select(g => new { Category = g.Key, Amount = -g.Sum(t => t.Amount) })
            .OrderByDescending(x => x.Amount)
            .ToListAsync();
        
        var colors = new[] { "#111111", "#2B2B2B", "#444444", "#666666", "#888888", "#AAAAAA", "#C0C0C0" };
        
        var chartData = new
        {
            labels = categorySpending.Select(x => x.Category).ToList(),
            datasets = new[]
            {
                new
                {
                    label = "Spending",
                    data = categorySpending.Select(x => x.Amount).ToList(),
                    backgroundColor = categorySpending.Select((_, i) => colors[i % colors.Length]).ToList(),
                    borderColor = "#000000",
                    borderWidth = 1
                }
            }
        };
        
        return Json(chartData);
    }
    
    /// <summary>
    /// Daily Spending Trend - Last 14 Days Line Chart with Target
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDailySpendingTrend(int? year, int? month)
    {
        var targetYear = year ?? DateTime.Now.Year;
        var targetMonth = month ?? DateTime.Now.Month;
        var targetDate = new DateTime(targetYear, targetMonth, DateTime.DaysInMonth(targetYear, targetMonth));
        var startDate = targetDate.AddDays(-13);
        
        var dailySpending = await _context.Transactions
            .Include(t => t.Category)
            .Where(t => t.Date >= startDate && t.Date <= targetDate && t.Amount < 0)
            .GroupBy(t => t.Date.Date)
            .Select(g => new { Date = g.Key, Total = -g.Sum(t => t.Amount) })
            .ToListAsync();
        
        // Fill in missing days with zero
        var allDays = Enumerable.Range(0, 14)
            .Select(i => startDate.AddDays(i))
            .ToList();
        
        var dailyData = allDays.Select(d => new
        {
            Date = d,
            Label = d.ToString("MM/dd"),
            Amount = dailySpending.FirstOrDefault(x => x.Date == d)?.Total ?? 0m
        }).ToList();
        
        // Calculate daily budget target
        var monthlyBudget = await _context.MonthlyBudgets
            .Where(b => b.Year == targetYear && b.Month == targetMonth)
            .SumAsync(b => b.BudgetAmount);
        var dailyTarget = monthlyBudget / DateTime.DaysInMonth(targetYear, targetMonth);
        var averageSpending = dailyData.Where(d => d.Amount > 0).Select(d => d.Amount).DefaultIfEmpty(0).Average();
        
        var chartData = new
        {
            labels = dailyData.Select(d => d.Label).ToList(),
            datasets = new object[]
            {
                new
                {
                    label = "Daily Spending",
                    data = dailyData.Select(d => d.Amount).ToList(),
                    backgroundColor = "rgba(17, 17, 17, 0.6)",
                    borderColor = "#111111",
                    borderWidth = 2,
                    type = "bar"
                },
                new
                {
                    label = $"Daily Target (${dailyTarget:N0})",
                    data = dailyData.Select(_ => dailyTarget).ToList(),
                    borderColor = "#FF6384",
                    borderWidth = 2,
                    borderDash = new[] { 5, 5 },
                    fill = false,
                    type = "line",
                    pointRadius = 0
                },
                new
                {
                    label = $"Average (${averageSpending:N0})",
                    data = dailyData.Select(_ => averageSpending).ToList(),
                    borderColor = "#36A2EB",
                    borderWidth = 2,
                    borderDash = new[] { 2, 2 },
                    fill = false,
                    type = "line",
                    pointRadius = 0
                }
            },
            dailyTarget = dailyTarget,
            averageSpending = averageSpending
        };
        
        return Json(chartData);
    }
    
    /// <summary>
    /// Cash Flow Sankey Diagram Data
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCashFlowSankey(int? year, int? month)
    {
        var targetYear = year ?? DateTime.Now.Year;
        var targetMonth = month ?? DateTime.Now.Month;
        
        var transactions = await _context.Transactions
            .Include(t => t.Category)
            .Where(t => t.Date.Year == targetYear && t.Date.Month == targetMonth)
            .ToListAsync();
        
        var totalIncome = transactions.Where(t => t.Amount > 0).Sum(t => t.Amount);
        var totalExpenses = transactions.Where(t => t.Amount < 0).Sum(t => -t.Amount);
        var savings = totalIncome - totalExpenses;
        
        // Get spending by category - limit to top 6 categories to fit in the chart
        var categorySpending = transactions
            .Where(t => t.Amount < 0)
            .GroupBy(t => t.Category?.Name ?? "Uncategorized")
            .Select(g => new { Category = g.Key, Amount = -g.Sum(t => t.Amount) })
            .OrderByDescending(x => x.Amount)
            .Take(6)
            .ToList();
        
        // Calculate "Other" category for remaining spending
        var topCategoriesTotal = categorySpending.Sum(c => c.Amount);
        var otherAmount = totalExpenses - topCategoriesTotal;
        
        var colors = new[] { "#3b82f6", "#10b981", "#f59e0b", "#ef4444", "#8b5cf6", "#ec4899", "#64748b" };
        
        // Build nodes - simplified: Income -> Categories (+ Savings if positive)
        var nodes = new List<object> { new { id = "income", label = $"Income (${totalIncome:N0})", color = "#10b981" } };
        var categoryIndex = 0;
        foreach (var cat in categorySpending)
        {
            nodes.Add(new { id = $"cat_{cat.Category}", label = $"{cat.Category}", color = colors[categoryIndex % colors.Length] });
            categoryIndex++;
        }
        
        // Add "Other" node if there's remaining spending
        if (otherAmount > 1)
        {
            nodes.Add(new { id = "cat_Other", label = "Other", color = colors[categoryIndex % colors.Length] });
        }
        
        // Add savings node if positive
        if (savings > 0)
        {
            nodes.Add(new { id = "savings", label = $"Savings", color = "#10b981" });
        }
        
        // Build links - Income to each category
        var links = new List<object>();
        categoryIndex = 0;
        foreach (var cat in categorySpending)
        {
            links.Add(new
            {
                source = "income",
                target = $"cat_{cat.Category}",
                value = (double)cat.Amount
            });
            categoryIndex++;
        }
        
        // Add "Other" link if there's remaining spending
        if (otherAmount > 1)
        {
            links.Add(new
            {
                source = "income",
                target = "cat_Other",
                value = (double)otherAmount
            });
        }
        
        // Add savings link
        if (savings > 0)
        {
            links.Add(new
            {
                source = "income",
                target = "savings",
                value = (double)savings
            });
        }
        
        return Json(new { nodes, links, totalIncome = (double)totalIncome, totalExpenses = (double)totalExpenses, savings = (double)savings });
    }
    
    /// <summary>
    /// Spending Calendar Heatmap Data
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSpendingCalendar(int? year, int? month)
    {
        var targetYear = year ?? DateTime.Now.Year;
        var targetMonth = month ?? DateTime.Now.Month;
        
        var firstDay = new DateTime(targetYear, targetMonth, 1);
        var lastDay = new DateTime(targetYear, targetMonth, DateTime.DaysInMonth(targetYear, targetMonth));
        
        var dailySpending = await _context.Transactions
            .Where(t => t.Date >= firstDay && t.Date <= lastDay && t.Amount < 0)
            .GroupBy(t => t.Date.Day)
            .Select(g => new { Day = g.Key, Amount = -g.Sum(t => t.Amount), Count = g.Count() })
            .ToListAsync();
        
        var maxSpend = dailySpending.Any() ? dailySpending.Max(d => d.Amount) : 0m;
        
        var calendarData = new List<object>();
        for (int day = 1; day <= DateTime.DaysInMonth(targetYear, targetMonth); day++)
        {
            var dayData = dailySpending.FirstOrDefault(d => d.Day == day);
            var spent = dayData?.Amount ?? 0m;
            var count = dayData?.Count ?? 0;
            
            // Calculate intensity (0-4 scale for heatmap)
            var intensity = maxSpend > 0 ? (int)Math.Min(4, Math.Ceiling((spent / maxSpend) * 4)) : 0;
            
            calendarData.Add(new
            {
                day,
                date = new DateTime(targetYear, targetMonth, day).ToString("yyyy-MM-dd"),
                dayOfWeek = (int)new DateTime(targetYear, targetMonth, day).DayOfWeek,
                spent,
                count,
                intensity,
                intensityClass = intensity switch
                {
                    0 => "bg-light",
                    1 => "bg-success bg-opacity-25",
                    2 => "bg-warning bg-opacity-50",
                    3 => "bg-danger bg-opacity-50",
                    4 => "bg-danger",
                    _ => "bg-light"
                }
            });
        }
        
        return Json(new
        {
            year = targetYear,
            month = targetMonth,
            monthName = firstDay.ToString("MMMM yyyy"),
            firstDayOfWeek = (int)firstDay.DayOfWeek,
            days = calendarData,
            maxSpend,
            totalSpent = dailySpending.Sum(d => d.Amount)
        });
    }
    
    /// <summary>
    /// Weekly Progress Bar Data
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetWeeklyProgress(int? year, int? month)
    {
        var targetYear = year ?? DateTime.Now.Year;
        var targetMonth = month ?? DateTime.Now.Month;
        
        var today = DateTime.Today;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var weekEnd = weekStart.AddDays(6);
        
        var weeklySpending = await _context.Transactions
            .Where(t => t.Date >= weekStart && t.Date <= weekEnd && t.Amount < 0)
            .SumAsync(t => -t.Amount);
        
        var monthlyBudget = await _context.MonthlyBudgets
            .Where(b => b.Year == targetYear && b.Month == targetMonth)
            .SumAsync(b => b.BudgetAmount);
        
        var weeklyBudget = monthlyBudget / 4; // Approximate weekly budget
        var percentUsed = weeklyBudget > 0 ? (weeklySpending / weeklyBudget) * 100 : 0;
        
        return Json(new
        {
            weekStart = weekStart.ToString("MMM dd"),
            weekEnd = weekEnd.ToString("MMM dd"),
            spent = weeklySpending,
            budget = weeklyBudget,
            remaining = weeklyBudget - weeklySpending,
            percentUsed,
            status = percentUsed > 100 ? "over" : percentUsed > 80 ? "warning" : "ok"
        });
    }
}
