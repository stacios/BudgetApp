using BudgetManager.Web.Models;

namespace BudgetManager.Web.Services.Interfaces;

public interface ICategoryService
{
    Task<IEnumerable<Category>> GetAllCategoriesAsync();
    
    Task<IEnumerable<Category>> GetActiveCategoriesAsync();
    
    Task<Category?> GetCategoryByIdAsync(int id);
    
    Task<Category?> GetCategoryByNameAsync(string name);
    
    Task<Category> CreateCategoryAsync(Category category, string? userId = null);
    
    Task<Category> UpdateCategoryAsync(Category category, string? userId = null);
    
    Task<(bool Success, string Message)> DeleteCategoryAsync(int id, string? userId = null);
    
    Task<int?> GetDefaultCategoryIdAsync();
}
