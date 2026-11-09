using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using DeskFlow.Web.Data;
using DeskFlow.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeskFlow.Web.Api;

[ApiController]
[ApiExplorerSettings(GroupName = "requester")]
[Route("api/requester/tickets")]
[Authorize(Roles = ApplicationRoles.EndUser)]
public sealed class RequesterTicketsController : ControllerBase
{
    private const int MaximumPageSize = 100;
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public RequesterTicketsController(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    [HttpGet]
    [EndpointSummary("List your tickets")]
    [EndpointDescription("Returns the signed-in End User's own tickets with bounded pagination and optional status and search filters.")]
    [ProducesResponseType(typeof(PagedResponse<RequesterTicketListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<RequesterTicketListItemDto>>> GetTickets(
        [FromQuery] RequesterTicketQuery query,
        CancellationToken cancellationToken)
    {
        var requesterId = _users.GetUserId(User);
        if (requesterId is null) return Unauthorized();

        var tickets = _db.Tickets.AsNoTracking()
            .Where(ticket => ticket.RequesterId == requesterId);

        var statusCode = query.StatusCode?.Trim().ToUpperInvariant();
        if (!string.IsNullOrEmpty(statusCode))
        {
            tickets = tickets.Where(ticket => ticket.Status.Code == statusCode);
        }

        var search = query.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            tickets = tickets.Where(ticket => ticket.Reference.Contains(search)
                || ticket.Title.Contains(search)
                || ticket.Category.Name.Contains(search));
        }

        var totalCount = await tickets.CountAsync(cancellationToken);
        var items = await tickets
            .OrderByDescending(ticket => ticket.CreatedAt)
            .ThenByDescending(ticket => ticket.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(ticket => new RequesterTicketListItemDto
            {
                Reference = ticket.Reference,
                Title = ticket.Title,
                Category = ticket.Category.Name,
                Priority = ticket.Priority.Name,
                Status = ticket.Status.Name,
                CreatedAt = ticket.CreatedAt,
                UpdatedAt = ticket.History.OrderByDescending(history => history.OccurredAt)
                    .Select(history => (DateTimeOffset?)history.OccurredAt).FirstOrDefault() ?? ticket.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(new PagedResponse<RequesterTicketListItemDto>
        {
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
            Items = items
        });
    }

    [HttpGet("{id:long}")]
    [EndpointSummary("Get a ticket and its public replies")]
    [EndpointDescription("Returns a ticket only when it belongs to the signed-in End User. Internal notes and staff-only fields are excluded.")]
    [ProducesResponseType(typeof(RequesterTicketDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RequesterTicketDetailDto>> GetTicket(
        long id,
        CancellationToken cancellationToken)
    {
        var requesterId = _users.GetUserId(User);
        if (requesterId is null) return Unauthorized();

        var ticket = await _db.Tickets.AsNoTracking()
            .Where(item => item.Id == id && item.RequesterId == requesterId)
            .Select(item => new RequesterTicketDetailDto
            {
                Reference = item.Reference,
                Title = item.Title,
                Category = item.Category.Name,
                Priority = item.Priority.Name,
                Status = item.Status.Name,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.History.OrderByDescending(history => history.OccurredAt)
                    .Select(history => (DateTimeOffset?)history.OccurredAt).FirstOrDefault() ?? item.CreatedAt,
                PublicReplies = item.Comments
                    .Where(comment => comment.Visibility == CommentVisibility.Public)
                    .OrderBy(comment => comment.CreatedAt)
                    .ThenBy(comment => comment.Id)
                    .Select(comment => new RequesterTicketReplyDto
                    {
                        Author = comment.Author.Profile!.DisplayName,
                        Body = comment.Body,
                        CreatedAt = comment.CreatedAt
                    })
                    .ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

        return ticket is null ? NotFound() : Ok(ticket);
    }
}

public sealed class RequesterTicketQuery
{
    [Description("One-based page number. Values from 1 to 1,000,000 are accepted.")]
    [Range(1, 1_000_000)]
    public int Page { get; set; } = 1;

    [Description("Number of tickets per page. Values from 1 to 100 are accepted.")]
    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    [Description("Optional ticket status code, up to 30 characters.")]
    [StringLength(30)]
    public string? StatusCode { get; set; }

    [Description("Optional search across ticket reference, title, and category, up to 100 characters.")]
    [StringLength(100)]
    public string? Search { get; set; }
}

public sealed class PagedResponse<T>
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public IReadOnlyList<T> Items { get; init; } = [];
}

public class RequesterTicketListItemDto
{
    public string Reference { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

public sealed class RequesterTicketDetailDto : RequesterTicketListItemDto
{
    public IReadOnlyList<RequesterTicketReplyDto> PublicReplies { get; init; } = [];
}

public sealed class RequesterTicketReplyDto
{
    public string Author { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
}
