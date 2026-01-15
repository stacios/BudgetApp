using System.Globalization;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using BudgetManager.Web.Data;
using BudgetManager.Web.Models;
using BudgetManager.Web.Services.Interfaces;
using BudgetManager.Web.ViewModels;

namespace BudgetManager.Web.Services;

public class ImportService : IImportService
{
    private readonly ApplicationDbContext _context;
    private readonly IRuleService _ruleService;
    private readonly ICategoryService _categoryService;
    private readonly IActivityLogService _activityLogService;
    private readonly ILockingService _lockingService;
    
    public ImportService(
        ApplicationDbContext context,
        IRuleService ruleService,
        ICategoryService categoryService,
        IActivityLogService activityLogService,
        ILockingService lockingService)
    {
        _context = context;
        _ruleService = ruleService;
        _categoryService = categoryService;
        _activityLogService = activityLogService;
        _lockingService = lockingService;
    }
    
    public async Task<ImportPreviewViewModel> ParseCsvAsync(Stream fileStream, int accountId)
    {
        var preview = new ImportPreviewViewModel
        {
            AccountId = accountId,
            Rows = new List<ImportRowViewModel>()
        };
        
        var defaultCategoryId = await _categoryService.GetDefaultCategoryIdAsync() ?? 1;
        
        using var reader = new StreamReader(fileStream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            HeaderValidated = null,
            BadDataFound = null
        });
        
        // Read header
        await csv.ReadAsync();
        csv.ReadHeader();
        
        int rowNumber = 1;
        while (await csv.ReadAsync())
        {
            rowNumber++;
            var row = new ImportRowViewModel
            {
                RowNumber = rowNumber
            };
            
            try
            {
                // Parse Date
                var dateStr = csv.GetField("Date") ?? csv.GetField(0);
                if (!DateTime.TryParse(dateStr, out var date))
                {
                    row.Status = ImportRowStatus.Invalid;
                    row.ValidationErrors.Add($"Invalid date format: {dateStr}");
                }
                else
                {
                    row.Date = date;
                }
                
                // Parse Description
                row.Description = csv.GetField("Description") ?? csv.GetField(1) ?? "";
                if (string.IsNullOrWhiteSpace(row.Description))
                {
                    row.Status = ImportRowStatus.Invalid;
                    row.ValidationErrors.Add("Description is required");
                }
                
                // Parse Amount
                var amountStr = csv.GetField("Amount") ?? csv.GetField(2);
                if (!decimal.TryParse(amountStr?.Replace("$", "").Replace(",", ""), out var amount))
                {
                    row.Status = ImportRowStatus.Invalid;
                    row.ValidationErrors.Add($"Invalid amount format: {amountStr}");
                }
                else
                {
                    row.Amount = amount;
                }
                
                // Parse Category (optional)
                var categoryStr = csv.GetField("Category");
                if (!string.IsNullOrWhiteSpace(categoryStr))
                {
                    var category = await _categoryService.GetCategoryByNameAsync(categoryStr);
                    if (category != null)
                    {
                        row.CategoryId = category.Id;
                        row.CategoryName = category.Name;
                    }
                }
                
                // If no category set, try to apply rules
                if (!row.CategoryId.HasValue)
                {
                    var suggestedCategoryId = await _ruleService.ApplyRulesToDescriptionAsync(row.Description);
                    if (suggestedCategoryId.HasValue)
                    {
                        row.CategoryId = suggestedCategoryId.Value;
                        var category = await _categoryService.GetCategoryByIdAsync(suggestedCategoryId.Value);
                        row.CategoryName = category?.Name ?? "Unknown";
                        row.CategorySuggestedByRule = true;
                    }
                    else
                    {
                        row.CategoryId = defaultCategoryId;
                        row.CategoryName = "Uncategorized";
                    }
                }
                
                // Check for duplicate if row is valid so far
                if (row.Status != ImportRowStatus.Invalid && row.Date.HasValue)
                {
                    var isDuplicate = await IsDuplicateAsync(row.Date.Value, row.Amount, row.Description, accountId);
                    
                    // Check if month is locked
                    var isLocked = await _lockingService.IsMonthLockedAsync(row.Date.Value.Year, row.Date.Value.Month);
                    
                    if (isDuplicate)
                    {
                        row.Status = ImportRowStatus.Duplicate;
                    }
                    else if (isLocked)
                    {
                        row.Status = ImportRowStatus.Invalid;
                        row.ValidationErrors.Add($"Month {row.Date.Value:MMM yyyy} is locked");
                    }
                    else
                    {
                        row.Status = ImportRowStatus.OK;
                    }
                }
            }
            catch (Exception ex)
            {
                row.Status = ImportRowStatus.Invalid;
                row.ValidationErrors.Add($"Error parsing row: {ex.Message}");
            }
            
            preview.Rows.Add(row);
        }
        
