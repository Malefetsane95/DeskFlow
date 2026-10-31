namespace DeskFlow.Web.Models;

public class TicketAssignment
{
    public long Id { get; set; }
    public long TicketId { get; set; }
    public string TechnicianId { get; set; } = string.Empty;
    public string AssignedById { get; set; } = string.Empty;
    public DateTimeOffset AssignedAt { get; set; }
    public DateTimeOffset? UnassignedAt { get; set; }

    public Ticket Ticket { get; set; } = null!;
    public ApplicationUser Technician { get; set; } = null!;
    public ApplicationUser AssignedBy { get; set; } = null!;
}
