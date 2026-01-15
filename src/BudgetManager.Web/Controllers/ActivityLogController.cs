using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BudgetManager.Web.Services.Interfaces;
using BudgetManager.Web.ViewModels;

namespace BudgetManager.Web.Controllers;

[Authorize]
public class ActivityLogController : Controller
{
    private readonly IActivityLogService _activityLogService;
    
    public ActivityLogController(IActivityLogService activityLogService)
    {
        _activityLogService = activityLogService;
    }
    
    public async Task<IActionResult> Index(int page = 1, int pageSize = 50)
    {
        var logs = await _activityLogService.GetLogsAsync(page, pageSize);
        var totalCount = await _activityLogService.GetTotalCountAsync();
        
        var logViewModels = logs.Select(l => new ActivityLogViewModel
        {
            Id = l.Id,
            EntityName = l.EntityName,
            EntityId = l.EntityId,
            ActionType = l.ActionType,
            Description = l.Description,
            OldValuesJson = l.OldValuesJson,
            NewValuesJson = l.NewValuesJson,
            UserName = l.User?.UserName ?? "System",
            Timestamp = l.Timestamp
        }).ToList();
        
        var viewModel = new ActivityLogListViewModel
        {
            Logs = logViewModels,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
        
        return View(viewModel);
    }
    
    public async Task<IActionResult> Details(int id)
    {
        var logs = await _activityLogService.GetLogsAsync(1, int.MaxValue);
        var log = logs.FirstOrDefault(l => l.Id == id);
        
        if (log == null)
        {
            return NotFound();
        }
        
        var viewModel = new ActivityLogViewModel
        {
            Id = log.Id,
            EntityName = log.EntityName,
            EntityId = log.EntityId,
            ActionType = log.ActionType,
            Description = log.Description,
            OldValuesJson = log.OldValuesJson,
            NewValuesJson = log.NewValuesJson,
            UserName = log.User?.UserName ?? "System",
            Timestamp = log.Timestamp
        };
        
        return View(viewModel);
    }
}
