using System.ComponentModel.DataAnnotations;

namespace BudgetManager.Web.Models;

public class Category
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public virtual ICollection<MonthlyBudget> MonthlyBudgets { get; set; } = new List<MonthlyBudget>();
    public virtual ICollection<CategorizationRule> CategorizationRules { get; set; } = new List<CategorizationRule>();
}
