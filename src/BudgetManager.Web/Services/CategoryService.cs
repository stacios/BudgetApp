using Microsoft.EntityFrameworkCore;
using BudgetManager.Web.Data;
using BudgetManager.Web.Models;
using BudgetManager.Web.Services.Interfaces;

namespace BudgetManager.Web.Services;

public class CategoryService : ICategoryService
{
    private readonly ApplicationDbContext _context;
    private readonly IActivityLogService _activityLogService;
    
    public CategoryService(ApplicationDbContext context, IActivityLogService activityLogService)
    {
        _context = context;
        _activityLogService = activityLogService;
    }
    
    public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
    {
        return await _context.Categories
            .OrderBy(c => c.Name)
            .ToListAsync();
    }
    
    public async Task<IEnumerable<Category>> GetActiveCategoriesAsync()
    {
        return await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }
    
    public async Task<Category?> GetCategoryByIdAsync(int id)
    {
        return await _context.Categories.FindAsync(id);
    }
    
    public async Task<Category?> GetCategoryByNameAsync(string name)
    {
        return await _context.Categories
            .FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower());
    }
    
    public async Task<Category> CreateCategoryAsync(Category category, string? userId = null)
    {
        category.CreatedAt = DateTime.UtcNow;
        
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        
        await _activityLogService.LogAsync(
            "Category",
            category.Id,
            "Create",
            $"Created category: {category.Name}",
            null,
            new { category.Name, category.Description, category.IsActive },
            userId);
        
        return category;
    }
    
    public async Task<Category> UpdateCategoryAsync(Category category, string? userId = null)
    {
        var existing = await _context.Categories.FindAsync(category.Id);
        if (existing == null)
        {
            throw new InvalidOperationException($"Category {category.Id} not found.");
        }
        
        var oldValues = new
        {
            existing.Name,
            existing.Description,
            existing.IsActive
        };
        
        existing.Name = category.Name;
        existing.Description = category.Description;
        existing.IsActive = category.IsActive;
        
        await _context.SaveChangesAsync();
        
        await _activityLogService.LogAsync(
            "Category",
            category.Id,
            "Update",
            $"Updated category: {category.Name}",
            oldValues,
            new { category.Name, category.Description, category.IsActive },
            userId);
        
        return existing;
    }
    
    public async Task<(bool Success, string Message)> DeleteCategoryAsync(int id, string? userId = null)
    {
        var category = await _context.Categories
            .Include(c => c.Transactions)
            .FirstOrDefaultAsync(c => c.Id == id);
        
        if (category == null)
        {
            return (false, "Category not found.");
        }
        
        if (category.Transactions.Any())
        {
            return (false, "Cannot delete category with existing transactions. Consider deactivating it instead.");
        }
        
        var oldValues = new
        {
            category.Name,
            category.Description
        };
        
        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
        
        await _activityLogService.LogAsync(
            "Category",
            id,
            "Delete",
            $"Deleted category: {category.Name}",
            oldValues,
            null,
            userId);
        
        return (true, "Category deleted successfully.");
    }
    
    public async Task<int?> GetDefaultCategoryIdAsync()
    {
        var uncategorized = await _context.Categories
            .FirstOrDefaultAsync(c => c.Name == "Uncategorized");
        
        return uncategorized?.Id;
    }
}
