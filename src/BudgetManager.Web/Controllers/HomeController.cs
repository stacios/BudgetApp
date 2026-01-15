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
        var totalExpenses = transactions.Where(t => t.Amount < 0).Sum(t => Math.Abs(t.Amount));
        
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
}
