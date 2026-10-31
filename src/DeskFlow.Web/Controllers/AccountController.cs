using DeskFlow.Web.Data;
using DeskFlow.Web.Models;
using DeskFlow.Web.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeskFlow.Web.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountController(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        return View(new RegisterViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var email = model.Email.Trim();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email
        };

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        var createResult = await _userManager.CreateAsync(user, model.Password);
        if (!createResult.Succeeded)
        {
            await transaction.RollbackAsync();
            AddIdentityErrors(createResult);
            return View(model);
        }

        _dbContext.UserProfiles.Add(new UserProfile
        {
            UserId = user.Id,
            DisplayName = model.DisplayName.Trim(),
            Department = string.IsNullOrWhiteSpace(model.Department) ? null : model.Department.Trim()
        });
        await _dbContext.SaveChangesAsync();

        var roleResult = await _userManager.AddToRoleAsync(user, ApplicationRoles.EndUser);
        if (!roleResult.Succeeded)
        {
            await transaction.RollbackAsync();
            AddIdentityErrors(roleResult);
            return View(model);
        }

        await transaction.CommitAsync();
        await _signInManager.SignInAsync(user, isPersistent: false);

        if (Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl!);
        }

        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            model.Email.Trim(),
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            if (Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl!);
            }

            return RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError(
            string.Empty,
            result.IsLockedOut
                ? "This account is temporarily locked. Try again later."
                : "The email address or password is incorrect.");

        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var profile = await _dbContext.UserProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.UserId == user.Id);

        return View(new ProfileViewModel
        {
            DisplayName = profile?.DisplayName ?? user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Department = profile?.Department,
            Phone = profile?.Phone
        });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        model.Email = user.Email ?? string.Empty;
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var profile = await _dbContext.UserProfiles
            .SingleOrDefaultAsync(item => item.UserId == user.Id);

        if (profile is null)
        {
            profile = new UserProfile { UserId = user.Id };
            _dbContext.UserProfiles.Add(profile);
        }

        profile.DisplayName = model.DisplayName.Trim();
        profile.Department = string.IsNullOrWhiteSpace(model.Department) ? null : model.Department.Trim();
        profile.Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();

        await _dbContext.SaveChangesAsync();
        TempData["SuccessMessage"] = "Your profile has been updated.";
        return RedirectToAction(nameof(Profile));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }
}
