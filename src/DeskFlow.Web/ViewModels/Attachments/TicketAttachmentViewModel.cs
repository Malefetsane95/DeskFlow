namespace DeskFlow.Web.ViewModels.Attachments;

public class TicketAttachmentViewModel
{
    public long Id { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public string UploadedBy { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
}
