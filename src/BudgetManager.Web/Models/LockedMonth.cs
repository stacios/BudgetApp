using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BudgetManager.Web.Models;

public class LockedMonth
{
    public int Id { get; set; }
    
    [Required]
    [Range(2000, 2100)]
    public int Year { get; set; }
    
    [Required]
    [Range(1, 12)]
    public int Month { get; set; }
    
    public DateTime LockedAt { get; set; } = DateTime.UtcNow;
    
    // Foreign keys
    public string? LockedByUserId { get; set; }
    
    // Navigation properties
    [ForeignKey(nameof(LockedByUserId))]
    public virtual ApplicationUser? LockedByUser { get; set; }
}
