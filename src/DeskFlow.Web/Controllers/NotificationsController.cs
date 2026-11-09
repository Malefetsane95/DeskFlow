using DeskFlow.Web.Data;
using DeskFlow.Web.Models;
using DeskFlow.Web.ViewModels.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeskFlow.Web.Controllers;

[Authorize]
[Route("notifications")]
public class NotificationsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public NotificationsController(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var userId = _users.GetUserId(User);
        if (userId is null) return Challenge();

        var notifications = await _db.Notifications.AsNoTracking()
            .Where(notification => notification.RecipientId == userId)
            .OrderByDescending(notification => notification.CreatedAt)
            .Take(50)
            .Select(notification => new NotificationListItemViewModel
            {
                Id = notification.Id,
                Message = notification.Message,
                TicketReference = notification.Ticket == null ? string.Empty : notification.Ticket.Reference,
                CreatedAt = notification.CreatedAt,
                IsRead = notification.ReadAt != null
            })
            .ToListAsync();

        return View(notifications);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Open(long id)
    {
        var userId = _users.GetUserId(User);
        if (userId is null) return Challenge();

        var notification = await _db.Notifications
            .SingleOrDefaultAsync(item => item.Id == id && item.RecipientId == userId);
        if (notification is null) return NotFound();

        if (notification.ReadAt is null)
        {
            notification.ReadAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync();
        }

        if (notification.TicketId is null) return RedirectToAction(nameof(Index));

        if (User.IsInRole(ApplicationRoles.Technician) || User.IsInRole(ApplicationRoles.HelpdeskManager))
        {
            return RedirectToAction("Details", "StaffTickets", new { id = notification.TicketId.Value });
        }

        return RedirectToAction("Details", "Tickets", new { id = notification.TicketId.Value });
    }
}
