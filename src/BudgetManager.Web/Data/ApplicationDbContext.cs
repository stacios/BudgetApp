using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BudgetManager.Web.Models;

namespace BudgetManager.Web.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Account> Accounts { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Transaction> Transactions { get; set; } = null!;
    public DbSet<MonthlyBudget> MonthlyBudgets { get; set; } = null!;
    public DbSet<CategorizationRule> CategorizationRules { get; set; } = null!;
    public DbSet<LockedMonth> LockedMonths { get; set; } = null!;
    public DbSet<ActivityLog> ActivityLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Account configuration
        builder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
        });

        // Category configuration
        builder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
        });

        // Transaction configuration
        builder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasConversion<double>();
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            
            entity.HasOne(e => e.Category)
                .WithMany(c => c.Transactions)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.Account)
                .WithMany(a => a.Transactions)
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.User)
                .WithMany(u => u.Transactions)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Index for duplicate detection
            entity.HasIndex(e => new { e.Date, e.Amount, e.AccountId });
        });

        // MonthlyBudget configuration
        builder.Entity<MonthlyBudget>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BudgetAmount).HasConversion<double>();
            entity.HasIndex(e => new { e.Year, e.Month, e.CategoryId }).IsUnique();
            
            entity.HasOne(e => e.Category)
                .WithMany(c => c.MonthlyBudgets)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // CategorizationRule configuration
        builder.Entity<CategorizationRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ContainsText).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Priority);
            
            entity.HasOne(e => e.Category)
                .WithMany(c => c.CategorizationRules)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // LockedMonth configuration
        builder.Entity<LockedMonth>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Year, e.Month }).IsUnique();
            
            entity.HasOne(e => e.LockedByUser)
                .WithMany(u => u.LockedMonths)
                .HasForeignKey(e => e.LockedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ActivityLog configuration
        builder.Entity<ActivityLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EntityName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ActionType).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => new { e.EntityName, e.EntityId });
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.ActivityLogs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
