using Microsoft.AspNetCore.Mvc.Rendering;

namespace DeskFlow.Web.ViewModels.Staff;

public class StaffTicketDetailViewModel
{
    public long Id { get; init; }
    public string Reference { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Requester { get; init; } = string.Empty;
    public string? RequesterEmail { get; init; }
    public string? Department { get; init; }
    public string Category { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int StatusId { get; init; }
    public string StatusCode { get; init; } = string.Empty;
    public string? TechnicianId { get; init; }
    public string? Technician { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public List<SelectListItem> Statuses { get; init; } = [];
    public List<SelectListItem> Technicians { get; init; } = [];
    public List<StaffTicketHistoryViewModel> History { get; init; } = [];
}

public class StaffTicketHistoryViewModel
{
    public string Actor { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public string? PreviousValue { get; init; }
    public string? NewValue { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
}
