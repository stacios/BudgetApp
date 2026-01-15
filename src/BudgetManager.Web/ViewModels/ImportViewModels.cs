using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BudgetManager.Web.ViewModels;

public class ImportUploadViewModel
{
    [Required]
    [Display(Name = "CSV File")]
    public IFormFile? CsvFile { get; set; }
    
    [Required]
    [Display(Name = "Account")]
    public int AccountId { get; set; }
    
    public SelectList? Accounts { get; set; }
}

public class ImportPreviewViewModel
{
    public int AccountId { get; set; }
    public string? AccountName { get; set; }
    public List<ImportRowViewModel> Rows { get; set; } = new();
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int DuplicateRows { get; set; }
    public int InvalidRows { get; set; }
}

public class ImportRowViewModel
{
    public int RowNumber { get; set; }
    public DateTime? Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public bool CategorySuggestedByRule { get; set; }
    public ImportRowStatus Status { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
    public bool IsSelected { get; set; } = true;
    
    public string StatusClass => Status switch
    {
        ImportRowStatus.OK => "table-success",
        ImportRowStatus.Duplicate => "table-warning",
        ImportRowStatus.Invalid => "table-danger",
        _ => ""
    };
    
    public string StatusText => Status switch
    {
        ImportRowStatus.OK => "OK",
        ImportRowStatus.Duplicate => "Duplicate",
        ImportRowStatus.Invalid => "Invalid",
        _ => "Unknown"
    };
}

public enum ImportRowStatus
{
    OK,
    Duplicate,
    Invalid
}

public class ImportResultViewModel
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int ImportedCount { get; set; }
    public int FailedCount { get; set; }
    public int SkippedCount { get; set; }
}
