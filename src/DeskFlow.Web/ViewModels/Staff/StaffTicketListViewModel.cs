using Microsoft.AspNetCore.Mvc.Rendering;

namespace DeskFlow.Web.ViewModels.Staff;

public class StaffTicketListViewModel
{
    public string? Search { get; init; }
    public string? StatusCode { get; init; }
    public string? QueueView { get; init; }
    public List<SelectListItem> Statuses { get; init; } = [];
    public List<StaffTicketRowViewModel> Tickets { get; init; } = [];
}
