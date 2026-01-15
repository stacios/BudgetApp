using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BudgetManager.Web.ViewModels;

public class TransactionViewModel
{
    public int Id { get; set; }
    
    [Required]
    [DataType(DataType.Date)]
    public DateTime Date { get; set; } = DateTime.Today;
    
    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [DisplayFormat(DataFormatString = "{0:N2}")]
    public decimal Amount { get; set; }
    
    [MaxLength(1000)]
    public string? Notes { get; set; }
    
    public bool IsAdjustment { get; set; }
    
    [Required]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }
    
    [Required]
    [Display(Name = "Account")]
    public int AccountId { get; set; }
    
    // Display properties
    public string? CategoryName { get; set; }
    public string? AccountName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsLocked { get; set; }
    
    // Dropdown lists
    public SelectList? Categories { get; set; }
    public SelectList? Accounts { get; set; }
}

public class TransactionFilterViewModel
{
    [DataType(DataType.Date)]
    public DateTime? StartDate { get; set; }
    
    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }
    
    public int? CategoryId { get; set; }
    
    public int? AccountId { get; set; }
    
    public string? SearchTerm { get; set; }
    
    public decimal? MinAmount { get; set; }
    
    public decimal? MaxAmount { get; set; }
    
    public bool? IsAdjustment { get; set; }
    
    public int Page { get; set; } = 1;
    
    public int PageSize { get; set; } = 50;
    
    // Dropdown lists
    public SelectList? Categories { get; set; }
    public SelectList? Accounts { get; set; }
}

public class TransactionListViewModel
{
    public IEnumerable<TransactionViewModel> Transactions { get; set; } = new List<TransactionViewModel>();
    public TransactionFilterViewModel Filter { get; set; } = new();
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / Filter.PageSize);
}

public class TopExpenseViewModel
{
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string CategoryName { get; set; } = string.Empty;
}
