using DeskFlow.Web.Data;
using DeskFlow.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeskFlow.Web.ViewComponents;

public class UnreadNotificationsViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public UnreadNotificationsViewComponent(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = _users.GetUserId(HttpContext.User);
        var count = userId is null
            ? 0
            : await _db.Notifications.CountAsync(notification =>
                notification.RecipientId == userId && notification.ReadAt == null);
        return View(count);
    }
}
