namespace DeskFlow.Web.ViewModels.Staff;

public sealed class StaffOverviewViewModel
{
    public int OpenCount { get; init; }
    public int MineOpenCount { get; init; }
    public int UnassignedCount { get; init; }
    public int OverdueSlaCount { get; init; }
}
