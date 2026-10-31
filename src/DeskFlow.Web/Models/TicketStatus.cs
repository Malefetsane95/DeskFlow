using System.ComponentModel.DataAnnotations;

namespace DeskFlow.Web.Models;

public class TicketStatus
{
    public int Id { get; set; }

    [Required, StringLength(30)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(40)]
    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }
    public bool IsFinal { get; set; }

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
