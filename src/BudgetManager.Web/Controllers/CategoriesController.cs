using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BudgetManager.Web.Data;
using BudgetManager.Web.Models;
using BudgetManager.Web.Services.Interfaces;
using BudgetManager.Web.ViewModels;

namespace BudgetManager.Web.Controllers;

[Authorize]
public class CategoriesController : Controller
{
    private readonly ICategoryService _categoryService;
    private readonly ApplicationDbContext _context;
    
    public CategoriesController(ICategoryService categoryService, ApplicationDbContext context)
    {
        _categoryService = categoryService;
        _context = context;
    }
    
    public async Task<IActionResult> Index(bool showInactive = false)
    {
        var categories = showInactive
            ? await _categoryService.GetAllCategoriesAsync()
            : await _categoryService.GetActiveCategoriesAsync();
        
        var categoryViewModels = new List<CategoryViewModel>();
        foreach (var c in categories)
        {
            var transactionCount = await _context.Transactions.CountAsync(t => t.CategoryId == c.Id);
            var totalSpent = await _context.Transactions
                .Where(t => t.CategoryId == c.Id && t.Amount < 0)
                .SumAsync(t => -t.Amount);
            
            categoryViewModels.Add(new CategoryViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt,
                TransactionCount = transactionCount,
                TotalSpent = totalSpent
            });
        }
        
        var viewModel = new CategoryListViewModel
        {
            Categories = categoryViewModels,
            ShowInactive = showInactive
        };
        
        return View(viewModel);
    }
    
    public IActionResult Create()
    {
        return View(new CategoryViewModel());
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryViewModel viewModel)
    {
        if (ModelState.IsValid)
        {
            var category = new Category
            {
                Name = viewModel.Name,
                Description = viewModel.Description,
                IsActive = viewModel.IsActive
            };
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _categoryService.CreateCategoryAsync(category, userId);
            
            TempData["Success"] = "Category created successfully.";
            return RedirectToAction(nameof(Index));
        }
        
        return View(viewModel);
    }
    
    public async Task<IActionResult> Edit(int id)
    {
        var category = await _categoryService.GetCategoryByIdAsync(id);
        if (category == null)
        {
            return NotFound();
        }
        
        var viewModel = new CategoryViewModel
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            IsActive = category.IsActive
        };
        
        return View(viewModel);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CategoryViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            return NotFound();
        }
        
        if (ModelState.IsValid)
        {
            var category = new Category
            {
                Id = viewModel.Id,
                Name = viewModel.Name,
                Description = viewModel.Description,
                IsActive = viewModel.IsActive
            };
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _categoryService.UpdateCategoryAsync(category, userId);
            
            TempData["Success"] = "Category updated successfully.";
            return RedirectToAction(nameof(Index));
        }
        
        return View(viewModel);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _categoryService.DeleteCategoryAsync(id, userId);
        
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
