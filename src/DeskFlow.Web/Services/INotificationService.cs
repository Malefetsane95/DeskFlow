using DeskFlow.Web.Models;

namespace DeskFlow.Web.Services;

public interface INotificationService
{
    Task NotifyNewTicketAsync(Ticket ticket, string actorId);
    void NotifyAssignment(Ticket ticket, string technicianId, string actorId);
    void NotifyStatusChange(Ticket ticket, string statusName, string actorId);
    Task NotifyPublicReplyAsync(Ticket ticket, string actorId, bool actorIsStaff);
}
