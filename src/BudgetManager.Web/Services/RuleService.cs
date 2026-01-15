using Microsoft.EntityFrameworkCore;
using BudgetManager.Web.Data;
using BudgetManager.Web.Models;
using BudgetManager.Web.Services.Interfaces;

namespace BudgetManager.Web.Services;

public class RuleService : IRuleService
{
    private readonly ApplicationDbContext _context;
    private readonly IActivityLogService _activityLogService;
    
    public RuleService(ApplicationDbContext context, IActivityLogService activityLogService)
    {
        _context = context;
        _activityLogService = activityLogService;
    }
    
    public async Task<IEnumerable<CategorizationRule>> GetAllRulesAsync()
    {
        return await _context.CategorizationRules
            .Include(r => r.Category)
            .OrderBy(r => r.Priority)
            .ToListAsync();
    }
    
    public async Task<IEnumerable<CategorizationRule>> GetActiveRulesOrderedByPriorityAsync()
    {
        return await _context.CategorizationRules
            .Include(r => r.Category)
            .Where(r => r.IsActive)
            .OrderBy(r => r.Priority)
            .ToListAsync();
    }
    
    public async Task<CategorizationRule?> GetRuleByIdAsync(int id)
    {
        return await _context.CategorizationRules
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == id);
    }
    
    public async Task<CategorizationRule> CreateRuleAsync(CategorizationRule rule, string? userId = null)
    {
        rule.CreatedAt = DateTime.UtcNow;
        
        _context.CategorizationRules.Add(rule);
        await _context.SaveChangesAsync();
        
        await _activityLogService.LogAsync(
            "CategorizationRule",
            rule.Id,
            "Create",
            $"Created rule: '{rule.ContainsText}' → Category {rule.CategoryId}",
            null,
            new { rule.ContainsText, rule.CategoryId, rule.Priority, rule.IsActive },
            userId);
        
        return rule;
    }
    
    public async Task<CategorizationRule> UpdateRuleAsync(CategorizationRule rule, string? userId = null)
    {
        var existing = await _context.CategorizationRules.FindAsync(rule.Id);
        if (existing == null)
        {
            throw new InvalidOperationException($"Rule {rule.Id} not found.");
        }
        
        var oldValues = new
        {
            existing.ContainsText,
            existing.CategoryId,
            existing.Priority,
            existing.IsActive
        };
        
        existing.ContainsText = rule.ContainsText;
        existing.CategoryId = rule.CategoryId;
        existing.Priority = rule.Priority;
        existing.IsActive = rule.IsActive;
        
        await _context.SaveChangesAsync();
        
        await _activityLogService.LogAsync(
            "CategorizationRule",
            rule.Id,
            "Update",
            $"Updated rule: '{rule.ContainsText}'",
            oldValues,
            new { rule.ContainsText, rule.CategoryId, rule.Priority, rule.IsActive },
            userId);
        
        return existing;
    }
    
    public async Task DeleteRuleAsync(int id, string? userId = null)
    {
        var rule = await _context.CategorizationRules.FindAsync(id);
        if (rule == null)
        {
            throw new InvalidOperationException($"Rule {id} not found.");
        }
        
        var oldValues = new
        {
            rule.ContainsText,
            rule.CategoryId,
            rule.Priority
        };
        
        _context.CategorizationRules.Remove(rule);
        await _context.SaveChangesAsync();
        
        await _activityLogService.LogAsync(
            "CategorizationRule",
            id,
            "Delete",
            $"Deleted rule: '{rule.ContainsText}'",
            oldValues,
            null,
            userId);
    }
    
    public async Task ReorderRulesAsync(IEnumerable<(int RuleId, int NewPriority)> priorities, string? userId = null)
    {
        foreach (var (ruleId, newPriority) in priorities)
        {
            var rule = await _context.CategorizationRules.FindAsync(ruleId);
            if (rule != null)
            {
                rule.Priority = newPriority;
            }
        }
        
        await _context.SaveChangesAsync();
        
        await _activityLogService.LogAsync(
            "CategorizationRule",
            null,
            "Reorder",
            "Reordered categorization rules",
            null,
            null,
            userId);
    }
    
    public async Task<int?> ApplyRulesToDescriptionAsync(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }
        
        var normalizedDescription = description.ToLowerInvariant();
        var rules = await GetActiveRulesOrderedByPriorityAsync();
        
        foreach (var rule in rules)
        {
            if (normalizedDescription.Contains(rule.ContainsText.ToLowerInvariant()))
            {
                return rule.CategoryId;
            }
        }
        
        return null;
    }
    
    public async Task<int> ApplyRulesToUncategorizedTransactionsAsync(string? userId = null)
    {
        var defaultCategoryId = await _context.Categories
            .Where(c => c.Name == "Uncategorized" || c.Name == "Other")
            .Select(c => c.Id)
            .FirstOrDefaultAsync();
        
        if (defaultCategoryId == 0)
        {
            return 0;
        }
        
        var uncategorizedTransactions = await _context.Transactions
            .Where(t => t.CategoryId == defaultCategoryId)
            .ToListAsync();
        
        var rules = await GetActiveRulesOrderedByPriorityAsync();
        int categorizedCount = 0;
        
        foreach (var transaction in uncategorizedTransactions)
        {
            var normalizedDescription = transaction.Description.ToLowerInvariant();
            
            foreach (var rule in rules)
            {
                if (normalizedDescription.Contains(rule.ContainsText.ToLowerInvariant()))
                {
                    transaction.CategoryId = rule.CategoryId;
                    transaction.UpdatedAt = DateTime.UtcNow;
                    categorizedCount++;
                    break;
                }
            }
        }
        
        await _context.SaveChangesAsync();
        
        if (categorizedCount > 0)
        {
            await _activityLogService.LogAsync(
                "Transaction",
                null,
                "BulkCategorize",
                $"Applied rules to {categorizedCount} uncategorized transactions",
                null,
                new { CategorizedCount = categorizedCount },
                userId);
        }
        
        return categorizedCount;
    }
}
