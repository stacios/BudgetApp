using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BudgetManager.Web.Data;
using BudgetManager.Web.Services.Interfaces;
using BudgetManager.Web.ViewModels;

namespace BudgetManager.Web.Controllers;

[Authorize]
public class BudgetsController : Controller
{
    private readonly IBudgetService _budgetService;
    private readonly ICategoryService _categoryService;
    private readonly ApplicationDbContext _context;
    
    public BudgetsController(
        IBudgetService budgetService,
        ICategoryService categoryService,
        ApplicationDbContext context)
    {
        _budgetService = budgetService;
        _categoryService = categoryService;
        _context = context;
    }
    
    public async Task<IActionResult> Index(int? year, int? month)
    {
        var targetYear = year ?? DateTime.Now.Year;
        var targetMonth = month ?? DateTime.Now.Month;
        
        var pacing = await _budgetService.GetBudgetPacingAsync(targetYear, targetMonth);
        
        ViewBag.Year = targetYear;
        ViewBag.Month = targetMonth;
        ViewBag.MonthName = new DateTime(targetYear, targetMonth, 1).ToString("MMMM yyyy");
        
        return View(pacing);
    }
    
    public async Task<IActionResult> Edit(int? year, int? month)
    {
        var targetYear = year ?? DateTime.Now.Year;
        var targetMonth = month ?? DateTime.Now.Month;
        
        var categories = await _categoryService.GetActiveCategoriesAsync();
        var budgets = await _budgetService.GetBudgetsForMonthAsync(targetYear, targetMonth);
        var budgetDict = budgets.ToDictionary(b => b.CategoryId, b => b.BudgetAmount);
        
        // Get previous month data for reference
        var prevMonth = targetMonth == 1 ? 12 : targetMonth - 1;
        var prevYear = targetMonth == 1 ? targetYear - 1 : targetYear;
        
        var prevBudgets = await _budgetService.GetBudgetsForMonthAsync(prevYear, prevMonth);
        var prevBudgetDict = prevBudgets.ToDictionary(b => b.CategoryId, b => b.BudgetAmount);
        
        var prevSpent = await _context.Transactions
            .Where(t => t.Date.Year == prevYear && t.Date.Month == prevMonth && t.Amount < 0)
            .GroupBy(t => t.CategoryId)
            .Select(g => new { CategoryId = g.Key, Total = g.Sum(t => Math.Abs(t.Amount)) })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Total);
        
        var categoryEdits = categories.Select(c => new BudgetCategoryEditViewModel
        {
            CategoryId = c.Id,
            CategoryName = c.Name,
            BudgetAmount = budgetDict.TryGetValue(c.Id, out var b) ? b : 0,
            PreviousMonthBudget = prevBudgetDict.TryGetValue(c.Id, out var pb) ? pb : null,
            PreviousMonthSpent = prevSpent.TryGetValue(c.Id, out var ps) ? ps : null
        }).ToList();
        
        var viewModel = new BudgetEditViewModel
        {
            Year = targetYear,
            Month = targetMonth,
            Categories = categoryEdits
        };
        
        return View(viewModel);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int year, int month, Dictionary<int, decimal> budgets)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        foreach (var (categoryId, amount) in budgets)
        {
            if (amount >= 0)
            {
                await _budgetService.CreateOrUpdateBudgetAsync(year, month, categoryId, amount, userId);
            }
        }
        
        TempData["Success"] = "Budgets updated successfully.";
        return RedirectToAction(nameof(Index), new { year, month });
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CopyFromPrevious(int year, int month)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await _budgetService.CopyBudgetsFromPreviousMonthAsync(year, month, userId);
        
        TempData["Success"] = "Budgets copied from previous month.";
        return RedirectToAction(nameof(Edit), new { year, month });
    }
    
    [HttpGet]
    public async Task<IActionResult> GetSummary(int year, int month)
    {
        var summary = await _budgetService.GetBudgetSummaryAsync(year, month);
        return Json(summary);
    }
}
