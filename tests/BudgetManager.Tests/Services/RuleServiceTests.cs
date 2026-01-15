using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using BudgetManager.Web.Data;
using BudgetManager.Web.Models;
using BudgetManager.Web.Services;
using BudgetManager.Web.Services.Interfaces;

namespace BudgetManager.Tests.Services;

public class RuleServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IActivityLogService> _activityLogServiceMock;
    private readonly RuleService _ruleService;

    public RuleServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _activityLogServiceMock = new Mock<IActivityLogService>();
        _ruleService = new RuleService(_context, _activityLogServiceMock.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var categories = new[]
        {
            new Category { Id = 1, Name = "Groceries", IsActive = true },
            new Category { Id = 2, Name = "Dining Out", IsActive = true },
            new Category { Id = 3, Name = "Entertainment", IsActive = true },
            new Category { Id = 4, Name = "Transportation", IsActive = true },
            new Category { Id = 5, Name = "Uncategorized", IsActive = true }
        };

        _context.Categories.AddRange(categories);
        _context.SaveChanges();
    }

    [Fact]
    public async Task ApplyRulesToDescription_ReturnsCorrectCategory_ByPriority()
    {
        // Arrange - create rules with different priorities
        var rule1 = new CategorizationRule
        {
            Priority = 10,
            ContainsText = "walmart",
            CategoryId = 1, // Groceries
            IsActive = true
        };
        var rule2 = new CategorizationRule
        {
            Priority = 20,
            ContainsText = "walmart grocery",
            CategoryId = 1, // Groceries
            IsActive = true
        };
        
        _context.CategorizationRules.AddRange(rule1, rule2);
        await _context.SaveChangesAsync();

        // Act - test with "Walmart Grocery Store"
        var categoryId = await _ruleService.ApplyRulesToDescriptionAsync("WALMART GROCERY STORE");

        // Assert - should match rule1 (lower priority = higher precedence)
        Assert.Equal(1, categoryId);
    }

    [Fact]
    public async Task ApplyRulesToDescription_ReturnsHigherPriorityMatch()
    {
        // Arrange
        var rule1 = new CategorizationRule
        {
            Priority = 1, // Higher priority (lower number)
            ContainsText = "uber eats",
            CategoryId = 2, // Dining Out
            IsActive = true
        };
        var rule2 = new CategorizationRule
        {
            Priority = 10, // Lower priority
            ContainsText = "uber",
            CategoryId = 4, // Transportation
            IsActive = true
        };
        
        _context.CategorizationRules.AddRange(rule1, rule2);
        await _context.SaveChangesAsync();

        // Act - test with "Uber Eats" which matches both rules
        var categoryId = await _ruleService.ApplyRulesToDescriptionAsync("Uber Eats Delivery");

        // Assert - should match rule1 (higher priority)
        Assert.Equal(2, categoryId); // Dining Out
    }

    [Fact]
    public async Task ApplyRulesToDescription_ReturnsNull_WhenNoMatch()
    {
        // Arrange
        var rule = new CategorizationRule
        {
            Priority = 1,
            ContainsText = "amazon",
            CategoryId = 3,
            IsActive = true
        };
        
        _context.CategorizationRules.Add(rule);
        await _context.SaveChangesAsync();

        // Act
        var categoryId = await _ruleService.ApplyRulesToDescriptionAsync("Netflix Subscription");

        // Assert
        Assert.Null(categoryId);
    }

    [Fact]
    public async Task ApplyRulesToDescription_IgnoresInactiveRules()
    {
        // Arrange
        var inactiveRule = new CategorizationRule
        {
            Priority = 1,
            ContainsText = "starbucks",
            CategoryId = 2, // Dining Out
            IsActive = false // Inactive
        };
        
        _context.CategorizationRules.Add(inactiveRule);
        await _context.SaveChangesAsync();

        // Act
        var categoryId = await _ruleService.ApplyRulesToDescriptionAsync("Starbucks Coffee");

        // Assert - should return null since rule is inactive
        Assert.Null(categoryId);
    }

    [Fact]
    public async Task ApplyRulesToDescription_IsCaseInsensitive()
    {
        // Arrange
        var rule = new CategorizationRule
        {
            Priority = 1,
            ContainsText = "chipotle",
            CategoryId = 2, // Dining Out
            IsActive = true
        };
        
        _context.CategorizationRules.Add(rule);
        await _context.SaveChangesAsync();

        // Act - test with different cases
        var result1 = await _ruleService.ApplyRulesToDescriptionAsync("CHIPOTLE MEXICAN GRILL");
        var result2 = await _ruleService.ApplyRulesToDescriptionAsync("chipotle mexican grill");
        var result3 = await _ruleService.ApplyRulesToDescriptionAsync("Chipotle Mexican Grill");

        // Assert - all should match
        Assert.Equal(2, result1);
        Assert.Equal(2, result2);
        Assert.Equal(2, result3);
    }

    [Fact]
    public async Task ReorderRules_UpdatesPriorities()
    {
        // Arrange
        var rules = new[]
        {
            new CategorizationRule { Priority = 1, ContainsText = "rule1", CategoryId = 1, IsActive = true },
            new CategorizationRule { Priority = 2, ContainsText = "rule2", CategoryId = 2, IsActive = true },
            new CategorizationRule { Priority = 3, ContainsText = "rule3", CategoryId = 3, IsActive = true }
        };
        _context.CategorizationRules.AddRange(rules);
        await _context.SaveChangesAsync();

        // Act - reorder: rule3 first, then rule1, then rule2
        var newOrder = new[]
        {
            (rules[2].Id, 1), // rule3 becomes priority 1
            (rules[0].Id, 2), // rule1 becomes priority 2
            (rules[1].Id, 3)  // rule2 becomes priority 3
        };
        await _ruleService.ReorderRulesAsync(newOrder, "user1");

        // Assert
        var updatedRules = await _ruleService.GetAllRulesAsync();
        Assert.Equal(1, updatedRules.First(r => r.ContainsText == "rule3").Priority);
        Assert.Equal(2, updatedRules.First(r => r.ContainsText == "rule1").Priority);
        Assert.Equal(3, updatedRules.First(r => r.ContainsText == "rule2").Priority);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
