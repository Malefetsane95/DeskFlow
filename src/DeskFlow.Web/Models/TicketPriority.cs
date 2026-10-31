using System.ComponentModel.DataAnnotations;

namespace DeskFlow.Web.Models;

public class TicketPriority
{
    public int Id { get; set; }

    [Required, StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(40)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(7)]
    public string Color { get; set; } = "#6B7280";

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public SlaRule? SlaRule { get; set; }
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
