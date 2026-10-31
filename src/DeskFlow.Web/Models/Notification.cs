using System.ComponentModel.DataAnnotations;

namespace DeskFlow.Web.Models;

public class Notification
{
    public long Id { get; set; }
    public string RecipientId { get; set; } = string.Empty;
    public long? TicketId { get; set; }

    [Required, StringLength(50)]
    public string Type { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string Message { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ReadAt { get; set; }

    public ApplicationUser Recipient { get; set; } = null!;
    public Ticket? Ticket { get; set; }
}
