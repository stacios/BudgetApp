using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BudgetManager.Web.Models;

namespace BudgetManager.Web.Data;

public static class SeedData
{
    public static async Task InitializeAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        // Apply pending migrations
        await context.Database.MigrateAsync();
        
        // Seed demo user
        if (!await context.Users.AnyAsync())
        {
            var demoUser = new ApplicationUser
            {
                UserName = "demo@budgetmanager.com",
                Email = "demo@budgetmanager.com",
                EmailConfirmed = true,
                FirstName = "Demo",
                LastName = "User"
            };
            
            await userManager.CreateAsync(demoUser, "Demo123!");
        }
        
        // Seed accounts
        if (!await context.Accounts.AnyAsync())
        {
            var accounts = new[]
            {
                new Account { Name = "Main Checking", Type = "Checking", IsActive = true },
                new Account { Name = "Savings Account", Type = "Savings", IsActive = true }
            };
            
            context.Accounts.AddRange(accounts);
            await context.SaveChangesAsync();
        }
        
        // Seed categories
        if (!await context.Categories.AnyAsync())
        {
            var categories = new[]
            {
                new Category { Name = "Uncategorized", Description = "Default category for uncategorized transactions", IsActive = true },
                new Category { Name = "Housing", Description = "Rent, mortgage, repairs, property taxes", IsActive = true },
                new Category { Name = "Utilities", Description = "Electric, gas, water, internet, phone", IsActive = true },
                new Category { Name = "Groceries", Description = "Food and household supplies", IsActive = true },
                new Category { Name = "Transportation", Description = "Gas, car payments, insurance, repairs, public transit", IsActive = true },
                new Category { Name = "Healthcare", Description = "Medical bills, prescriptions, insurance", IsActive = true },
                new Category { Name = "Insurance", Description = "Life, health, home, auto insurance", IsActive = true },
                new Category { Name = "Dining Out", Description = "Restaurants, fast food, coffee shops", IsActive = true },
                new Category { Name = "Entertainment", Description = "Movies, streaming, games, hobbies", IsActive = true },
                new Category { Name = "Shopping", Description = "Clothing, electronics, household items", IsActive = true },
                new Category { Name = "Personal Care", Description = "Haircuts, gym, toiletries", IsActive = true },
                new Category { Name = "Education", Description = "Tuition, books, courses", IsActive = true },
                new Category { Name = "Subscriptions", Description = "Monthly subscriptions and memberships", IsActive = true },
                new Category { Name = "Gifts & Donations", Description = "Gifts, charitable donations", IsActive = true },
                new Category { Name = "Travel", Description = "Flights, hotels, vacation expenses", IsActive = true },
                new Category { Name = "Income", Description = "Salary, wages, freelance income", IsActive = true },
                new Category { Name = "Investments", Description = "Stock purchases, 401k, IRA", IsActive = true },
                new Category { Name = "Transfer", Description = "Transfers between accounts", IsActive = true }
            };
            
            context.Categories.AddRange(categories);
            await context.SaveChangesAsync();
        }
        
        // Seed categorization rules
        if (!await context.CategorizationRules.AnyAsync())
        {
            var groceriesId = await context.Categories.Where(c => c.Name == "Groceries").Select(c => c.Id).FirstOrDefaultAsync();
            var diningId = await context.Categories.Where(c => c.Name == "Dining Out").Select(c => c.Id).FirstOrDefaultAsync();
            var utilitiesId = await context.Categories.Where(c => c.Name == "Utilities").Select(c => c.Id).FirstOrDefaultAsync();
            var transportationId = await context.Categories.Where(c => c.Name == "Transportation").Select(c => c.Id).FirstOrDefaultAsync();
            var subscriptionsId = await context.Categories.Where(c => c.Name == "Subscriptions").Select(c => c.Id).FirstOrDefaultAsync();
            var incomeId = await context.Categories.Where(c => c.Name == "Income").Select(c => c.Id).FirstOrDefaultAsync();
            
            var rules = new[]
            {
                // Grocery stores
                new CategorizationRule { Priority = 1, ContainsText = "walmart", CategoryId = groceriesId, IsActive = true },
                new CategorizationRule { Priority = 2, ContainsText = "target", CategoryId = groceriesId, IsActive = true },
                new CategorizationRule { Priority = 3, ContainsText = "kroger", CategoryId = groceriesId, IsActive = true },
                new CategorizationRule { Priority = 4, ContainsText = "whole foods", CategoryId = groceriesId, IsActive = true },
                new CategorizationRule { Priority = 5, ContainsText = "trader joe", CategoryId = groceriesId, IsActive = true },
                
                // Dining
                new CategorizationRule { Priority = 10, ContainsText = "starbucks", CategoryId = diningId, IsActive = true },
                new CategorizationRule { Priority = 11, ContainsText = "mcdonald", CategoryId = diningId, IsActive = true },
                new CategorizationRule { Priority = 12, ContainsText = "chipotle", CategoryId = diningId, IsActive = true },
                new CategorizationRule { Priority = 13, ContainsText = "doordash", CategoryId = diningId, IsActive = true },
                new CategorizationRule { Priority = 14, ContainsText = "uber eats", CategoryId = diningId, IsActive = true },
                new CategorizationRule { Priority = 15, ContainsText = "grubhub", CategoryId = diningId, IsActive = true },
                
                // Utilities
                new CategorizationRule { Priority = 20, ContainsText = "electric", CategoryId = utilitiesId, IsActive = true },
                new CategorizationRule { Priority = 21, ContainsText = "water bill", CategoryId = utilitiesId, IsActive = true },
                new CategorizationRule { Priority = 22, ContainsText = "comcast", CategoryId = utilitiesId, IsActive = true },
                new CategorizationRule { Priority = 23, ContainsText = "verizon", CategoryId = utilitiesId, IsActive = true },
                new CategorizationRule { Priority = 24, ContainsText = "at&t", CategoryId = utilitiesId, IsActive = true },
                
                // Transportation
                new CategorizationRule { Priority = 30, ContainsText = "shell", CategoryId = transportationId, IsActive = true },
                new CategorizationRule { Priority = 31, ContainsText = "chevron", CategoryId = transportationId, IsActive = true },
                new CategorizationRule { Priority = 32, ContainsText = "exxon", CategoryId = transportationId, IsActive = true },
                new CategorizationRule { Priority = 33, ContainsText = "uber", CategoryId = transportationId, IsActive = true },
                new CategorizationRule { Priority = 34, ContainsText = "lyft", CategoryId = transportationId, IsActive = true },
                
                // Subscriptions
                new CategorizationRule { Priority = 40, ContainsText = "netflix", CategoryId = subscriptionsId, IsActive = true },
                new CategorizationRule { Priority = 41, ContainsText = "spotify", CategoryId = subscriptionsId, IsActive = true },
                new CategorizationRule { Priority = 42, ContainsText = "amazon prime", CategoryId = subscriptionsId, IsActive = true },
                new CategorizationRule { Priority = 43, ContainsText = "hulu", CategoryId = subscriptionsId, IsActive = true },
                new CategorizationRule { Priority = 44, ContainsText = "disney+", CategoryId = subscriptionsId, IsActive = true },
                
                // Income
                new CategorizationRule { Priority = 50, ContainsText = "direct deposit", CategoryId = incomeId, IsActive = true },
                new CategorizationRule { Priority = 51, ContainsText = "payroll", CategoryId = incomeId, IsActive = true }
            };
            
            context.CategorizationRules.AddRange(rules);
            await context.SaveChangesAsync();
        }
    }
}
