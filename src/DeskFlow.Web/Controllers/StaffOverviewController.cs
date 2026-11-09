using DeskFlow.Web.Data;
using DeskFlow.Web.Models;
using DeskFlow.Web.Services;
using DeskFlow.Web.ViewModels.Staff;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeskFlow.Web.Controllers;

[Authorize(Roles = ApplicationRoles.Technician + "," + ApplicationRoles.HelpdeskManager)]
[Route("staff/overview")]
public sealed class StaffOverviewController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public StaffOverviewController(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var openTickets = _db.Tickets.AsNoTracking().Where(ticket => !ticket.Status.IsFinal);
        var openCount = await openTickets.CountAsync();
        var unassignedCount = await openTickets.CountAsync(ticket =>
            !ticket.Assignments.Any(assignment => assignment.UnassignedAt == null));

        var userId = _users.GetUserId(User);
        var mineOpenCount = userId is null
            ? 0
            : await openTickets.CountAsync(ticket => ticket.Assignments.Any(assignment =>
                assignment.UnassignedAt == null && assignment.TechnicianId == userId));

        var targets = await openTickets.Select(ticket => new
        {
            ticket.CreatedAt,
            ticket.FirstRespondedAt,
            ticket.ResolvedAt,
            ResponseTargetMinutes = ticket.Priority.SlaRule != null && ticket.Priority.SlaRule.IsActive
                ? (int?)ticket.Priority.SlaRule.ResponseTargetMinutes : null,
            ResolutionTargetMinutes = ticket.Priority.SlaRule != null && ticket.Priority.SlaRule.IsActive
                ? (int?)ticket.Priority.SlaRule.ResolutionTargetMinutes : null
        }).ToListAsync();

        var now = DateTimeOffset.UtcNow;
        var overdueSlaCount = targets.Count(ticket =>
            SlaProgressCalculator.Calculate(
                "Reply", ticket.CreatedAt, ticket.ResponseTargetMinutes, ticket.FirstRespondedAt, now).StateClass == "overdue"
            || SlaProgressCalculator.Calculate(
                "Resolution", ticket.CreatedAt, ticket.ResolutionTargetMinutes, ticket.ResolvedAt, now).StateClass == "overdue");

        return View(new StaffOverviewViewModel
        {
            OpenCount = openCount,
            MineOpenCount = mineOpenCount,
            UnassignedCount = unassignedCount,
            OverdueSlaCount = overdueSlaCount
        });
    }
}
