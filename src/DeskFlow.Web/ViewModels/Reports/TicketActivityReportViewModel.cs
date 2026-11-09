namespace DeskFlow.Web.ViewModels.Reports;

public sealed class TicketActivityReportViewModel
{
    public int RangeDays { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public int OpenedCount { get; init; }
    public int ResolvedCount { get; init; }
    public int OpenCount { get; init; }
    public int OverdueSlaCount { get; init; }
    public string BucketName { get; init; } = string.Empty;
    public IReadOnlyList<TicketActivityPeriodViewModel> Periods { get; init; } = [];
}

public sealed class TicketActivityPeriodViewModel
{
    public string Label { get; init; } = string.Empty;
    public int OpenedCount { get; set; }
    public int ResolvedCount { get; set; }
}
