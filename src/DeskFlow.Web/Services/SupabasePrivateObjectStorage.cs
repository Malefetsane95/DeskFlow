using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace DeskFlow.Web.Services;

public class SupabasePrivateObjectStorage : IPrivateObjectStorage
{
    private const int SignedUrlLifetimeSeconds = 300;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public SupabasePrivateObjectStorage(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task UploadAsync(
        string objectKey,
        Stream content,
        long sizeBytes,
        string contentType,
        CancellationToken cancellationToken)
    {
        var settings = GetSettings();
        using var request = CreateRequest(
            HttpMethod.Post,
            BuildObjectUrl(settings, objectKey),
            settings.SecretKey);
        request.Headers.TryAddWithoutValidation("x-upsert", "false");
        request.Content = new StreamContent(content);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        request.Content.Headers.ContentLength = sizeBytes;

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Supabase Storage rejected the upload ({(int)response.StatusCode}).");
        }
    }

    public async Task<Uri> CreateDownloadUrlAsync(
        string objectKey,
        string downloadFileName,
        CancellationToken cancellationToken)
    {
        var settings = GetSettings();
        using var request = CreateRequest(
            HttpMethod.Post,
            BuildObjectUrl(settings, objectKey, signed: true),
            settings.SecretKey);
        request.Content = JsonContent.Create(new { expiresIn = SignedUrlLifetimeSeconds });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Supabase Storage could not create a download link ({(int)response.StatusCode}).");
        }

        var result = await response.Content.ReadFromJsonAsync<SignedUrlResponse>(cancellationToken);
        if (string.IsNullOrWhiteSpace(result?.SignedUrl))
        {
            throw new HttpRequestException("Supabase Storage returned an empty download link.");
        }

        var url = Uri.TryCreate(result.SignedUrl, UriKind.Absolute, out var absolute)
            ? absolute
            : new Uri(settings.ProjectUrl, result.SignedUrl.TrimStart('/').StartsWith("storage/v1/", StringComparison.Ordinal)
                ? result.SignedUrl.TrimStart('/')
                : "storage/v1/" + result.SignedUrl.TrimStart('/'));

        var builder = new UriBuilder(url);
        var downloadParameter = "download=" + Uri.EscapeDataString(downloadFileName);
        builder.Query = string.IsNullOrEmpty(builder.Query)
            ? downloadParameter
            : builder.Query.TrimStart('?') + "&" + downloadParameter;
        return builder.Uri;
    }

    public async Task DeleteAsync(string objectKey, CancellationToken cancellationToken)
    {
        var settings = GetSettings();
        var url = new Uri(settings.ProjectUrl, $"storage/v1/object/{Uri.EscapeDataString(settings.Bucket)}");
        using var request = CreateRequest(HttpMethod.Delete, url, settings.SecretKey);
        request.Content = JsonContent.Create(new { prefixes = new[] { objectKey } });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Supabase Storage could not remove the uploaded file ({(int)response.StatusCode}).");
        }
    }

    private StorageSettings GetSettings()
    {
        var rawUrl = _configuration["Supabase:Url"];
        var secretKey = _configuration["Supabase:SecretKey"]
            ?? _configuration["Supabase:ServiceRoleKey"];
        var bucket = _configuration["Supabase:StorageBucket"];

        if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var projectUrl)
            || projectUrl.Scheme != Uri.UriSchemeHttps
            || string.IsNullOrWhiteSpace(secretKey)
            || string.IsNullOrWhiteSpace(bucket))
        {
            throw new InvalidOperationException(
                "Supabase Storage is not configured. Set Supabase:SecretKey in User Secrets and create the private ticket-attachments bucket.");
        }

        return new StorageSettings(new Uri(projectUrl.ToString().TrimEnd('/') + "/"), secretKey, bucket);
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, Uri url, string secretKey)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.TryAddWithoutValidation("apikey", secretKey);
        return request;
    }

    private static Uri BuildObjectUrl(StorageSettings settings, string objectKey, bool signed = false)
    {
        var prefix = signed ? "storage/v1/object/sign/" : "storage/v1/object/";
        var path = string.Join("/", objectKey.Split('/').Select(Uri.EscapeDataString));
        return new Uri(settings.ProjectUrl, prefix + Uri.EscapeDataString(settings.Bucket) + "/" + path);
    }

    private sealed record StorageSettings(Uri ProjectUrl, string SecretKey, string Bucket);

    private sealed class SignedUrlResponse
    {
        [JsonPropertyName("signedURL")]
        public string SignedUrl { get; init; } = string.Empty;
    }
}
