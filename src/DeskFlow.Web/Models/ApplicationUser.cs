using Microsoft.AspNetCore.Identity;

namespace DeskFlow.Web.Models;

public class ApplicationUser : IdentityUser
{
    public UserProfile? Profile { get; set; }

    public ICollection<Ticket> RequestedTickets { get; set; } = new List<Ticket>();
    public ICollection<TicketAssignment> TechnicianAssignments { get; set; } = new List<TicketAssignment>();
    public ICollection<TicketAssignment> AssignmentsMade { get; set; } = new List<TicketAssignment>();
    public ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();
    public ICollection<TicketAttachment> UploadedAttachments { get; set; } = new List<TicketAttachment>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
