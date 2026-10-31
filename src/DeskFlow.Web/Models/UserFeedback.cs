using System.ComponentModel.DataAnnotations;

namespace DeskFlow.Web.Models;

public class UserFeedback
{
    public long Id { get; set; }
    public long TicketId { get; set; }
    public string UserId { get; set; } = string.Empty;

    [Range(1, 5)]
    public int Rating { get; set; }

    [StringLength(2000)]
    public string? Comment { get; set; }

    public DateTimeOffset SubmittedAt { get; set; }

    public Ticket Ticket { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}
