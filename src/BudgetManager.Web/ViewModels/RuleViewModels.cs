using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BudgetManager.Web.ViewModels;

public class RuleViewModel
{
    public int Id { get; set; }
    
    [Required]
    public int Priority { get; set; } = 100;
    
    [Required]
    [MaxLength(200)]
    [Display(Name = "Contains Text")]
    public string ContainsText { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }
    
    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
    
    // Display properties
    public string? CategoryName { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Dropdown
    public SelectList? Categories { get; set; }
}

public class RuleListViewModel
{
    public IEnumerable<RuleViewModel> Rules { get; set; } = new List<RuleViewModel>();
    public bool ShowInactive { get; set; }
}

public class RuleReorderViewModel
{
    public List<RuleOrderItem> Rules { get; set; } = new();
}

public class RuleOrderItem
{
    public int Id { get; set; }
    public int Priority { get; set; }
    public string ContainsText { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
}
