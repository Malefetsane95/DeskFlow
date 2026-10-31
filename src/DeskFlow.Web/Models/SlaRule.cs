namespace DeskFlow.Web.Models;

public class SlaRule
{
    public int Id { get; set; }
    public int PriorityId { get; set; }
    public int ResponseTargetMinutes { get; set; }
    public int ResolutionTargetMinutes { get; set; }
    public bool IsActive { get; set; } = true;

    public TicketPriority Priority { get; set; } = null!;
}
