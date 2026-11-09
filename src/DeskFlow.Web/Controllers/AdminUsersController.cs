using DeskFlow.Web.Data;
using DeskFlow.Web.Models;
using DeskFlow.Web.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeskFlow.Web.Controllers;

[Authorize(Roles = ApplicationRoles.Administrator)]
[Route("admin/users")]
public sealed class AdminUsersController : Controller
{
    private const int PageSize = 25;
    private const int MaximumPage = 1_000_000;
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<IdentityRole> _roles;

    public AdminUsersController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> users,
        RoleManager<IdentityRole> roles)
    {
        _db = db;
        _users = users;
        _roles = roles;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        search = search?.Trim();
        if (search?.Length > 120 || page < 1 || page > MaximumPage) return BadRequest();

        var query = _db.UserProfiles.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(profile => profile.DisplayName.Contains(search)
                || (profile.User.Email != null && profile.User.Email.Contains(search)));
        }

        var totalUsers = await query.CountAsync();
        var users = await query
            .OrderBy(profile => profile.DisplayName)
            .ThenBy(profile => profile.UserId)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(profile => new
            {
                Id = profile.UserId,
                profile.DisplayName,
                profile.User.Email
            })
            .ToListAsync();

        var userIds = users.Select(user => user.Id).ToArray();
        var roleRows = await (from userRole in _db.UserRoles.AsNoTracking()
                              join role in _db.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                              where userIds.Contains(userRole.UserId)
                              select new { userRole.UserId, RoleName = role.Name! })
            .ToListAsync();
        var rolesByUser = roleRows.GroupBy(row => row.UserId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<string>)group.Select(row => row.RoleName).ToArray());

        return View(new AdminUsersViewModel
        {
            Search = search,
            Page = page,
            PageSize = PageSize,
            TotalUsers = totalUsers,
            TotalPages = Math.Max(1, (int)Math.Ceiling(totalUsers / (double)PageSize)),
            Users = users.Select(user => new AdminUserRowViewModel
            {
                Id = user.Id,
                DisplayName = user.DisplayName,
                Email = user.Email,
                Roles = rolesByUser.GetValueOrDefault(user.Id, [])
            }).ToList()
        });
    }

    [HttpPost("role")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRole(
        string userId,
        string role,
        bool enabled,
        string? search,
        int page = 1)
    {
        search = search?.Trim();
        if (page < 1 || page > MaximumPage) page = 1;
        if (search?.Length > 120) search = search[..120];

        if (string.IsNullOrWhiteSpace(userId)
            || role is not (ApplicationRoles.Technician or ApplicationRoles.HelpdeskManager))
        {
            TempData["AdminUsersError"] = "That role change is not available.";
            return RedirectToAction(nameof(Index), new { search, page });
        }

        var user = await _users.FindByIdAsync(userId);
        if (user is null)
        {
            TempData["AdminUsersError"] = "That account could not be found. Refresh the list and try again.";
            return RedirectToAction(nameof(Index), new { search, page });
        }

        if (!await _roles.RoleExistsAsync(role))
        {
            TempData["AdminUsersError"] = "The selected role is unavailable. Contact the system administrator.";
            return RedirectToAction(nameof(Index), new { search, page });
        }

        var alreadyEnabled = await _users.IsInRoleAsync(user, role);
        if (alreadyEnabled != enabled)
        {
            var result = enabled
                ? await _users.AddToRoleAsync(user, role)
                : await _users.RemoveFromRoleAsync(user, role);

            if (!result.Succeeded)
            {
                TempData["AdminUsersError"] = "The role change could not be saved. Refresh the list and try again.";
                return RedirectToAction(nameof(Index), new { search, page });
            }
        }

        var displayName = await _db.UserProfiles.AsNoTracking()
            .Where(profile => profile.UserId == userId)
            .Select(profile => profile.DisplayName)
            .SingleOrDefaultAsync() ?? user.Email ?? "The account";
        TempData["AdminUsersNotice"] = enabled
            ? $"{role} access granted to {displayName}. It will be available after their next sign-in."
            : $"{role} access removed from {displayName}. The change takes effect after their next sign-in.";

        return RedirectToAction(nameof(Index), new { search, page });
    }
}
