using System.ComponentModel.DataAnnotations;

namespace BudgetManager.Web.ViewModels;

public class AccountViewModel
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty;
    
    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
    
    // Display properties
    public DateTime CreatedAt { get; set; }
    public int TransactionCount { get; set; }
    public decimal Balance { get; set; }
}

public class AccountListViewModel
{
    public IEnumerable<AccountViewModel> Accounts { get; set; } = new List<AccountViewModel>();
    public bool ShowInactive { get; set; }
}

public static class AccountTypes
{
    public static readonly string[] Types = new[]
    {
        "Checking",
        "Savings",
        "Credit Card",
        "Cash",
        "Investment",
        "Other"
    };
}
