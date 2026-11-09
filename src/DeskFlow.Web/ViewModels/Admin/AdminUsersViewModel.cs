namespace DeskFlow.Web.ViewModels.Admin;

public sealed class AdminUsersViewModel
{
    public string? Search { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalUsers { get; init; }
    public int TotalPages { get; init; }
    public IReadOnlyList<AdminUserRowViewModel> Users { get; init; } = [];
}

public sealed class AdminUserRowViewModel
{
    public string Id { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = [];

    public bool IsAdministrator => Roles.Contains(DeskFlow.Web.Models.ApplicationRoles.Administrator);
    public bool IsTechnician => Roles.Contains(DeskFlow.Web.Models.ApplicationRoles.Technician);
    public bool IsHelpdeskManager => Roles.Contains(DeskFlow.Web.Models.ApplicationRoles.HelpdeskManager);
}
