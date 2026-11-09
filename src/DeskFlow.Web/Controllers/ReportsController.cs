using DeskFlow.Web.Data;
using DeskFlow.Web.Models;
using DeskFlow.Web.Services;
using DeskFlow.Web.ViewModels.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeskFlow.Web.Controllers;

[Authorize(Roles = ApplicationRoles.HelpdeskManager)]
[Route("staff/reports")]
public sealed class ReportsController : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(int days = 30)
    {
        if (days is not (7 or 30 or 90)) return BadRequest();

        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
        var startDate = today.AddDays(-(days - 1));
        var endDateExclusive = today.AddDays(1);
        var startUtc = new DateTimeOffset(startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        var endUtc = new DateTimeOffset(endDateExclusive.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));

        var activity = await _db.Tickets.AsNoTracking()
            .Where(ticket => (ticket.CreatedAt >= startUtc && ticket.CreatedAt < endUtc)
                || (ticket.ResolvedAt >= startUtc && ticket.ResolvedAt < endUtc))
            .Select(ticket => new { ticket.CreatedAt, ticket.ResolvedAt })
            .ToListAsync();

        var periodDays = days == 7 ? 1 : 7;
        var periods = new List<TicketActivityPeriodViewModel>();
        for (var offset = 0; offset < days; offset += periodDays)
        {
            var periodStart = startDate.AddDays(offset);
            var periodEnd = periodStart.AddDays(Math.Min(periodDays, days - offset) - 1);
            var label = periodDays == 1
                ? periodStart.ToString("ddd, d MMM")
                : $"{periodStart:d MMM} – {periodEnd:d MMM}";
            periods.Add(new TicketActivityPeriodViewModel { Label = label });
        }

        var openedCount = 0;
        var resolvedCount = 0;
        foreach (var ticket in activity)
        {
            if (ticket.CreatedAt >= startUtc && ticket.CreatedAt < endUtc)
            {
                openedCount++;
                var index = (DateOnly.FromDateTime(ticket.CreatedAt.UtcDateTime).DayNumber - startDate.DayNumber) / periodDays;
                periods[index].OpenedCount++;
            }

            if (ticket.ResolvedAt is { } resolvedAt && resolvedAt >= startUtc && resolvedAt < endUtc)
            {
                resolvedCount++;
                var index = (DateOnly.FromDateTime(resolvedAt.UtcDateTime).DayNumber - startDate.DayNumber) / periodDays;
                periods[index].ResolvedCount++;
            }
        }

        var openTickets = await _db.Tickets.AsNoTracking()
            .Where(ticket => !ticket.Status.IsFinal)
            .Select(ticket => new
            {
                ticket.CreatedAt,
                ticket.FirstRespondedAt,
                ticket.ResolvedAt,
                ResponseTargetMinutes = ticket.Priority.SlaRule != null && ticket.Priority.SlaRule.IsActive
                    ? (int?)ticket.Priority.SlaRule.ResponseTargetMinutes : null,
                ResolutionTargetMinutes = ticket.Priority.SlaRule != null && ticket.Priority.SlaRule.IsActive
                    ? (int?)ticket.Priority.SlaRule.ResolutionTargetMinutes : null
            })
            .ToListAsync();

        var now = DateTimeOffset.UtcNow;
        var overdueSlaCount = openTickets.Count(ticket =>
            SlaProgressCalculator.Calculate(
                "Reply", ticket.CreatedAt, ticket.ResponseTargetMinutes, ticket.FirstRespondedAt, now).StateClass == "overdue"
            || SlaProgressCalculator.Calculate(
                "Resolution", ticket.CreatedAt, ticket.ResolutionTargetMinutes, ticket.ResolvedAt, now).StateClass == "overdue");

        return View(new TicketActivityReportViewModel
        {
            RangeDays = days,
            StartDate = startDate,
            EndDate = today,
            OpenedCount = openedCount,
            ResolvedCount = resolvedCount,
            OpenCount = openTickets.Count,
            OverdueSlaCount = overdueSlaCount,
            BucketName = days == 7 ? "day" : "week",
            Periods = periods
        });
    }

    private readonly ApplicationDbContext _db;

    public ReportsController(ApplicationDbContext db) => _db = db;
}
