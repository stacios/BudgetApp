using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using BudgetManager.Web.Data;
using BudgetManager.Web.Models;
using BudgetManager.Web.Services;
using BudgetManager.Web.Services.Interfaces;

namespace BudgetManager.Tests.Services;

public class LockingServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IActivityLogService> _activityLogServiceMock;
    private readonly LockingService _lockingService;

    public LockingServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _activityLogServiceMock = new Mock<IActivityLogService>();
        _lockingService = new LockingService(_context, _activityLogServiceMock.Object);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        var account = new Account { Id = 1, Name = "Test Account", Type = "Checking" };
        var category = new Category { Id = 1, Name = "Test Category" };
        
        _context.Accounts.Add(account);
        _context.Categories.Add(category);
        _context.SaveChanges();
    }

    [Fact]
    public async Task IsMonthLocked_ReturnsTrue_WhenMonthIsLocked()
    {
        // Arrange
        _context.LockedMonths.Add(new LockedMonth 
        { 
            Year = 2024, 
            Month = 1, 
            LockedByUserId = "user1" 
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _lockingService.IsMonthLockedAsync(2024, 1);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsMonthLocked_ReturnsFalse_WhenMonthIsNotLocked()
    {
        // Act
        var result = await _lockingService.IsMonthLockedAsync(2024, 6);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CanEditTransaction_ReturnsFalse_WhenMonthIsLocked()
    {
        // Arrange
        _context.LockedMonths.Add(new LockedMonth 
        { 
            Year = 2024, 
            Month = 1, 
            LockedByUserId = "user1" 
        });
        await _context.SaveChangesAsync();

        var transaction = new Transaction
        {
            Date = new DateTime(2024, 1, 15),
            Description = "Test",
            Amount = -50,
            CategoryId = 1,
            AccountId = 1,
            IsAdjustment = false
        };

        // Act
        var result = await _lockingService.CanEditTransactionAsync(transaction);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CanEditTransaction_ReturnsTrue_WhenTransactionIsAdjustment_EvenIfMonthIsLocked()
    {
        // Arrange
        _context.LockedMonths.Add(new LockedMonth 
        { 
            Year = 2024, 
            Month = 1, 
            LockedByUserId = "user1" 
        });
        await _context.SaveChangesAsync();

        var adjustmentTransaction = new Transaction
        {
            Date = new DateTime(2024, 1, 15),
            Description = "Adjustment",
            Amount = -50,
            CategoryId = 1,
            AccountId = 1,
            IsAdjustment = true
        };

        // Act
        var result = await _lockingService.CanEditTransactionAsync(adjustmentTransaction);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanDeleteTransaction_ReturnsFalse_WhenMonthIsLocked()
    {
        // Arrange
        _context.LockedMonths.Add(new LockedMonth 
        { 
            Year = 2024, 
            Month = 2, 
            LockedByUserId = "user1" 
        });
        await _context.SaveChangesAsync();

        var transaction = new Transaction
        {
            Date = new DateTime(2024, 2, 10),
            Description = "Test",
            Amount = -100,
            CategoryId = 1,
            AccountId = 1,
            IsAdjustment = false
        };

        // Act
        var result = await _lockingService.CanDeleteTransactionAsync(transaction);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CanDeleteTransaction_ReturnsTrue_WhenTransactionIsAdjustment_EvenIfMonthIsLocked()
    {
        // Arrange
        _context.LockedMonths.Add(new LockedMonth 
        { 
            Year = 2024, 
            Month = 2, 
            LockedByUserId = "user1" 
        });
        await _context.SaveChangesAsync();

        var adjustmentTransaction = new Transaction
        {
            Date = new DateTime(2024, 2, 10),
            Description = "Adjustment",
            Amount = -100,
            CategoryId = 1,
            AccountId = 1,
            IsAdjustment = true
        };

        // Act
        var result = await _lockingService.CanDeleteTransactionAsync(adjustmentTransaction);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task LockMonth_SuccessfullyLocksMonth()
    {
        // Act
        var result = await _lockingService.LockMonthAsync(2024, 3, "user1");

        // Assert
        Assert.True(result.Success);
        Assert.Contains("locked", result.Message.ToLower());
        Assert.True(await _lockingService.IsMonthLockedAsync(2024, 3));
    }

    [Fact]
    public async Task LockMonth_Fails_WhenAlreadyLocked()
    {
        // Arrange
        _context.LockedMonths.Add(new LockedMonth 
        { 
            Year = 2024, 
            Month = 4, 
            LockedByUserId = "user1" 
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _lockingService.LockMonthAsync(2024, 4, "user2");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("already locked", result.Message.ToLower());
    }

    [Fact]
    public async Task UnlockMonth_SuccessfullyUnlocksMonth()
    {
        // Arrange
        _context.LockedMonths.Add(new LockedMonth 
        { 
            Year = 2024, 
            Month = 5, 
            LockedByUserId = "user1" 
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _lockingService.UnlockMonthAsync(2024, 5, "user1");

        // Assert
        Assert.True(result.Success);
        Assert.Contains("unlocked", result.Message.ToLower());
        Assert.False(await _lockingService.IsMonthLockedAsync(2024, 5));
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
