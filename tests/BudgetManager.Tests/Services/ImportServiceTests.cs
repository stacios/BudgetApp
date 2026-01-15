using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using BudgetManager.Web.Data;
using BudgetManager.Web.Models;
using BudgetManager.Web.Services;
using BudgetManager.Web.Services.Interfaces;

namespace BudgetManager.Tests.Services;

public class ImportServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IRuleService> _ruleServiceMock;
    private readonly Mock<ICategoryService> _categoryServiceMock;
    private readonly Mock<IActivityLogService> _activityLogServiceMock;
    private readonly Mock<ILockingService> _lockingServiceMock;
    private readonly ImportService _importService;

    public ImportServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _ruleServiceMock = new Mock<IRuleService>();
        _categoryServiceMock = new Mock<ICategoryService>();
        _activityLogServiceMock = new Mock<IActivityLogService>();
        _lockingServiceMock = new Mock<ILockingService>();
        
        _importService = new ImportService(
            _context,
            _ruleServiceMock.Object,
            _categoryServiceMock.Object,
            _activityLogServiceMock.Object,
            _lockingServiceMock.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var account = new Account { Id = 1, Name = "Test Account", Type = "Checking" };
        var category = new Category { Id = 1, Name = "Uncategorized", IsActive = true };
        
        _context.Accounts.Add(account);
        _context.Categories.Add(category);
        _context.SaveChanges();
    }

    [Fact]
    public async Task IsDuplicate_ReturnsTrue_ForExactDuplicate()
    {
        // Arrange
        _context.Transactions.Add(new Transaction
        {
            Date = new DateTime(2024, 1, 15),
            Description = "Walmart Shopping",
            Amount = -85.50m,
            CategoryId = 1,
            AccountId = 1
        });
        await _context.SaveChangesAsync();

        // Act
        var isDuplicate = await _importService.IsDuplicateAsync(
            new DateTime(2024, 1, 15),
            -85.50m,
            "Walmart Shopping",
            1);

        // Assert
        Assert.True(isDuplicate);
    }

    [Fact]
    public async Task IsDuplicate_ReturnsTrue_ForSimilarDescription()
    {
        // Arrange
        _context.Transactions.Add(new Transaction
        {
            Date = new DateTime(2024, 1, 15),
            Description = "WALMART SHOPPING #12345",
            Amount = -85.50m,
            CategoryId = 1,
            AccountId = 1
        });
        await _context.SaveChangesAsync();

        // Act - slightly different description (normalized should match)
        var isDuplicate = await _importService.IsDuplicateAsync(
            new DateTime(2024, 1, 15),
            -85.50m,
            "walmart shopping",
            1);

        // Assert
        Assert.True(isDuplicate);
    }

    [Fact]
    public async Task IsDuplicate_ReturnsFalse_ForDifferentDate()
    {
        // Arrange
        _context.Transactions.Add(new Transaction
        {
            Date = new DateTime(2024, 1, 15),
            Description = "Walmart Shopping",
            Amount = -85.50m,
            CategoryId = 1,
            AccountId = 1
        });
        await _context.SaveChangesAsync();

        // Act - different date
        var isDuplicate = await _importService.IsDuplicateAsync(
            new DateTime(2024, 1, 16),
            -85.50m,
            "Walmart Shopping",
            1);

        // Assert
        Assert.False(isDuplicate);
    }

    [Fact]
    public async Task IsDuplicate_ReturnsFalse_ForDifferentAmount()
    {
        // Arrange
        _context.Transactions.Add(new Transaction
        {
            Date = new DateTime(2024, 1, 15),
            Description = "Walmart Shopping",
            Amount = -85.50m,
            CategoryId = 1,
            AccountId = 1
        });
        await _context.SaveChangesAsync();

        // Act - different amount
        var isDuplicate = await _importService.IsDuplicateAsync(
            new DateTime(2024, 1, 15),
            -95.50m,
            "Walmart Shopping",
            1);

        // Assert
        Assert.False(isDuplicate);
    }

    [Fact]
    public async Task IsDuplicate_ReturnsFalse_ForDifferentAccount()
    {
        // Arrange
        _context.Accounts.Add(new Account { Id = 2, Name = "Savings", Type = "Savings" });
        _context.Transactions.Add(new Transaction
        {
            Date = new DateTime(2024, 1, 15),
            Description = "Walmart Shopping",
            Amount = -85.50m,
            CategoryId = 1,
            AccountId = 1
        });
        await _context.SaveChangesAsync();

        // Act - different account
        var isDuplicate = await _importService.IsDuplicateAsync(
            new DateTime(2024, 1, 15),
            -85.50m,
            "Walmart Shopping",
            2);

        // Assert
        Assert.False(isDuplicate);
    }

    [Fact]
    public void NormalizeDescription_RemovesExtraSpaces()
    {
        // Act
        var normalized = _importService.NormalizeDescription("  Walmart   Shopping   ");

        // Assert
        Assert.Equal("walmart shopping", normalized);
    }

    [Fact]
    public void NormalizeDescription_ConvertsToLowercase()
    {
        // Act
        var normalized = _importService.NormalizeDescription("WALMART SHOPPING");

        // Assert
        Assert.Equal("walmart shopping", normalized);
    }

    [Fact]
    public void NormalizeDescription_RemovesLongNumbers()
    {
        // Act
        var normalized = _importService.NormalizeDescription("Walmart #12345678 Shopping");

        // Assert - should remove the long number
        Assert.DoesNotContain("12345678", normalized);
    }

    [Fact]
    public void RoundAmount_RoundsToTwoDecimals()
    {
        // Act
        var rounded1 = _importService.RoundAmount(85.555m);
        var rounded2 = _importService.RoundAmount(85.554m);

        // Assert
        Assert.Equal(85.56m, rounded1);
        Assert.Equal(85.55m, rounded2);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
