using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using BudgetManager.Web.Data;
using BudgetManager.Web.Models;
using BudgetManager.Web.Services.Interfaces;

namespace BudgetManager.Web.Services;

public class ActivityLogService : IActivityLogService
{
    private readonly ApplicationDbContext _context;
    
    public ActivityLogService(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task LogAsync(string entityName, int? entityId, string actionType, string? description = null,
        object? oldValues = null, object? newValues = null, string? userId = null)
    {
        var log = new ActivityLog
        {
            EntityName = entityName,
            EntityId = entityId,
            ActionType = actionType,
            Description = description,
            OldValuesJson = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
            NewValuesJson = newValues != null ? JsonSerializer.Serialize(newValues) : null,
            UserId = userId,
            Timestamp = DateTime.UtcNow
        };
        
        _context.ActivityLogs.Add(log);
        await _context.SaveChangesAsync();
    }
    
    public async Task<IEnumerable<ActivityLog>> GetLogsAsync(int page = 1, int pageSize = 50)
    {
        return await _context.ActivityLogs
            .Include(l => l.User)
            .OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
    
    public async Task<IEnumerable<ActivityLog>> GetLogsByEntityAsync(string entityName, int entityId)
    {
        return await _context.ActivityLogs
            .Include(l => l.User)
            .Where(l => l.EntityName == entityName && l.EntityId == entityId)
            .OrderByDescending(l => l.Timestamp)
            .ToListAsync();
    }
    
    public async Task<int> GetTotalCountAsync()
    {
        return await _context.ActivityLogs.CountAsync();
    }
}
