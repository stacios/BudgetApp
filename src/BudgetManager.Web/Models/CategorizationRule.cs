using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BudgetManager.Web.Models;

public class CategorizationRule
{
    public int Id { get; set; }
    
    [Required]
    public int Priority { get; set; } = 100;
    
    [Required]
    [MaxLength(200)]
    public string ContainsText { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Foreign keys
    public int CategoryId { get; set; }
    
    // Navigation properties
    [ForeignKey(nameof(CategoryId))]
    public virtual Category? Category { get; set; }
}
