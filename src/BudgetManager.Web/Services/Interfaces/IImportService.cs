using BudgetManager.Web.ViewModels;

namespace BudgetManager.Web.Services.Interfaces;

public interface IImportService
{
    Task<ImportPreviewViewModel> ParseCsvAsync(Stream fileStream, int accountId);
    
    Task<ImportResultViewModel> ImportTransactionsAsync(ImportPreviewViewModel preview, string? userId = null);
    
    Task<bool> IsDuplicateAsync(DateTime date, decimal amount, string description, int accountId);
    
    string NormalizeDescription(string description);
    
    decimal RoundAmount(decimal amount);
}
