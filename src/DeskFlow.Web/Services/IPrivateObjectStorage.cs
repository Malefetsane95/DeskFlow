namespace DeskFlow.Web.Services;

public interface IPrivateObjectStorage
{
    Task UploadAsync(string objectKey, Stream content, long sizeBytes, string contentType, CancellationToken cancellationToken);
    Task<Uri> CreateDownloadUrlAsync(string objectKey, string downloadFileName, CancellationToken cancellationToken);
    Task DeleteAsync(string objectKey, CancellationToken cancellationToken);
}
