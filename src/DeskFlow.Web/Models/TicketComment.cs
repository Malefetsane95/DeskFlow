using System.ComponentModel.DataAnnotations;

namespace DeskFlow.Web.Models;

public class TicketComment
{
    public long Id { get; set; }
    public long TicketId { get; set; }
    public string AuthorId { get; set; } = string.Empty;
    public CommentVisibility Visibility { get; set; }

    [Required, StringLength(4000)]
    public string Body { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public Ticket Ticket { get; set; } = null!;
    public ApplicationUser Author { get; set; } = null!;
}
