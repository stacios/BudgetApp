using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BudgetManager.Web.Data;
using BudgetManager.Web.Services.Interfaces;
using BudgetManager.Web.ViewModels;

namespace BudgetManager.Web.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly IBudgetService _budgetService;
    private readonly ITransactionService _transactionService;
    private readonly ApplicationDbContext _context;
    
    public ReportsController(
        IBudgetService budgetService,
        ITransactionService transactionService,
        ApplicationDbContext context)
    {
        _budgetService = budgetService;
        _transactionService = transactionService;
        _context = context;
    }
    
    public IActionResult Index()
    {
        return View();
    }
    
    public async Task<IActionResult> BudgetVsActual(int? year, int? month)
    {
        var targetYear = year ?? DateTime.Now.Year;
        var targetMonth = month ?? DateTime.Now.Month;
        
        var summary = await _budgetService.GetBudgetSummaryAsync(targetYear, targetMonth);
        
        var viewModel = new BudgetVsActualReportViewModel
        {
            Year = targetYear,
            Month = targetMonth,
            Categories = summary.Select(s => new BudgetVsActualCategoryViewModel
            {
                CategoryId = s.CategoryId,
                CategoryName = s.CategoryName,
                Budget = s.BudgetAmount,
                Actual = s.SpentAmount
            }).Where(c => c.Budget > 0 || c.Actual > 0).ToList(),
            TotalBudget = summary.Sum(s => s.BudgetAmount),
            TotalActual = summary.Sum(s => s.SpentAmount)
        };
        
        return View(viewModel);
    }
    
    public async Task<IActionResult> MonthOverMonth(int? year)
    {
        var targetYear = year ?? DateTime.Now.Year;
        
        var dataPoints = new List<MonthOverMonthDataPoint>();
        
        for (int month = 1; month <= 12; month++)
        {
            var transactions = await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.Date.Year == targetYear && t.Date.Month == month)
                .ToListAsync();
            
            if (!transactions.Any()) continue;
            
            var categoryTotals = transactions
                .Where(t => t.Amount < 0)
                .GroupBy(t => t.Category?.Name ?? "Uncategorized")
                .ToDictionary(g => g.Key, g => Math.Abs(g.Sum(t => t.Amount)));
            
            dataPoints.Add(new MonthOverMonthDataPoint
            {
                Year = targetYear,
                Month = month,
                TotalIncome = transactions.Where(t => t.Amount > 0).Sum(t => t.Amount),
                TotalExpenses = Math.Abs(transactions.Where(t => t.Amount < 0).Sum(t => t.Amount)),
                CategoryTotals = categoryTotals
            });
        }
        
        var allCategories = dataPoints
            .SelectMany(dp => dp.CategoryTotals.Keys)
            .Distinct()
            .OrderBy(c => c)
            .ToList();
        
        var viewModel = new MonthOverMonthReportViewModel
        {
            Year = targetYear,
            DataPoints = dataPoints,
            Categories = allCategories
        };
        
        return View(viewModel);
    }
    
    public async Task<IActionResult> TopExpenses(int? year, int? month, int count = 10)
    {
        var targetYear = year ?? DateTime.Now.Year;
        var targetMonth = month ?? DateTime.Now.Month;
        
        var expenses = await _transactionService.GetTopExpensesAsync(targetYear, targetMonth, count);
        
        var viewModel = new TopExpensesReportViewModel
        {
            Year = targetYear,
            Month = targetMonth,
            TopCount = count,
            Expenses = expenses,
            TotalAmount = expenses.Sum(e => e.Amount)
        };
        
        return View(viewModel);
    }
    
    [HttpGet]
    public async Task<IActionResult> GetBudgetVsActualChart(int year, int month)
    {
        var summary = await _budgetService.GetBudgetSummaryAsync(year, month);
        var filtered = summary.Where(s => s.BudgetAmount > 0 || s.SpentAmount > 0).ToList();
        
        var chartData = new ChartDataViewModel
        {
            Labels = filtered.Select(s => s.CategoryName).ToList(),
            Datasets = new List<ChartDataset>
            {
                new ChartDataset
                {
                    Label = "Budget",
                    Data = filtered.Select(s => s.BudgetAmount).ToList(),
                    BackgroundColor = "rgba(54, 162, 235, 0.5)",
                    BorderColor = "rgba(54, 162, 235, 1)"
                },
                new ChartDataset
                {
                    Label = "Actual",
                    Data = filtered.Select(s => s.SpentAmount).ToList(),
                    BackgroundColor = "rgba(255, 99, 132, 0.5)",
                    BorderColor = "rgba(255, 99, 132, 1)"
                }
            }
        };
        
        return Json(chartData);
    }
    
    [HttpGet]
    public async Task<IActionResult> GetMonthOverMonthChart(int year)
    {
        var dataPoints = new List<MonthOverMonthDataPoint>();
        
        for (int month = 1; month <= 12; month++)
        {
            var transactions = await _context.Transactions
                .Where(t => t.Date.Year == year && t.Date.Month == month)
                .ToListAsync();
            
            dataPoints.Add(new MonthOverMonthDataPoint
            {
                Year = year,
                Month = month,
                TotalIncome = transactions.Where(t => t.Amount > 0).Sum(t => t.Amount),
                TotalExpenses = Math.Abs(transactions.Where(t => t.Amount < 0).Sum(t => t.Amount))
            });
        }
        
        var chartData = new ChartDataViewModel
        {
            Labels = dataPoints.Select(dp => dp.MonthName).ToList(),
            Datasets = new List<ChartDataset>
            {
                new ChartDataset
                {
                    Label = "Income",
                    Data = dataPoints.Select(dp => dp.TotalIncome).ToList(),
                    BackgroundColor = "rgba(75, 192, 192, 0.5)",
                    BorderColor = "rgba(75, 192, 192, 1)"
                },
                new ChartDataset
                {
                    Label = "Expenses",
                    Data = dataPoints.Select(dp => dp.TotalExpenses).ToList(),
                    BackgroundColor = "rgba(255, 99, 132, 0.5)",
                    BorderColor = "rgba(255, 99, 132, 1)"
                }
            }
        };
        
        return Json(chartData);
    }
}
