namespace DeskFlow.Web.ViewModels.Notifications;

public class NotificationListItemViewModel
{
    public long Id { get; init; }
    public string Message { get; init; } = string.Empty;
    public string TicketReference { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public bool IsRead { get; init; }
}
