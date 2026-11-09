namespace DeskFlow.Web.ViewModels.Staff;

public class StaffTicketRowViewModel
{
    public long Id { get; init; }
    public string Reference { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Requester { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public int PriorityOrder { get; init; }
    public string Status { get; init; } = string.Empty;
    public string StatusCode { get; init; } = string.Empty;
    public string? Technician { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public int? ResponseTargetMinutes { get; init; }
    public int? ResolutionTargetMinutes { get; init; }
    public DateTimeOffset? FirstRespondedAt { get; init; }
    public DateTimeOffset? ResolvedAt { get; init; }
    public SlaProgressViewModel ResponseSla { get; set; } = new();
    public SlaProgressViewModel ResolutionSla { get; set; } = new();
}
