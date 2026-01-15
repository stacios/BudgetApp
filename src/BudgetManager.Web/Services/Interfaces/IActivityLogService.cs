using BudgetManager.Web.Models;

namespace BudgetManager.Web.Services.Interfaces;

public interface IActivityLogService
{
    Task LogAsync(string entityName, int? entityId, string actionType, string? description = null, 
        object? oldValues = null, object? newValues = null, string? userId = null);
    
    Task<IEnumerable<ActivityLog>> GetLogsAsync(int page = 1, int pageSize = 50);
    
    Task<IEnumerable<ActivityLog>> GetLogsByEntityAsync(string entityName, int entityId);
    
    Task<int> GetTotalCountAsync();
}
