namespace BudgetManager.Web.ViewModels;

public class ActivityLogViewModel
{
    public int Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    public string? UserName { get; set; }
    public DateTime Timestamp { get; set; }
    
    public string ActionClass => ActionType switch
    {
        "Create" => "success",
        "Update" => "info",
        "Delete" => "danger",
        "Import" => "primary",
        "Lock" => "warning",
        "Unlock" => "secondary",
        _ => "secondary"
    };
    
    public string ActionIcon => ActionType switch
    {
        "Create" => "bi-plus-circle",
        "Update" => "bi-pencil",
        "Delete" => "bi-trash",
        "Import" => "bi-upload",
        "Lock" => "bi-lock",
        "Unlock" => "bi-unlock",
        _ => "bi-circle"
    };
}

public class ActivityLogListViewModel
{
    public IEnumerable<ActivityLogViewModel> Logs { get; set; } = new List<ActivityLogViewModel>();
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    
    // Filters
    public string? EntityFilter { get; set; }
    public string? ActionFilter { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
