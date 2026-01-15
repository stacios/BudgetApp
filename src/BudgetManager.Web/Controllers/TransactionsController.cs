using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using BudgetManager.Web.Models;
using BudgetManager.Web.Services.Interfaces;
using BudgetManager.Web.ViewModels;

namespace BudgetManager.Web.Controllers;

[Authorize]
public class TransactionsController : Controller
{
    private readonly ITransactionService _transactionService;
    private readonly ICategoryService _categoryService;
    private readonly IAccountService _accountService;
    private readonly ILockingService _lockingService;
    
    public TransactionsController(
        ITransactionService transactionService,
        ICategoryService categoryService,
        IAccountService accountService,
        ILockingService lockingService)
    {
        _transactionService = transactionService;
        _categoryService = categoryService;
        _accountService = accountService;
        _lockingService = lockingService;
    }
    
    public async Task<IActionResult> Index(TransactionFilterViewModel filter)
    {
        // Default to current month if no filter
        if (!filter.StartDate.HasValue && !filter.EndDate.HasValue)
        {
            filter.StartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            filter.EndDate = filter.StartDate.Value.AddMonths(1).AddDays(-1);
        }
        
        var transactions = await _transactionService.GetTransactionsAsync(filter);
        var totalCount = await _transactionService.GetTransactionCountAsync(filter);
        
        var categories = await _categoryService.GetActiveCategoriesAsync();
        var accounts = await _accountService.GetActiveAccountsAsync();
        
        filter.Categories = new SelectList(categories, "Id", "Name", filter.CategoryId);
        filter.Accounts = new SelectList(accounts, "Id", "Name", filter.AccountId);
        
        var transactionViewModels = new List<TransactionViewModel>();
        foreach (var t in transactions)
        {
            var isLocked = await _lockingService.IsMonthLockedAsync(t.Date.Year, t.Date.Month);
            transactionViewModels.Add(new TransactionViewModel
            {
                Id = t.Id,
                Date = t.Date,
                Description = t.Description,
                Amount = t.Amount,
                Notes = t.Notes,
                IsAdjustment = t.IsAdjustment,
                CategoryId = t.CategoryId,
                AccountId = t.AccountId,
                CategoryName = t.Category?.Name ?? "Uncategorized",
                AccountName = t.Account?.Name ?? "Unknown",
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                IsLocked = isLocked && !t.IsAdjustment
            });
        }
        
        var viewModel = new TransactionListViewModel
        {
            Transactions = transactionViewModels,
            Filter = filter,
            TotalCount = totalCount
        };
        
        return View(viewModel);
    }
    
    public async Task<IActionResult> Create()
    {
        var categories = await _categoryService.GetActiveCategoriesAsync();
        var accounts = await _accountService.GetActiveAccountsAsync();
        
        var viewModel = new TransactionViewModel
        {
            Date = DateTime.Today,
            Categories = new SelectList(categories, "Id", "Name"),
            Accounts = new SelectList(accounts, "Id", "Name")
        };
        
        return View(viewModel);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TransactionViewModel viewModel)
    {
        if (ModelState.IsValid)
        {
            var transaction = new Transaction
            {
                Date = viewModel.Date,
                Description = viewModel.Description,
                Amount = viewModel.Amount,
                Notes = viewModel.Notes,
                IsAdjustment = viewModel.IsAdjustment,
                CategoryId = viewModel.CategoryId,
                AccountId = viewModel.AccountId
            };
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _transactionService.CreateTransactionAsync(transaction, userId);
            
            if (result.Success)
            {
                TempData["Success"] = result.Message;
                return RedirectToAction(nameof(Index));
            }
            
            ModelState.AddModelError("", result.Message);
        }
        
        var categories = await _categoryService.GetActiveCategoriesAsync();
        var accounts = await _accountService.GetActiveAccountsAsync();
        viewModel.Categories = new SelectList(categories, "Id", "Name", viewModel.CategoryId);
        viewModel.Accounts = new SelectList(accounts, "Id", "Name", viewModel.AccountId);
        
        return View(viewModel);
    }
    
    public async Task<IActionResult> Edit(int id)
    {
        var transaction = await _transactionService.GetTransactionByIdAsync(id);
        if (transaction == null)
        {
            return NotFound();
        }
        
        var isLocked = await _lockingService.IsMonthLockedAsync(transaction.Date.Year, transaction.Date.Month);
        
        var categories = await _categoryService.GetActiveCategoriesAsync();
        var accounts = await _accountService.GetActiveAccountsAsync();
        
        var viewModel = new TransactionViewModel
        {
            Id = transaction.Id,
            Date = transaction.Date,
            Description = transaction.Description,
            Amount = transaction.Amount,
            Notes = transaction.Notes,
            IsAdjustment = transaction.IsAdjustment,
            CategoryId = transaction.CategoryId,
            AccountId = transaction.AccountId,
            IsLocked = isLocked && !transaction.IsAdjustment,
            Categories = new SelectList(categories, "Id", "Name", transaction.CategoryId),
            Accounts = new SelectList(accounts, "Id", "Name", transaction.AccountId)
        };
        
        return View(viewModel);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TransactionViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            return NotFound();
        }
        
        if (ModelState.IsValid)
        {
            var transaction = new Transaction
            {
                Id = viewModel.Id,
                Date = viewModel.Date,
                Description = viewModel.Description,
                Amount = viewModel.Amount,
                Notes = viewModel.Notes,
                IsAdjustment = viewModel.IsAdjustment,
                CategoryId = viewModel.CategoryId,
                AccountId = viewModel.AccountId
            };
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _transactionService.UpdateTransactionAsync(transaction, userId);
            
            if (result.Success)
            {
                TempData["Success"] = result.Message;
                return RedirectToAction(nameof(Index));
            }
            
            ModelState.AddModelError("", result.Message);
        }
        
        var categories = await _categoryService.GetActiveCategoriesAsync();
        var accounts = await _accountService.GetActiveAccountsAsync();
        viewModel.Categories = new SelectList(categories, "Id", "Name", viewModel.CategoryId);
        viewModel.Accounts = new SelectList(accounts, "Id", "Name", viewModel.AccountId);
        
        return View(viewModel);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _transactionService.DeleteTransactionAsync(id, userId);
        
        if (result.Success)
        {
            TempData["Success"] = result.Message;
        }
        else
        {
            TempData["Error"] = result.Message;
        }
        
        return RedirectToAction(nameof(Index));
    }
}
