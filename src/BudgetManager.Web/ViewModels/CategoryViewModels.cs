using System.ComponentModel.DataAnnotations;

namespace BudgetManager.Web.ViewModels;

public class CategoryViewModel
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
    
    // Display properties
    public DateTime CreatedAt { get; set; }
    public int TransactionCount { get; set; }
    public decimal TotalSpent { get; set; }
}

public class CategoryListViewModel
{
    public IEnumerable<CategoryViewModel> Categories { get; set; } = new List<CategoryViewModel>();
    public bool ShowInactive { get; set; }
}
