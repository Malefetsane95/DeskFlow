namespace DeskFlow.Web.Models;

public static class ApplicationRoles
{
    public const string Administrator = "Administrator";
    public const string HelpdeskManager = "Helpdesk Manager";
    public const string Technician = "IT Technician";
    public const string EndUser = "End User";

    public static IReadOnlyList<string> All { get; } =
    [
        Administrator,
        HelpdeskManager,
        Technician,
        EndUser
    ];
}
