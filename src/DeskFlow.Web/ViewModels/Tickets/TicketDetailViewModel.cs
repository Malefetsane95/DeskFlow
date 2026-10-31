namespace DeskFlow.Web.ViewModels.Tickets;

public class TicketDetailViewModel
{
    public long Id { get; init; }
    public string Reference { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public List<TicketTimelineItemViewModel> History { get; init; } = [];
    public List<TicketConversationItemViewModel> Conversation { get; init; } = [];
}

public class TicketTimelineItemViewModel
{
    public string EventType { get; init; } = string.Empty;
    public string? NewValue { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
}

public class TicketConversationItemViewModel
{
    public string Author { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
}
