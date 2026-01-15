using BudgetManager.Web.Models;

namespace BudgetManager.Web.Services.Interfaces;

public interface IRuleService
{
    Task<IEnumerable<CategorizationRule>> GetAllRulesAsync();
    
    Task<IEnumerable<CategorizationRule>> GetActiveRulesOrderedByPriorityAsync();
    
    Task<CategorizationRule?> GetRuleByIdAsync(int id);
    
    Task<CategorizationRule> CreateRuleAsync(CategorizationRule rule, string? userId = null);
    
    Task<CategorizationRule> UpdateRuleAsync(CategorizationRule rule, string? userId = null);
    
    Task DeleteRuleAsync(int id, string? userId = null);
    
    Task ReorderRulesAsync(IEnumerable<(int RuleId, int NewPriority)> priorities, string? userId = null);
    
    Task<int?> ApplyRulesToDescriptionAsync(string description);
    
    Task<int> ApplyRulesToUncategorizedTransactionsAsync(string? userId = null);
}
