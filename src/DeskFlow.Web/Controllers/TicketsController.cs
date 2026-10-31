using System.Security.Cryptography;
using DeskFlow.Web.Data;
using DeskFlow.Web.Models;
using DeskFlow.Web.ViewModels.Tickets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DeskFlow.Web.Controllers;

[Authorize]
public class TicketsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public TicketsController(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _users.GetUserId(User);
        if (userId is null) return Challenge();

        var tickets = await _db.Tickets.AsNoTracking()
            .Where(ticket => ticket.RequesterId == userId)
            .OrderByDescending(ticket => ticket.CreatedAt)
            .Take(100)
            .Select(ticket => new TicketListItemViewModel
            {
                Id = ticket.Id,
                Reference = ticket.Reference,
                Title = ticket.Title,
                Category = ticket.Category.Name,
                Status = ticket.Status.Name,
                Priority = ticket.Priority.Name,
                CreatedAt = ticket.CreatedAt,
                UpdatedAt = ticket.History.OrderByDescending(history => history.OccurredAt)
                    .Select(history => (DateTimeOffset?)history.OccurredAt).FirstOrDefault()
            })
            .ToListAsync();

        return View(tickets);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadCategoriesAsync();
        return View(new CreateTicketViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateTicketViewModel model)
    {
        var userId = _users.GetUserId(User);
        if (userId is null) return Challenge();

        if (ModelState.IsValid && !await _db.TicketCategories.AnyAsync(category => category.Id == model.CategoryId && category.IsActive))
        {
            ModelState.AddModelError(nameof(model.CategoryId), "Choose one of the available categories.");
        }

        if (!ModelState.IsValid)
        {
            await LoadCategoriesAsync();
            return View(model);
        }

        var statusId = await _db.TicketStatuses.Where(status => status.Code == "OPEN").Select(status => status.Id).SingleAsync();
        var priorityId = await _db.TicketPriorities.Where(priority => priority.Code == "MEDIUM").Select(priority => priority.Id).SingleAsync();
        var ticket = new Ticket
        {
            Reference = await CreateReferenceAsync(),
            Title = model.Title.Trim(),
            Description = model.Description.Trim(),
            RequesterId = userId,
            CategoryId = model.CategoryId,
            PriorityId = priorityId,
            StatusId = statusId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await using var transaction = await _db.Database.BeginTransactionAsync();
        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();
        _db.TicketHistory.Add(new TicketHistory
        {
            TicketId = ticket.Id,
            ActorId = userId,
            EventType = "Created",
            NewValue = "Open",
            OccurredAt = ticket.CreatedAt
        });
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["TicketCreated"] = ticket.Reference;
        return RedirectToAction(nameof(Details), new { id = ticket.Id });
    }

    public async Task<IActionResult> Details(long id)
    {
        var userId = _users.GetUserId(User);
        if (userId is null) return Challenge();

        var ticket = await _db.Tickets.AsNoTracking()
            .Include(item => item.Category)
            .Include(item => item.Priority)
            .Include(item => item.Status)
            .Include(item => item.History.OrderBy(history => history.OccurredAt))
            .Where(item => item.Id == id && item.RequesterId == userId)
            .Select(item => new TicketDetailViewModel
            {
                Id = item.Id,
                Reference = item.Reference,
                Title = item.Title,
                Description = item.Description,
                Category = item.Category.Name,
                Priority = item.Priority.Name,
                Status = item.Status.Name,
                CreatedAt = item.CreatedAt,
                History = item.History.Select(history => new TicketTimelineItemViewModel
                {
                    EventType = history.EventType,
                    NewValue = history.NewValue,
                    OccurredAt = history.OccurredAt
                }).ToList()
            })
            .SingleOrDefaultAsync();

        return ticket is null ? NotFound() : View(ticket);
    }

    private async Task LoadCategoriesAsync()
    {
        ViewBag.Categories = new SelectList(
            await _db.TicketCategories.AsNoTracking().Where(category => category.IsActive)
                .OrderBy(category => category.Name).ToListAsync(),
            nameof(TicketCategory.Id), nameof(TicketCategory.Name));
    }

    private async Task<string> CreateReferenceAsync()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var bytes = RandomNumberGenerator.GetBytes(6);
            var suffix = new string(bytes.Select(value => alphabet[value % alphabet.Length]).ToArray());
            var reference = $"DF-{DateTime.UtcNow:yy}-{suffix}";
            if (!await _db.Tickets.AnyAsync(ticket => ticket.Reference == reference)) return reference;
        }
        throw new InvalidOperationException("A unique ticket reference could not be generated.");
    }
}