        // Calculate summary
        preview.TotalRows = preview.Rows.Count;
        preview.ValidRows = preview.Rows.Count(r => r.Status == ImportRowStatus.OK);
        preview.DuplicateRows = preview.Rows.Count(r => r.Status == ImportRowStatus.Duplicate);
        preview.InvalidRows = preview.Rows.Count(r => r.Status == ImportRowStatus.Invalid);
        
        return preview;
    }
    
    public async Task<ImportResultViewModel> ImportTransactionsAsync(ImportPreviewViewModel preview, string? userId = null)
    {
        var result = new ImportResultViewModel();
        var defaultCategoryId = await _categoryService.GetDefaultCategoryIdAsync() ?? 1;
        
        var rowsToImport = preview.Rows
            .Where(r => r.Status == ImportRowStatus.OK && r.IsSelected)
            .ToList();
        
        foreach (var row in rowsToImport)
        {
            try
            {
                var transaction = new Transaction
                {
                    Date = row.Date!.Value,
                    Description = row.Description,
                    Amount = row.Amount,
                    CategoryId = row.CategoryId ?? defaultCategoryId,
                    AccountId = preview.AccountId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                
                _context.Transactions.Add(transaction);
                result.ImportedCount++;
            }
            catch
            {
                result.FailedCount++;
            }
        }
        
        await _context.SaveChangesAsync();
        
        await _activityLogService.LogAsync(
            "Transaction",
            null,
            "Import",
            $"Imported {result.ImportedCount} transactions from CSV (Account: {preview.AccountId})",
            null,
            new { AccountId = preview.AccountId, ImportedCount = result.ImportedCount, FailedCount = result.FailedCount },
            userId);
        
        result.Success = result.FailedCount == 0;
        result.Message = result.Success
            ? $"Successfully imported {result.ImportedCount} transactions."
            : $"Imported {result.ImportedCount} transactions with {result.FailedCount} failures.";
        
        return result;
    }
    
    public async Task<bool> IsDuplicateAsync(DateTime date, decimal amount, string description, int accountId)
    {
        var normalizedDescription = NormalizeDescription(description);
        var roundedAmount = RoundAmount(amount);
        
        // Look for existing transactions with same date, rounded amount, and normalized description
        var existingTransactions = await _context.Transactions
            .Where(t => t.Date.Date == date.Date && t.AccountId == accountId)
            .ToListAsync();
        
        foreach (var existing in existingTransactions)
        {
            var existingNormalized = NormalizeDescription(existing.Description);
            var existingRounded = RoundAmount(existing.Amount);
            
            if (existingNormalized == normalizedDescription && existingRounded == roundedAmount)
            {
                return true;
            }
        }
        
        return false;
    }
    
    public string NormalizeDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return string.Empty;
        }
        
        // Remove extra whitespace, convert to lowercase
        var normalized = Regex.Replace(description.Trim().ToLowerInvariant(), @"\s+", " ");
        
        // Remove common variations like transaction numbers, dates, etc.
        normalized = Regex.Replace(normalized, @"\b\d{4,}\b", ""); // Remove long numbers
        normalized = Regex.Replace(normalized, @"#\d+", ""); // Remove # followed by numbers
        normalized = Regex.Replace(normalized, @"\s+", " ").Trim();
        
        return normalized;
    }
    
    public decimal RoundAmount(decimal amount)
    {
        // Round to 2 decimal places
        return Math.Round(amount, 2);
    }
}
