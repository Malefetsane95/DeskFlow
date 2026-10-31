using System.ComponentModel.DataAnnotations;

namespace DeskFlow.Web.Models;

public class TicketAttachment
{
    public long Id { get; set; }
    public long TicketId { get; set; }
    public string UploadedById { get; set; } = string.Empty;

    [Required, StringLength(400)]
    public string ObjectKey { get; set; } = string.Empty;

    [Required, StringLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Ticket Ticket { get; set; } = null!;
    public ApplicationUser UploadedBy { get; set; } = null!;
}
