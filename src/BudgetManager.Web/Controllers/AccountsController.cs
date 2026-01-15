using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BudgetManager.Web.Data;
using BudgetManager.Web.Models;
using BudgetManager.Web.Services.Interfaces;
using BudgetManager.Web.ViewModels;

namespace BudgetManager.Web.Controllers;

[Authorize]
public class AccountsController : Controller
{
    private readonly IAccountService _accountService;
    private readonly ApplicationDbContext _context;
    
    public AccountsController(IAccountService accountService, ApplicationDbContext context)
    {
        _accountService = accountService;
        _context = context;
    }
    
    public async Task<IActionResult> Index(bool showInactive = false)
    {
        var accounts = showInactive
            ? await _accountService.GetAllAccountsAsync()
            : await _accountService.GetActiveAccountsAsync();
        
        var accountViewModels = new List<AccountViewModel>();
        foreach (var a in accounts)
        {
            var transactionCount = await _context.Transactions.CountAsync(t => t.AccountId == a.Id);
            var balance = await _context.Transactions
                .Where(t => t.AccountId == a.Id)
                .SumAsync(t => t.Amount);
            
            accountViewModels.Add(new AccountViewModel
            {
                Id = a.Id,
                Name = a.Name,
                Type = a.Type,
                IsActive = a.IsActive,
                CreatedAt = a.CreatedAt,
                TransactionCount = transactionCount,
                Balance = balance
            });
        }
        
        var viewModel = new AccountListViewModel
        {
            Accounts = accountViewModels,
            ShowInactive = showInactive
        };
        
        return View(viewModel);
    }
    
    public IActionResult Create()
    {
        ViewBag.AccountTypes = AccountTypes.Types;
        return View(new AccountViewModel());
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AccountViewModel viewModel)
    {
        if (ModelState.IsValid)
        {
            var account = new Account
            {
                Name = viewModel.Name,
                Type = viewModel.Type,
                IsActive = viewModel.IsActive
            };
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _accountService.CreateAccountAsync(account, userId);
            
            TempData["Success"] = "Account created successfully.";
            return RedirectToAction(nameof(Index));
        }
        
        ViewBag.AccountTypes = AccountTypes.Types;
        return View(viewModel);
    }
    
    public async Task<IActionResult> Edit(int id)
    {
        var account = await _accountService.GetAccountByIdAsync(id);
        if (account == null)
        {
            return NotFound();
        }
        
        var viewModel = new AccountViewModel
        {
            Id = account.Id,
            Name = account.Name,
            Type = account.Type,
            IsActive = account.IsActive
        };
        
        ViewBag.AccountTypes = AccountTypes.Types;
        return View(viewModel);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AccountViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            return NotFound();
        }
        
        if (ModelState.IsValid)
        {
            var account = new Account
            {
                Id = viewModel.Id,
                Name = viewModel.Name,
                Type = viewModel.Type,
                IsActive = viewModel.IsActive
            };
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _accountService.UpdateAccountAsync(account, userId);
            
            TempData["Success"] = "Account updated successfully.";
            return RedirectToAction(nameof(Index));
        }
        
        ViewBag.AccountTypes = AccountTypes.Types;
        return View(viewModel);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _accountService.DeleteAccountAsync(id, userId);
        
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
