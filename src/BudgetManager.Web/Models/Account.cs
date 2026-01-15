using System.ComponentModel.DataAnnotations;

namespace BudgetManager.Web.Models;

public class Account
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty; // Checking, Savings, Credit Card, etc.
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
