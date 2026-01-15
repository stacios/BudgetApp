using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BudgetManager.Web.Models;

public class MonthlyBudget
{
    public int Id { get; set; }
    
    [Required]
    [Range(2000, 2100)]
    public int Year { get; set; }
    
    [Required]
    [Range(1, 12)]
    public int Month { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal BudgetAmount { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Foreign keys
    public int CategoryId { get; set; }
    
    // Navigation properties
    [ForeignKey(nameof(CategoryId))]
    public virtual Category? Category { get; set; }
}
