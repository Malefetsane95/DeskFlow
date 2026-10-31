using System.ComponentModel.DataAnnotations;

namespace DeskFlow.Web.Models;

public class TicketHistory
{
    public long Id { get; set; }
    public long TicketId { get; set; }
    public string ActorId { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string EventType { get; set; } = string.Empty;

    [StringLength(500)]
    public string? PreviousValue { get; set; }

    [StringLength(500)]
    public string? NewValue { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public Ticket Ticket { get; set; } = null!;
    public ApplicationUser Actor { get; set; } = null!;
}
