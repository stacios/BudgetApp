using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using BudgetManager.Web.Models;
using BudgetManager.Web.Services.Interfaces;
using BudgetManager.Web.ViewModels;

namespace BudgetManager.Web.Controllers;

[Authorize]
public class RulesController : Controller
{
    private readonly IRuleService _ruleService;
    private readonly ICategoryService _categoryService;
    
    public RulesController(IRuleService ruleService, ICategoryService categoryService)
    {
        _ruleService = ruleService;
        _categoryService = categoryService;
    }
    
    public async Task<IActionResult> Index(bool showInactive = false)
    {
        var rules = showInactive
            ? await _ruleService.GetAllRulesAsync()
            : await _ruleService.GetActiveRulesOrderedByPriorityAsync();
        
        var ruleViewModels = rules.Select(r => new RuleViewModel
        {
            Id = r.Id,
            Priority = r.Priority,
            ContainsText = r.ContainsText,
            CategoryId = r.CategoryId,
            CategoryName = r.Category?.Name ?? "Unknown",
            IsActive = r.IsActive,
            CreatedAt = r.CreatedAt
        }).ToList();
        
        var viewModel = new RuleListViewModel
        {
            Rules = ruleViewModels,
            ShowInactive = showInactive
        };
        
        return View(viewModel);
    }
    
    public async Task<IActionResult> Create()
    {
        var categories = await _categoryService.GetActiveCategoriesAsync();
        
        var viewModel = new RuleViewModel
        {
            Priority = 100,
            IsActive = true,
            Categories = new SelectList(categories, "Id", "Name")
        };
        
        return View(viewModel);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RuleViewModel viewModel)
    {
        if (ModelState.IsValid)
        {
            var rule = new CategorizationRule
            {
                Priority = viewModel.Priority,
                ContainsText = viewModel.ContainsText,
                CategoryId = viewModel.CategoryId,
                IsActive = viewModel.IsActive
            };
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _ruleService.CreateRuleAsync(rule, userId);
            
            TempData["Success"] = "Rule created successfully.";
            return RedirectToAction(nameof(Index));
        }
        
        var categories = await _categoryService.GetActiveCategoriesAsync();
        viewModel.Categories = new SelectList(categories, "Id", "Name", viewModel.CategoryId);
        return View(viewModel);
    }
    
    public async Task<IActionResult> Edit(int id)
    {
        var rule = await _ruleService.GetRuleByIdAsync(id);
        if (rule == null)
        {
            return NotFound();
        }
        
        var categories = await _categoryService.GetActiveCategoriesAsync();
        
        var viewModel = new RuleViewModel
        {
            Id = rule.Id,
            Priority = rule.Priority,
            ContainsText = rule.ContainsText,
            CategoryId = rule.CategoryId,
            IsActive = rule.IsActive,
            Categories = new SelectList(categories, "Id", "Name", rule.CategoryId)
        };
        
        return View(viewModel);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, RuleViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            return NotFound();
        }
        
        if (ModelState.IsValid)
        {
            var rule = new CategorizationRule
            {
                Id = viewModel.Id,
                Priority = viewModel.Priority,
                ContainsText = viewModel.ContainsText,
                CategoryId = viewModel.CategoryId,
                IsActive = viewModel.IsActive
            };
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _ruleService.UpdateRuleAsync(rule, userId);
            
            TempData["Success"] = "Rule updated successfully.";
            return RedirectToAction(nameof(Index));
        }
        
        var categories = await _categoryService.GetActiveCategoriesAsync();
        viewModel.Categories = new SelectList(categories, "Id", "Name", viewModel.CategoryId);
        return View(viewModel);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await _ruleService.DeleteRuleAsync(id, userId);
        
        TempData["Success"] = "Rule deleted successfully.";
        return RedirectToAction(nameof(Index));
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reorder(List<RuleOrderItem> rules)
    {
        var priorities = rules.Select((r, index) => (r.Id, index + 1)).ToList();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        await _ruleService.ReorderRulesAsync(priorities, userId);
        
        return Json(new { success = true });
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyRules()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var count = await _ruleService.ApplyRulesToUncategorizedTransactionsAsync(userId);
        
        if (count > 0)
        {
            TempData["Success"] = $"Applied rules to {count} transactions.";
        }
        else
        {
            TempData["Info"] = "No uncategorized transactions found to process.";
        }
        
        return RedirectToAction(nameof(Index));
    }
}
