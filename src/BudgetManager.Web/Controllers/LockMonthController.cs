using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BudgetManager.Web.Data;
using BudgetManager.Web.Services.Interfaces;
using BudgetManager.Web.ViewModels;

namespace BudgetManager.Web.Controllers;

[Authorize]
public class LockMonthController : Controller
{
    private readonly ILockingService _lockingService;
    private readonly ApplicationDbContext _context;
    
    public LockMonthController(ILockingService lockingService, ApplicationDbContext context)
    {
        _lockingService = lockingService;
        _context = context;
    }
    
    public async Task<IActionResult> Index()
    {
        var lockedMonths = await _lockingService.GetAllLockedMonthsAsync();
        
        var lockedViewModels = new List<LockMonthViewModel>();
        foreach (var lm in lockedMonths)
        {
            var transactionCount = await _context.Transactions
                .CountAsync(t => t.Date.Year == lm.Year && t.Date.Month == lm.Month);
            var totalExpenses = await _context.Transactions
                .Where(t => t.Date.Year == lm.Year && t.Date.Month == lm.Month && t.Amount < 0)
                .SumAsync(t => Math.Abs(t.Amount));
            
            lockedViewModels.Add(new LockMonthViewModel
            {
                Id = lm.Id,
                Year = lm.Year,
                Month = lm.Month,
                LockedAt = lm.LockedAt,
                LockedByUserName = lm.LockedByUser?.UserName ?? "Unknown",
                TransactionCount = transactionCount,
                TotalExpenses = totalExpenses
            });
        }
        
        // Get unlocked months with transactions
        var monthsWithTransactions = await _context.Transactions
            .GroupBy(t => new { t.Date.Year, t.Date.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count(), Total = g.Where(t => t.Amount < 0).Sum(t => Math.Abs(t.Amount)) })
            .ToListAsync();
        
        var lockedKeys = lockedMonths.Select(lm => (lm.Year, lm.Month)).ToHashSet();
        
        var unlockedViewModels = monthsWithTransactions
            .Where(m => !lockedKeys.Contains((m.Year, m.Month)))
            .OrderByDescending(m => m.Year)
            .ThenByDescending(m => m.Month)
            .Select(m => new UnlockedMonthViewModel
            {
                Year = m.Year,
                Month = m.Month,
                TransactionCount = m.Count,
                TotalExpenses = m.Total,
                CanLock = m.Year < DateTime.Now.Year || (m.Year == DateTime.Now.Year && m.Month < DateTime.Now.Month)
            })
            .ToList();
        
        var viewModel = new LockMonthListViewModel
        {
            LockedMonths = lockedViewModels,
            UnlockedMonths = unlockedViewModels
        };
        
        return View(viewModel);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Lock(int year, int month)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (string.IsNullOrEmpty(userId))
        {
            TempData["Error"] = "User not found.";
            return RedirectToAction(nameof(Index));
        }
        
        var result = await _lockingService.LockMonthAsync(year, month, userId);
        
        if (result.Success)
        {
            TempData["Success"] = result.Message;
        }
        else
        {
            TempData["Error"] = result.Message;
        }
        
        return RedirectToAction(nameof(Index));
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(int year, int month)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (string.IsNullOrEmpty(userId))
        {
            TempData["Error"] = "User not found.";
            return RedirectToAction(nameof(Index));
        }
        
        var result = await _lockingService.UnlockMonthAsync(year, month, userId);
        
        if (result.Success)
        {
            TempData["Success"] = result.Message;
        }
        else
        {
            TempData["Error"] = result.Message;
        }
        
        return RedirectToAction(nameof(Index));
    }
}
