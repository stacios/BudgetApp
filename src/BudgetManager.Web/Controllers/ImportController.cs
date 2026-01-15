using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using BudgetManager.Web.Services.Interfaces;
using BudgetManager.Web.ViewModels;

namespace BudgetManager.Web.Controllers;

[Authorize]
public class ImportController : Controller
{
    private readonly IImportService _importService;
    private readonly IAccountService _accountService;
    
    public ImportController(IImportService importService, IAccountService accountService)
    {
        _importService = importService;
        _accountService = accountService;
    }
    
    public async Task<IActionResult> Index()
    {
        var accounts = await _accountService.GetActiveAccountsAsync();
        
        var viewModel = new ImportUploadViewModel
        {
            Accounts = new SelectList(accounts, "Id", "Name")
        };
        
        return View(viewModel);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Preview(ImportUploadViewModel model)
    {
        if (model.CsvFile == null || model.CsvFile.Length == 0)
        {
            ModelState.AddModelError("CsvFile", "Please select a CSV file.");
            var accounts = await _accountService.GetActiveAccountsAsync();
            model.Accounts = new SelectList(accounts, "Id", "Name", model.AccountId);
            return View("Index", model);
        }
        
        using var stream = model.CsvFile.OpenReadStream();
        var preview = await _importService.ParseCsvAsync(stream, model.AccountId);
        
        var account = await _accountService.GetAccountByIdAsync(model.AccountId);
        preview.AccountName = account?.Name ?? "Unknown";
        
        // Store preview in TempData for the confirm action
        HttpContext.Session.SetString("ImportPreview", System.Text.Json.JsonSerializer.Serialize(preview));
        
        return View(preview);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(List<int> selectedRows)
    {
        var previewJson = HttpContext.Session.GetString("ImportPreview");
        if (string.IsNullOrEmpty(previewJson))
        {
            TempData["Error"] = "Import session expired. Please upload your file again.";
            return RedirectToAction(nameof(Index));
        }
        
        var preview = System.Text.Json.JsonSerializer.Deserialize<ImportPreviewViewModel>(previewJson);
        if (preview == null)
        {
            TempData["Error"] = "Invalid import data. Please try again.";
            return RedirectToAction(nameof(Index));
        }
        
        // Mark selected rows
        foreach (var row in preview.Rows)
        {
            row.IsSelected = selectedRows.Contains(row.RowNumber);
        }
        
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _importService.ImportTransactionsAsync(preview, userId);
        
        HttpContext.Session.Remove("ImportPreview");
        
        if (result.Success)
        {
            TempData["Success"] = result.Message;
        }
        else
        {
            TempData["Warning"] = result.Message;
        }
        
        return RedirectToAction("Index", "Transactions");
    }
}
