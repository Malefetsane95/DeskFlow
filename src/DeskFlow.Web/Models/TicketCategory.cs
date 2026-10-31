using System.ComponentModel.DataAnnotations;

namespace DeskFlow.Web.Models;

public class TicketCategory
{
    public int Id { get; set; }

    [Required, StringLength(80)]
    public string Name { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
