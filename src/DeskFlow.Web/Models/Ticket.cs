using System.ComponentModel.DataAnnotations;

namespace DeskFlow.Web.Models;

public class Ticket
{
    public long Id { get; set; }

    [Required, StringLength(20)]
    public string Reference { get; set; } = string.Empty;

    [Required, StringLength(160)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string RequesterId { get; set; } = string.Empty;

    public int CategoryId { get; set; }
    public int PriorityId { get; set; }
    public int StatusId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? FirstRespondedAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }

    public ApplicationUser Requester { get; set; } = null!;
    public TicketCategory Category { get; set; } = null!;
    public TicketPriority Priority { get; set; } = null!;
    public TicketStatus Status { get; set; } = null!;

    public ICollection<TicketAssignment> Assignments { get; set; } = new List<TicketAssignment>();
    public ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();
    public ICollection<TicketAttachment> Attachments { get; set; } = new List<TicketAttachment>();
    public ICollection<TicketHistory> History { get; set; } = new List<TicketHistory>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public UserFeedback? Feedback { get; set; }
}
