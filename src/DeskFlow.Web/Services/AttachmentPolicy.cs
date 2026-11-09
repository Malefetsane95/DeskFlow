namespace DeskFlow.Web.Services;

public static class AttachmentPolicy
{
    public const long MaxFileSizeBytes = 10 * 1024 * 1024;
    public const int MaxPerTicket = 5;

    public static readonly IReadOnlyDictionary<string, string> AllowedTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".pdf"] = "application/pdf",
            [".txt"] = "text/plain",
            [".log"] = "text/plain"
        };

    public static bool HasValidSignature(ReadOnlySpan<byte> prefix, string extension)
    {
        if (extension.Equals(".png", StringComparison.OrdinalIgnoreCase))
        {
            return prefix.Length >= 8
                && prefix[0] == 0x89 && prefix[1] == 0x50 && prefix[2] == 0x4E && prefix[3] == 0x47
                && prefix[4] == 0x0D && prefix[5] == 0x0A && prefix[6] == 0x1A && prefix[7] == 0x0A;
        }

        if (extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
        {
            return prefix.Length >= 3 && prefix[0] == 0xFF && prefix[1] == 0xD8 && prefix[2] == 0xFF;
        }

        if (extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return prefix.Length >= 5 && prefix[..5].SequenceEqual("%PDF-"u8);
        }

        return !prefix.Contains((byte)0);
    }
}
