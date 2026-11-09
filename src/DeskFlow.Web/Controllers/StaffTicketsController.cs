using DeskFlow.Web.Data;
using DeskFlow.Web.Models;
using DeskFlow.Web.ViewModels.Staff;
using DeskFlow.Web.ViewModels.Tickets;
using DeskFlow.Web.ViewModels.Attachments;
using DeskFlow.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DeskFlow.Web.Controllers;

[Authorize(Roles = ApplicationRoles.Technician + "," + ApplicationRoles.HelpdeskManager)]
[Route("staff/tickets")]
public class StaffTicketsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly INotificationService _notifications;

    public StaffTicketsController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> users,
        INotificationService notifications)
    {
        _db = db;
        _users = users;
        _notifications = notifications;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, string? statusCode)
    {
        var query = _db.Tickets.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(statusCode))
        {
            query = query.Where(ticket => ticket.Status.Code == statusCode);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(ticket => ticket.Reference.Contains(term)
                || ticket.Title.Contains(term)
                || ticket.Requester.Profile!.DisplayName.Contains(term)
                || ticket.Requester.Email!.Contains(term));
        }

        var rows = await query
            .OrderBy(ticket => ticket.Status.IsFinal)
            .ThenByDescending(ticket => ticket.Priority.SortOrder)
            .ThenBy(ticket => ticket.CreatedAt)
            .Take(150)
            .Select(ticket => new StaffTicketRowViewModel
            {
                Id = ticket.Id,
                Reference = ticket.Reference,
                Title = ticket.Title,
                Requester = ticket.Requester.Profile!.DisplayName,
                Category = ticket.Category.Name,
                Priority = ticket.Priority.Name,
                PriorityOrder = ticket.Priority.SortOrder,
                Status = ticket.Status.Name,
                StatusCode = ticket.Status.Code,
                Technician = ticket.Assignments.Where(assignment => assignment.UnassignedAt == null)
                    .Select(assignment => assignment.Technician.Profile!.DisplayName).FirstOrDefault(),
                CreatedAt = ticket.CreatedAt
            })
            .ToListAsync();

        var statuses = await _db.TicketStatuses.AsNoTracking().OrderBy(status => status.SortOrder)
            .Select(status => new SelectListItem(status.Name, status.Code, status.Code == statusCode))
            .ToListAsync();

        return View(new StaffTicketListViewModel
        {
            Search = search,
            StatusCode = statusCode,
            Statuses = statuses,
            Tickets = rows
        });
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Details(long id)
    {
        var ticket = await _db.Tickets
            .Include(item => item.Requester).ThenInclude(user => user.Profile)
            .Include(item => item.Category)
            .Include(item => item.Priority)
            .Include(item => item.Status)
            .Include(item => item.Assignments.Where(assignment => assignment.UnassignedAt == null))
                .ThenInclude(assignment => assignment.Technician).ThenInclude(user => user.Profile)
            .Include(item => item.History).ThenInclude(history => history.Actor).ThenInclude(user => user.Profile)
            .Include(item => item.Comments).ThenInclude(comment => comment.Author).ThenInclude(user => user.Profile)
            .Include(item => item.Attachments).ThenInclude(attachment => attachment.UploadedBy).ThenInclude(user => user.Profile)
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id);

        if (ticket is null) return NotFound();
        return View(await BuildDetailViewModelAsync(ticket));
    }

    [HttpPost("{id:long}/claim")]
    [Authorize(Roles = ApplicationRoles.Technician)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Claim(long id)
    {
        var userId = _users.GetUserId(User);
        if (userId is null) return Challenge();

        var ticket = await _db.Tickets.Include(item => item.Status)
            .Include(item => item.Assignments.Where(assignment => assignment.UnassignedAt == null))
            .SingleOrDefaultAsync(item => item.Id == id);
        if (ticket is null) return NotFound();

        if (ticket.Status.Code is "RESOLVED" or "CLOSED")
        {
            TempData["StaffError"] = "Resolved or closed requests can’t be claimed. Reopen the request first.";
            return RedirectToAction(nameof(Details), new { id });
        }
        if (ticket.Assignments.Any())
        {
            TempData["StaffError"] = "This request is already assigned. Ask a Helpdesk Manager to reassign it.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var now = DateTimeOffset.UtcNow;
        ticket.Assignments.Add(new TicketAssignment
        {
            TechnicianId = userId,
            AssignedById = userId,
            AssignedAt = now
        });
        _notifications.NotifyAssignment(ticket, userId, userId);
        _db.TicketHistory.Add(new TicketHistory
        {
            TicketId = ticket.Id,
            ActorId = userId,
            EventType = "Assigned",
            NewValue = "Claimed by " + (User.Identity?.Name ?? "technician"),
            OccurredAt = now
        });
        if (ticket.Status.Code == "OPEN")
        {
            await SetStatusAsync(ticket, "ASSIGNED", userId, now);
        }

        try
        {
            await _db.SaveChangesAsync();
            TempData["StaffNotice"] = "You’re now assigned to this request.";
        }
        catch (DbUpdateException exception) when (IsActiveAssignmentConflict(exception))
        {
            _db.ChangeTracker.Clear();
            TempData["StaffError"] = "Someone else claimed this request first. Refresh the queue to see the latest assignment.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:long}/assign")]
    [Authorize(Roles = ApplicationRoles.HelpdeskManager)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(long id, string technicianId)
    {
        var actorId = _users.GetUserId(User);
        if (actorId is null) return Challenge();

        var eligibleTechnicians = await _users.GetUsersInRoleAsync(ApplicationRoles.Technician);
        var selectedTechnician = eligibleTechnicians.SingleOrDefault(user => user.Id == technicianId);
        var ticket = await _db.Tickets.Include(item => item.Status)
            .Include(item => item.Assignments.Where(assignment => assignment.UnassignedAt == null))
            .SingleOrDefaultAsync(item => item.Id == id);
        if (ticket is null) return NotFound();
        if (selectedTechnician is null)
        {
            TempData["StaffError"] = "Choose an active IT Technician to assign this request.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var now = DateTimeOffset.UtcNow;
        var activeAssignment = ticket.Assignments.SingleOrDefault();
        if (activeAssignment?.TechnicianId == selectedTechnician.Id)
        {
            TempData["StaffNotice"] = "This request is already assigned to that technician.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (activeAssignment is not null) activeAssignment.UnassignedAt = now;
        ticket.Assignments.Add(new TicketAssignment
        {
            TechnicianId = selectedTechnician.Id,
            AssignedById = actorId,
            AssignedAt = now
        });
        _notifications.NotifyAssignment(ticket, selectedTechnician.Id, actorId);

        var selectedName = await _db.UserProfiles.AsNoTracking()
            .Where(profile => profile.UserId == selectedTechnician.Id)
            .Select(profile => profile.DisplayName).SingleOrDefaultAsync() ?? selectedTechnician.Email ?? "IT Technician";
        _db.TicketHistory.Add(new TicketHistory
        {
            TicketId = ticket.Id,
            ActorId = actorId,
            EventType = activeAssignment is null ? "Assigned" : "Reassigned",
            PreviousValue = activeAssignment is null ? null : "Assigned to another technician",
            NewValue = "Assigned to " + selectedName,
            OccurredAt = now
        });
        if (ticket.Status.Code == "OPEN") await SetStatusAsync(ticket, "ASSIGNED", actorId, now);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (IsActiveAssignmentConflict(exception))
        {
            _db.ChangeTracker.Clear();
            TempData["StaffError"] = "The assignment changed while you were updating it. Refresh the request and try again.";
            return RedirectToAction(nameof(Details), new { id });
        }
        TempData["StaffNotice"] = $"Request assigned to {selectedName}.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:long}/status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(long id, int statusId)
    {
        var actorId = _users.GetUserId(User);
        if (actorId is null) return Challenge();

        var ticket = await _db.Tickets.Include(item => item.Status)
            .Include(item => item.Assignments.Where(assignment => assignment.UnassignedAt == null))
            .SingleOrDefaultAsync(item => item.Id == id);
        if (ticket is null) return NotFound();

        var nextStatus = await _db.TicketStatuses.SingleOrDefaultAsync(status => status.Id == statusId);
        if (nextStatus is null)
        {
            TempData["StaffError"] = "Choose a valid status.";
            return RedirectToAction(nameof(Details), new { id });
        }
        if (nextStatus.Code == ticket.Status.Code)
        {
            TempData["StaffNotice"] = "The request already has that status.";
            return RedirectToAction(nameof(Details), new { id });
        }
        if (nextStatus.Code == "ASSIGNED" && !ticket.Assignments.Any())
        {
            TempData["StaffError"] = "Assign the request to a technician before setting it to Assigned.";
            return RedirectToAction(nameof(Details), new { id });
        }

        await SetStatusAsync(ticket, nextStatus.Code, actorId, DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync();
        TempData["StaffNotice"] = $"Status changed to {nextStatus.Name}.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:long}/comment")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(long id, AddTicketCommentViewModel model)
    {
        var actorId = _users.GetUserId(User);
        if (actorId is null) return Challenge();

        if (string.IsNullOrWhiteSpace(model.Body))
        {
            TempData["StaffError"] = "Write a note or reply before sending it.";
            return RedirectToAction(nameof(Details), new { id });
        }
        if (!ModelState.IsValid)
        {
            TempData["StaffError"] = "Check the visibility and keep the message under 4,000 characters.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var ticket = await _db.Tickets.SingleOrDefaultAsync(item => item.Id == id);
        if (ticket is null) return NotFound();

        var now = DateTimeOffset.UtcNow;
        var comment = new TicketComment
        {
            TicketId = id,
            AuthorId = actorId,
            Visibility = model.Visibility,
            Body = model.Body.Trim(),
            CreatedAt = now
        };
        _db.TicketComments.Add(comment);
        if (model.Visibility == CommentVisibility.Public)
        {
            await _notifications.NotifyPublicReplyAsync(ticket, actorId, actorIsStaff: true);
        }
        if (model.Visibility == CommentVisibility.Public && ticket.FirstRespondedAt is null)
        {
            ticket.FirstRespondedAt = now;
        }
        await _db.SaveChangesAsync();
        TempData["StaffNotice"] = model.Visibility == CommentVisibility.Internal
            ? "Internal note saved for the IT team."
            : "Public reply sent to the requester.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<StaffTicketDetailViewModel> BuildDetailViewModelAsync(Ticket ticket)
    {
        var statuses = await _db.TicketStatuses.AsNoTracking().OrderBy(status => status.SortOrder)
            .Select(status => new SelectListItem(status.Name, status.Id.ToString(), status.Id == ticket.StatusId))
            .ToListAsync();

        var technicians = await _users.GetUsersInRoleAsync(ApplicationRoles.Technician);
        var technicianProfiles = await _db.UserProfiles.AsNoTracking()
            .Where(profile => technicians.Select(user => user.Id).Contains(profile.UserId))
            .ToDictionaryAsync(profile => profile.UserId, profile => profile.DisplayName);
        var currentAssignment = ticket.Assignments.SingleOrDefault();

        return new StaffTicketDetailViewModel
        {
            Id = ticket.Id,
            Reference = ticket.Reference,
            Title = ticket.Title,
            Description = ticket.Description,
            Requester = ticket.Requester.Profile?.DisplayName ?? ticket.Requester.Email ?? "Requester",
            RequesterEmail = ticket.Requester.Email,
            Department = ticket.Requester.Profile?.Department,
            Category = ticket.Category.Name,
            Priority = ticket.Priority.Name,
            Status = ticket.Status.Name,
            StatusId = ticket.StatusId,
            StatusCode = ticket.Status.Code,
            TechnicianId = currentAssignment?.TechnicianId,
            Technician = currentAssignment?.Technician.Profile?.DisplayName,
            CreatedAt = ticket.CreatedAt,
            Statuses = statuses,
            Technicians = technicians
                .OrderBy(user => technicianProfiles.GetValueOrDefault(user.Id, user.Email ?? "IT Technician"))
                .Select(user => new SelectListItem(
                    technicianProfiles.GetValueOrDefault(user.Id, user.Email ?? "IT Technician"),
                    user.Id,
                    user.Id == currentAssignment?.TechnicianId))
                .ToList(),
            History = ticket.History.OrderBy(history => history.OccurredAt)
                .Select(history => new StaffTicketHistoryViewModel
                {
                    Actor = history.Actor.Profile?.DisplayName ?? history.Actor.Email ?? "DeskFlow",
                    EventType = history.EventType,
                    PreviousValue = history.PreviousValue,
                    NewValue = history.NewValue,
                    OccurredAt = history.OccurredAt
                })
                .ToList(),
            Conversation = ticket.Comments.OrderBy(comment => comment.CreatedAt)
                .Select(comment => new StaffTicketCommentViewModel
                {
                    Author = comment.Author.Profile?.DisplayName ?? comment.Author.Email ?? "DeskFlow",
                    Body = comment.Body,
                    Visibility = comment.Visibility == CommentVisibility.Internal ? "Internal note · staff only" : "Public reply",
                    CreatedAt = comment.CreatedAt
                })
                .ToList(),
            Attachments = ticket.Attachments.OrderBy(attachment => attachment.CreatedAt)
                .Select(attachment => new TicketAttachmentViewModel
                {
                    Id = attachment.Id,
                    FileName = attachment.OriginalFileName,
                    ContentType = attachment.ContentType,
                    SizeBytes = attachment.SizeBytes,
                    UploadedBy = attachment.UploadedBy.Profile?.DisplayName ?? attachment.UploadedBy.Email ?? "DeskFlow",
                    CreatedAt = attachment.CreatedAt
                })
                .ToList()
        };
    }

    private async Task SetStatusAsync(Ticket ticket, string code, string actorId, DateTimeOffset now)
    {
        var nextStatus = await _db.TicketStatuses.SingleAsync(status => status.Code == code);
        var previousStatus = ticket.Status;
        ticket.Status = nextStatus;
        ticket.StatusId = nextStatus.Id;
        if (ticket.FirstRespondedAt is null) ticket.FirstRespondedAt = now;

        if (code == "RESOLVED") ticket.ResolvedAt = now;
        else if (code == "CLOSED")
        {
            ticket.ClosedAt = now;
            ticket.ResolvedAt ??= now;
        }
        else if (code == "REOPENED")
        {
            ticket.ResolvedAt = null;
            ticket.ClosedAt = null;
        }

        _db.TicketHistory.Add(new TicketHistory
        {
            TicketId = ticket.Id,
            ActorId = actorId,
            EventType = "StatusChanged",
            PreviousValue = previousStatus.Name,
            NewValue = nextStatus.Name,
            OccurredAt = now
        });
        _notifications.NotifyStatusChange(ticket, nextStatus.Name, actorId);
    }

    private static bool IsActiveAssignmentConflict(DbUpdateException exception) =>
        exception.InnerException is PostgresException postgres
        && postgres.SqlState == PostgresErrorCodes.UniqueViolation
        && postgres.ConstraintName == "IX_TicketAssignments_OneActiveAssignmentPerTicket";
}
