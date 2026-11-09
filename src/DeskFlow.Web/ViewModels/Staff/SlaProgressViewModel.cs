namespace DeskFlow.Web.ViewModels.Staff;

public sealed class SlaProgressViewModel
{
    public string Name { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string StateClass { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public DateTimeOffset? DueAt { get; init; }
}
