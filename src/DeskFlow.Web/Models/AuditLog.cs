using System.ComponentModel.DataAnnotations;

namespace DeskFlow.Web.Models;

public class AuditLog
{
    public long Id { get; set; }
    public string ActorId { get; set; } = string.Empty;
    public long? TicketId { get; set; }

    [Required, StringLength(80)]
    public string ActionType { get; set; } = string.Empty;

    public string? PreviousValueJson { get; set; }
    public string? NewValueJson { get; set; }
    public DateTimeOffset OccurredAt { get; set; }

    public ApplicationUser Actor { get; set; } = null!;
    public Ticket? Ticket { get; set; }
}
