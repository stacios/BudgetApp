using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BudgetManager.Web.Models;

public class ActivityLog
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string EntityName { get; set; } = string.Empty;
    
    public int? EntityId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string ActionType { get; set; } = string.Empty; // Create, Update, Delete, Import, Lock, Unlock
    
    public string? OldValuesJson { get; set; }
    
    public string? NewValuesJson { get; set; }
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    // Foreign keys
    public string? UserId { get; set; }
    
    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser? User { get; set; }
}
