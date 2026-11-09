using DeskFlow.Web.Data;
using DeskFlow.Web.Models;
using DeskFlow.Web.ViewModels.Staff;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeskFlow.Web.Controllers;

[Authorize(Roles = ApplicationRoles.HelpdeskManager)]
[Route("staff/settings/sla")]
public class SlaSettingsController : Controller
{
    private const decimal MaxTargetHours = 720m;
    private readonly ApplicationDbContext _db;

    public SlaSettingsController(ApplicationDbContext db) => _db = db;

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var priorities = await _db.TicketPriorities.AsNoTracking()
            .Include(priority => priority.SlaRule)
            .Where(priority => priority.IsActive)
            .OrderByDescending(priority => priority.SortOrder)
            .ToListAsync();

        return View(new SlaTargetsViewModel
        {
            Rules = priorities.Select(priority => new SlaTargetEditRowViewModel
            {
                PriorityId = priority.Id,
                PriorityName = priority.Name,
                ResponseTargetHours = priority.SlaRule is { IsActive: true }
                    ? priority.SlaRule.ResponseTargetMinutes / 60m : null,
                ResolutionTargetHours = priority.SlaRule is { IsActive: true }
                    ? priority.SlaRule.ResolutionTargetMinutes / 60m : null
            }).ToList()
        });
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(SlaTargetsViewModel model)
    {
        var priorities = await _db.TicketPriorities
            .Include(priority => priority.SlaRule)
            .Where(priority => priority.IsActive)
            .OrderByDescending(priority => priority.SortOrder)
            .ToListAsync();

        var submittedIds = model.Rules.Select(rule => rule.PriorityId).ToArray();
        var expectedIds = priorities.Select(priority => priority.Id).ToHashSet();
        if (submittedIds.Distinct().Count() != submittedIds.Length
            || !submittedIds.ToHashSet().SetEquals(expectedIds))
        {
            ModelState.AddModelError(string.Empty, "The priority list changed. Reload the page and try again.");
        }

        if (!ModelState.IsValid)
        {
            var names = priorities.ToDictionary(priority => priority.Id, priority => priority.Name);
            foreach (var rule in model.Rules)
            {
                rule.PriorityName = names.GetValueOrDefault(rule.PriorityId, "Priority");
            }
            return View("Index", model);
        }

        var submittedById = model.Rules.ToDictionary(rule => rule.PriorityId);
        foreach (var priority in priorities)
        {
            var target = submittedById[priority.Id];
            var rule = priority.SlaRule;
            if (rule is null)
            {
                rule = new SlaRule { PriorityId = priority.Id };
                _db.SlaRules.Add(rule);
            }

            rule.ResponseTargetMinutes = ToMinutes(target.ResponseTargetHours!.Value);
            rule.ResolutionTargetMinutes = ToMinutes(target.ResolutionTargetHours!.Value);
            rule.IsActive = true;
        }

        await _db.SaveChangesAsync();
        TempData["SlaNotice"] = "SLA targets saved. The new targets are now in use.";
        return RedirectToAction(nameof(Index));
    }

    private static int ToMinutes(decimal hours)
    {
        if (hours <= 0 || hours > MaxTargetHours || hours * 60m % 15m != 0)
        {
            throw new InvalidOperationException("SLA target values must be within the allowed range and use 15-minute increments.");
        }

        return decimal.ToInt32(hours * 60m);
    }
}
