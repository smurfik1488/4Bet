using _4Bet.Application.IServices;
using _4Bet.Infrastructure.IRepositories;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Linq;

namespace _4BetWebApi.Controllers;

[ApiController]
[Route("api/admin/verifications")]
[Authorize(Roles = "Admin")]
public class AdminVerificationController : ControllerBase
{
    private readonly IAdminVerificationService _adminVerificationService;
    private readonly IVerificationRepository _verificationRepository;
    private readonly string? _storageConnectionString;

    public AdminVerificationController(
        IAdminVerificationService adminVerificationService,
        IVerificationRepository verificationRepository,
        IConfiguration configuration)
    {
        _adminVerificationService = adminVerificationService;
        _verificationRepository = verificationRepository;
        _storageConnectionString = configuration["Azure:Storage:ConnectionString"];
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingRequests()
    {
        var requests = await _adminVerificationService.GetPendingRequestsAsync();
        var response = requests.Select(r => new
        {
            r.Id,
            r.UserId,
            r.DocumentUrl,
            r.Status,
            r.CreatedAt,
            UserEmail = r.User?.Email,
            UserFirstName = r.User?.FirstName,
            UserLastName = r.User?.LastName
        });
        return Ok(response);
    }

    [HttpGet("{id:guid}/document")]
    public async Task<IActionResult> GetDocument(Guid id)
    {
        var request = await _verificationRepository.GetByIdAsync(id);
        if (request == null || string.IsNullOrWhiteSpace(request.DocumentUrl))
        {
            return NotFound(new { message = "Document not found." });
        }

        if (string.IsNullOrWhiteSpace(_storageConnectionString))
        {
            return StatusCode(500, new { message = "Storage connection is not configured." });
        }

        try
        {
            var sourceUri = new Uri(request.DocumentUrl);
            var sourceBuilder = new BlobUriBuilder(sourceUri);
            var blobClient = new BlobServiceClient(_storageConnectionString)
                .GetBlobContainerClient(sourceBuilder.BlobContainerName)
                .GetBlobClient(sourceBuilder.BlobName);

            var download = await blobClient.DownloadStreamingAsync();
            var stream = download.Value.Content;
            var contentType = download.Value.Details.ContentType;
            if (string.IsNullOrWhiteSpace(contentType))
            {
                contentType = GuessContentTypeFromName(sourceBuilder.BlobName);
            }

            return File(stream, contentType, enableRangeProcessing: false);
        }
        catch
        {
            return StatusCode(500, new { message = "Could not load document from storage." });
        }
    }

    private static string GuessContentTypeFromName(string fileName)
    {
        var lower = fileName.ToLowerInvariant();
        if (lower.EndsWith(".png")) return "image/png";
        if (lower.EndsWith(".jpg") || lower.EndsWith(".jpeg")) return "image/jpeg";
        if (lower.EndsWith(".webp")) return "image/webp";
        if (lower.EndsWith(".gif")) return "image/gif";
        if (lower.EndsWith(".pdf")) return "application/pdf";
        return "application/octet-stream";
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> ApproveRequest(Guid id)
    {
        var result = await _adminVerificationService.ApproveRequestAsync(id);

        return result switch
        {
            "SUCCESS" => Ok(new { message = "User verification approved successfully." }),
            "NOT_FOUND" => NotFound(new { message = "Verification request not found." }),
            "ALREADY_PROCESSED" => BadRequest(new { message = "This request has already been processed." }),
            _ => StatusCode(500, new { message = "Internal server error." })
        };
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> RejectRequest(Guid id)
    {
        var result = await _adminVerificationService.RejectRequestAsync(id);

        return result switch
        {
            "SUCCESS" => Ok(new { message = "User verification rejected." }),
            "NOT_FOUND" => NotFound(new { message = "Verification request not found." }),
            "ALREADY_PROCESSED" => BadRequest(new { message = "This request has already been processed." }),
            _ => StatusCode(500, new { message = "Internal server error." })
        };
    }
}