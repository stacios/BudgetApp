using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using BudgetManager.Web.Data;
using BudgetManager.Web.Models;
using BudgetManager.Web.Services;
using BudgetManager.Web.Services.Interfaces;

namespace BudgetManager.Tests.Services;

public class BudgetServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IActivityLogService> _activityLogServiceMock;
    private readonly BudgetService _budgetService;

    public BudgetServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _activityLogServiceMock = new Mock<IActivityLogService>();
        _budgetService = new BudgetService(_context, _activityLogServiceMock.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var account = new Account { Id = 1, Name = "Test Account", Type = "Checking" };
        var categories = new[]
        {
            new Category { Id = 1, Name = "Groceries", IsActive = true },
            new Category { Id = 2, Name = "Utilities", IsActive = true },
            new Category { Id = 3, Name = "Entertainment", IsActive = true }
        };

        _context.Accounts.Add(account);
        _context.Categories.AddRange(categories);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetBudgetSummary_ReturnsOK_WhenUnderBudget()
    {
        // Arrange
        var year = 2024;
        var month = 1;
        
        // Add budget
        await _budgetService.CreateOrUpdateBudgetAsync(year, month, 1, 500, "user1");
        
        // Add transactions (spent 200 of 500 budget)
        _context.Transactions.Add(new Transaction
        {
            Date = new DateTime(year, month, 15),
            Description = "Grocery shopping",
            Amount = -200,
            CategoryId = 1,
            AccountId = 1
        });
        await _context.SaveChangesAsync();

        // Act
        var summary = await _budgetService.GetBudgetSummaryAsync(year, month);
        var grocerySummary = summary.First(s => s.CategoryId == 1);

        // Assert
        Assert.Equal(BudgetStatus.OK, grocerySummary.Status);
        Assert.Equal(500, grocerySummary.BudgetAmount);
        Assert.Equal(200, grocerySummary.SpentAmount);
        Assert.Equal(300, grocerySummary.Remaining);
    }

    [Fact]
    public async Task GetBudgetSummary_ReturnsOVER_WhenOverBudget()
    {
        // Arrange
        var year = 2024;
        var month = 2;
        
        // Add budget
        await _budgetService.CreateOrUpdateBudgetAsync(year, month, 2, 100, "user1");
        
        // Add transactions (spent 150 of 100 budget = over budget)
        _context.Transactions.Add(new Transaction
        {
            Date = new DateTime(year, month, 10),
            Description = "Electric bill",
            Amount = -150,
            CategoryId = 2,
            AccountId = 1
        });
        await _context.SaveChangesAsync();

        // Act
        var summary = await _budgetService.GetBudgetSummaryAsync(year, month);
        var utilitySummary = summary.First(s => s.CategoryId == 2);

        // Assert
        Assert.Equal(BudgetStatus.OVER, utilitySummary.Status);
        Assert.Equal(100, utilitySummary.BudgetAmount);
        Assert.Equal(150, utilitySummary.SpentAmount);
        Assert.Equal(-50, utilitySummary.Remaining);
    }

    [Fact]
    public async Task GetBudgetPacing_CalculatesSafeToSpendToday()
    {
        // Arrange
        var year = DateTime.Now.Year;
        var month = DateTime.Now.Month;
        
        // Add budget
        await _budgetService.CreateOrUpdateBudgetAsync(year, month, 1, 1000, "user1");
        
        // Add some transactions
        _context.Transactions.Add(new Transaction
        {
            Date = new DateTime(year, month, 1),
            Description = "Shopping",
            Amount = -300,
            CategoryId = 1,
            AccountId = 1
        });
        await _context.SaveChangesAsync();

        // Act
        var pacing = await _budgetService.GetBudgetPacingAsync(year, month);

        // Assert
        Assert.Equal(1000, pacing.TotalBudget);
        Assert.Equal(300, pacing.TotalSpent);
        Assert.Equal(700, pacing.TotalRemaining);
        Assert.True(pacing.SafeToSpendToday >= 0);
    }

    [Fact]
    public async Task CreateOrUpdateBudget_CreatesNewBudget()
    {
        // Act
        var budget = await _budgetService.CreateOrUpdateBudgetAsync(2024, 6, 1, 500, "user1");

        // Assert
        Assert.NotNull(budget);
        Assert.Equal(500, budget.BudgetAmount);
        Assert.Equal(2024, budget.Year);
        Assert.Equal(6, budget.Month);
        Assert.Equal(1, budget.CategoryId);
    }

    [Fact]
    public async Task CreateOrUpdateBudget_UpdatesExistingBudget()
    {
        // Arrange
        await _budgetService.CreateOrUpdateBudgetAsync(2024, 7, 1, 500, "user1");

        // Act
        var updatedBudget = await _budgetService.CreateOrUpdateBudgetAsync(2024, 7, 1, 750, "user1");

        // Assert
        Assert.Equal(750, updatedBudget.BudgetAmount);
        
        var budgetsCount = await _context.MonthlyBudgets
            .CountAsync(b => b.Year == 2024 && b.Month == 7 && b.CategoryId == 1);
        Assert.Equal(1, budgetsCount);
    }

    [Fact]
    public async Task CopyBudgetsFromPreviousMonth_CopiesBudgets()
    {
        // Arrange - create budgets for previous month
        await _budgetService.CreateOrUpdateBudgetAsync(2024, 1, 1, 500, "user1");
        await _budgetService.CreateOrUpdateBudgetAsync(2024, 1, 2, 200, "user1");

        // Act - copy to next month
        await _budgetService.CopyBudgetsFromPreviousMonthAsync(2024, 2, "user1");

        // Assert
        var februaryBudgets = await _budgetService.GetBudgetsForMonthAsync(2024, 2);
        Assert.Equal(2, februaryBudgets.Count());
        Assert.Contains(februaryBudgets, b => b.CategoryId == 1 && b.BudgetAmount == 500);
        Assert.Contains(februaryBudgets, b => b.CategoryId == 2 && b.BudgetAmount == 200);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
