using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using DeskFlow.Web.Data;
using DeskFlow.Web.Models;
using DeskFlow.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DeskFlow.Web.Controllers;

[Authorize]
[Route("tickets/{ticketId:long}/attachments")]
public class TicketAttachmentsController : Controller
{
    private const long MaxRequestBytes = AttachmentPolicy.MaxFileSizeBytes + 64 * 1024;
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IPrivateObjectStorage _storage;
    private readonly ILogger<TicketAttachmentsController> _logger;

    public TicketAttachmentsController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> users,
        IPrivateObjectStorage storage,
        ILogger<TicketAttachmentsController> logger)
    {
        _db = db;
        _users = users;
        _storage = storage;
        _logger = logger;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
    public async Task<IActionResult> Upload(long ticketId, IFormFile? file)
    {
        var userId = _users.GetUserId(User);
        if (userId is null) return Challenge();

        var ticket = await _db.Tickets.AsNoTracking()
            .Where(item => item.Id == ticketId)
            .Select(item => new { item.Id, item.Reference, item.RequesterId })
            .SingleOrDefaultAsync();
        if (ticket is null || !CanAccessTicket(ticket.RequesterId, userId)) return NotFound();

        if (file is null || file.Length <= 0)
        {
            SetError("Choose a file before uploading it.");
            return BackToTicket(ticketId);
        }
        if (file.Length > AttachmentPolicy.MaxFileSizeBytes)
        {
            SetError("Each attachment must be 10 MB or smaller.");
            return BackToTicket(ticketId);
        }

        var extension = Path.GetExtension(file.FileName);
        if (!AttachmentPolicy.AllowedTypes.TryGetValue(extension, out var expectedContentType))
        {
            SetError("Use a PNG, JPG, PDF, TXT, or LOG file.");
            return BackToTicket(ticketId);
        }

        var submittedContentType = file.ContentType.Split(';', 2)[0].Trim();
        if (!string.Equals(submittedContentType, expectedContentType, StringComparison.OrdinalIgnoreCase))
        {
            SetError("The file type doesn’t match its extension. Choose the original file and try again.");
            return BackToTicket(ticketId);
        }

        var prefix = new byte[(int)Math.Min(file.Length, 8)];
        await using (var signatureStream = file.OpenReadStream())
        {
            await signatureStream.ReadExactlyAsync(prefix, HttpContext.RequestAborted);
        }
        if (!AttachmentPolicy.HasValidSignature(prefix, extension))
        {
            SetError("The file contents don’t match the selected file type.");
            return BackToTicket(ticketId);
        }

        var existingCount = await _db.TicketAttachments.CountAsync(item => item.TicketId == ticketId);
        if (existingCount >= AttachmentPolicy.MaxPerTicket)
        {
            SetError("A request can have up to five attachments.");
            return BackToTicket(ticketId);
        }

        var originalFileName = CleanFileName(file.FileName);
        if (string.IsNullOrWhiteSpace(originalFileName) || originalFileName.Length > 255)
        {
            SetError("The file name must be 255 characters or fewer.");
            return BackToTicket(ticketId);
        }

        var objectKey = $"tickets/{ticketId}/{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var objectUploaded = false;
        try
        {
            await using (var stream = file.OpenReadStream())
            {
                await _storage.UploadAsync(
                    objectKey, stream, file.Length, expectedContentType, HttpContext.RequestAborted);
            }
            objectUploaded = true;

            await using var transaction = await _db.Database.BeginTransactionAsync(HttpContext.RequestAborted);
            if (!await LockTicketAsync(ticketId, HttpContext.RequestAborted))
            {
                await transaction.RollbackAsync(HttpContext.RequestAborted);
                await TryDeleteObjectAsync(objectKey);
                return NotFound();
            }

            var finalCount = await _db.TicketAttachments.CountAsync(item => item.TicketId == ticketId);
            if (finalCount >= AttachmentPolicy.MaxPerTicket)
            {
                await transaction.RollbackAsync(HttpContext.RequestAborted);
                await TryDeleteObjectAsync(objectKey);
                SetError("A request can have up to five attachments.");
                return BackToTicket(ticketId);
            }

            _db.TicketAttachments.Add(new TicketAttachment
            {
                TicketId = ticketId,
                UploadedById = userId,
                ObjectKey = objectKey,
                OriginalFileName = originalFileName,
                ContentType = expectedContentType,
                SizeBytes = file.Length,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await _db.SaveChangesAsync(HttpContext.RequestAborted);
            await transaction.CommitAsync(HttpContext.RequestAborted);

            objectUploaded = false;
            SetNotice($"{originalFileName} was attached to the request.");
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or DbUpdateException)
        {
            if (objectUploaded) await TryDeleteObjectAsync(objectKey);
            _logger.LogWarning(exception, "Could not save an attachment for ticket {TicketReference}.", ticket.Reference);
            SetError(exception is InvalidOperationException
                ? "File storage isn’t configured yet. Ask the administrator to finish setup."
                : "The file couldn’t be attached. Check the storage setup and try again.");
        }

        return BackToTicket(ticketId);
    }

    [HttpGet("{attachmentId:long}")]
    public async Task<IActionResult> Download(long ticketId, long attachmentId)
    {
        var userId = _users.GetUserId(User);
        if (userId is null) return Challenge();

        var attachment = await _db.TicketAttachments.AsNoTracking()
            .Where(item => item.Id == attachmentId && item.TicketId == ticketId)
            .Select(item => new
            {
                item.ObjectKey,
                item.OriginalFileName,
                item.Ticket.RequesterId
            })
            .SingleOrDefaultAsync();
        if (attachment is null || !CanAccessTicket(attachment.RequesterId, userId)) return NotFound();

        try
        {
            var signedUrl = await _storage.CreateDownloadUrlAsync(
                attachment.ObjectKey, attachment.OriginalFileName, HttpContext.RequestAborted);
            Response.Headers.CacheControl = "no-store";
            Response.Headers["Referrer-Policy"] = "no-referrer";
            return Redirect(signedUrl.ToString());
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException)
        {
            _logger.LogWarning(exception, "Could not create a download link for ticket attachment {AttachmentId}.", attachmentId);
            SetError(exception is InvalidOperationException
                ? "File storage isn’t configured yet. Ask the administrator to finish setup."
                : "The attachment couldn’t be downloaded. Try again in a moment.");
            return BackToTicket(ticketId);
        }
    }

    private bool CanAccessTicket(string requesterId, string userId) =>
        requesterId == userId
        || User.IsInRole(ApplicationRoles.Technician)
        || User.IsInRole(ApplicationRoles.HelpdeskManager);

    private async Task<bool> LockTicketAsync(long ticketId, CancellationToken cancellationToken)
    {
        var connection = _db.Database.GetDbConnection();
        var currentTransaction = _db.Database.CurrentTransaction?.GetDbTransaction();
        if (currentTransaction is null) throw new InvalidOperationException("A database transaction is required.");

        await using DbCommand command = connection.CreateCommand();
        command.Transaction = currentTransaction;
        command.CommandText = "SELECT 1 FROM \"Tickets\" WHERE \"Id\" = @ticketId FOR UPDATE";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@ticketId";
        parameter.Value = ticketId;
        command.Parameters.Add(parameter);
        return await command.ExecuteScalarAsync(cancellationToken) is not null;
    }

    private async Task TryDeleteObjectAsync(string objectKey)
    {
        try
        {
            await _storage.DeleteAsync(objectKey, CancellationToken.None);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException)
        {
            _logger.LogError(exception, "Could not clean up the storage object {ObjectKey} after a failed attachment upload.", objectKey);
        }
    }

    private IActionResult BackToTicket(long ticketId) =>
        User.IsInRole(ApplicationRoles.Technician) || User.IsInRole(ApplicationRoles.HelpdeskManager)
            ? RedirectToAction("Details", "StaffTickets", new { id = ticketId })
            : RedirectToAction("Details", "Tickets", new { id = ticketId });

    private void SetError(string message)
    {
        if (User.IsInRole(ApplicationRoles.Technician) || User.IsInRole(ApplicationRoles.HelpdeskManager))
            TempData["StaffError"] = message;
        else
            TempData["AttachmentError"] = message;
    }

    private void SetNotice(string message)
    {
        if (User.IsInRole(ApplicationRoles.Technician) || User.IsInRole(ApplicationRoles.HelpdeskManager))
            TempData["StaffNotice"] = message;
        else
            TempData["AttachmentNotice"] = message;
    }

    private static string CleanFileName(string fileName)
    {
        var leafName = fileName.Replace('\\', '/').Split('/').LastOrDefault() ?? string.Empty;
        var cleanName = new string(leafName.Where(character => !char.IsControl(character)).ToArray()).Trim();
        return cleanName;
    }
}
