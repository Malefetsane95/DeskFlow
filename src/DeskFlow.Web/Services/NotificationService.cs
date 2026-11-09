using DeskFlow.Web.Data;
using DeskFlow.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DeskFlow.Web.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public NotificationService(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public async Task NotifyNewTicketAsync(Ticket ticket, string actorId)
    {
        var managers = await _users.GetUsersInRoleAsync(ApplicationRoles.HelpdeskManager);
        AddNotifications(
            managers.Select(manager => manager.Id), actorId, ticket,
            "NewTicket", $"New request {ticket.Reference} is waiting in the team queue.");
    }

    public void NotifyAssignment(Ticket ticket, string technicianId, string actorId)
    {
        AddNotifications(
            [technicianId], actorId, ticket,
            "TicketAssigned", $"You’ve been assigned request {ticket.Reference}.");
    }

    public void NotifyStatusChange(Ticket ticket, string statusName, string actorId)
    {
        AddNotifications(
            [ticket.RequesterId], actorId, ticket,
            "TicketStatusChanged", $"The status of request {ticket.Reference} changed to {statusName}.");
    }

    public async Task NotifyPublicReplyAsync(Ticket ticket, string actorId, bool actorIsStaff)
    {
        if (actorIsStaff)
        {
            AddNotifications(
                [ticket.RequesterId], actorId, ticket,
                "PublicReply", $"The IT team replied to request {ticket.Reference}.");
            return;
        }

        var technicianId = await _db.TicketAssignments.AsNoTracking()
            .Where(assignment => assignment.TicketId == ticket.Id && assignment.UnassignedAt == null)
            .Select(assignment => assignment.TechnicianId)
            .SingleOrDefaultAsync();
        if (!string.IsNullOrWhiteSpace(technicianId))
        {
            AddNotifications(
                [technicianId], actorId, ticket,
                "PublicReply", $"The requester replied to {ticket.Reference}.");
            return;
        }

        var managers = await _users.GetUsersInRoleAsync(ApplicationRoles.HelpdeskManager);
        AddNotifications(
            managers.Select(manager => manager.Id), actorId, ticket,
            "PublicReply", $"The requester replied to {ticket.Reference}.");
    }

    private void AddNotifications(
        IEnumerable<string> recipientIds,
        string actorId,
        Ticket ticket,
        string type,
        string message)
    {
        var createdAt = DateTimeOffset.UtcNow;
        var notifications = recipientIds
            .Where(recipientId => recipientId != actorId)
            .Distinct(StringComparer.Ordinal)
            .Select(recipientId => new Notification
            {
                RecipientId = recipientId,
                TicketId = ticket.Id,
                Type = type,
                Message = message,
                CreatedAt = createdAt
            });

        _db.Notifications.AddRange(notifications);
    }
}
