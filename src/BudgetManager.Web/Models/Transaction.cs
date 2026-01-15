using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BudgetManager.Web.Models;

public class Transaction
{
    public int Id { get; set; }
    
    [Required]
    public DateTime Date { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }
    
    [MaxLength(1000)]
    public string? Notes { get; set; }
    
    public bool IsAdjustment { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Foreign keys
    public int CategoryId { get; set; }
    public int AccountId { get; set; }
    public string? UserId { get; set; }
    
    // Navigation properties
    [ForeignKey(nameof(CategoryId))]
    public virtual Category? Category { get; set; }
    
    [ForeignKey(nameof(AccountId))]
    public virtual Account? Account { get; set; }
    
    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser? User { get; set; }
}
