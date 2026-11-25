using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrthoFlowAI.Api.Storage;
using OrthoFlowAI.Application.DTOs;
using OrthoFlowAI.Application.Services;
using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Enums;

namespace OrthoFlowAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly ILogger<DocumentsController> _logger;
    private readonly PrivateFileStore _files;

    public DocumentsController(IDocumentService documentService, ILogger<DocumentsController> logger, PrivateFileStore files)
    {
        _documentService = documentService;
        _logger = logger;
        _files = files;
    }

    /// <summary>
    /// Download a document's file. Requires the caller's JWT, and the tenant filter
    /// guarantees the document belongs to the caller's clinic.
    /// </summary>
    [HttpGet("{id}/file")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(string id)
    {
        var result = await _documentService.GetByIdAsync(id);
        if (!result.Success || result.Data == null)
            return NotFound();

        var path = _files.ResolvePath(result.Data.FileUrl);
        if (path == null || !System.IO.File.Exists(path))
            return NotFound();

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        // Always an attachment so uploaded HTML/SVG can never run in the API origin.
        return PhysicalFile(path, "application/octet-stream", result.Data.FileName);
    }

    /// <summary>
    /// Get all documents with filtering and pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<DocumentResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] DocumentFilterRequest filter)
    {
        _logger.LogInformation("Getting documents - Page: {PageNumber}, Size: {PageSize}", filter.PageNumber, filter.PageSize);
        var result = await _documentService.GetAllAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Get a document by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<DocumentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        _logger.LogInformation("Getting document: {DocumentId}", id);
        var result = await _documentService.GetByIdAsync(id);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Upload a new document
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<DocumentResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> Upload([FromForm] UploadDocumentRequest request)
    {
        if (string.IsNullOrEmpty(request.PatientId))
            return BadRequest(ApiResponse.FailResponse("PatientId is required"));

        if (request.File == null || request.File.Length == 0)
            return BadRequest(ApiResponse.FailResponse("No file provided"));

        var clinicId = HttpContext.Items["ClinicId"]?.ToString() ?? string.Empty;
        var uploadedById = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        // Save file in private storage (not under wwwroot)
        string fileUrl;
        await using (var stream = request.File.OpenReadStream())
        {
            fileUrl = await _files.SaveAsync(stream, "documents", clinicId,
                Path.GetExtension(request.File.FileName), HttpContext.RequestAborted);
        }

        var createRequest = new CreateDocumentRequest
        {
            PatientId = request.PatientId,
            FileName = Path.GetFileName(request.File.FileName),
            Type = request.Type,
            Description = request.Description,
            Tags = request.Tags,
            ExpiryDate = request.ExpiryDate
        };

        var result = await _documentService.CreateAsync(createRequest, uploadedById, fileUrl, request.File.ContentType, request.File.Length);

        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    /// <summary>
    /// Update document metadata
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<DocumentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateDocumentRequest request)
    {
        _logger.LogInformation("Updating document: {DocumentId}", id);
        var result = await _documentService.UpdateAsync(id, request);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Sign a document
    /// </summary>
    [HttpPost("{id}/sign")]
    [ProducesResponseType(typeof(ApiResponse<DocumentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Sign(string id, [FromBody] SignDocumentRequest request)
    {
        _logger.LogInformation("Signing document: {DocumentId}", id);
        var signedById = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        var result = await _documentService.SignAsync(id, request, signedById);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Bulk sign multiple documents
    /// </summary>
    [HttpPost("bulk-sign")]
    [ProducesResponseType(typeof(ApiResponse<List<DocumentResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkSign([FromBody] BulkSignRequest request)
    {
        _logger.LogInformation("Bulk signing {Count} documents", request.DocumentIds.Count);
        var signedById = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        var result = await _documentService.BulkSignAsync(request, signedById);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Download document as PDF
    /// </summary>
    [HttpGet("{id}/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadPdf(string id)
    {
        _logger.LogInformation("Downloading PDF for document: {DocumentId}", id);
        var result = await _documentService.GenerateDocumentPdfAsync(id);

        if (!result.Success)
            return NotFound(result);

        var document = await _documentService.GetByIdAsync(id);
        var fileName = document.Success ? $"{document.Data!.FileName}.pdf" : "document.pdf";

        return File(result.Data!, "application/pdf", fileName);
    }

    /// <summary>
    /// Get all documents for a patient
    /// </summary>
    [HttpGet("patient/{patientId}")]
    [ProducesResponseType(typeof(ApiResponse<List<DocumentResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPatientDocuments(string patientId)
    {
        _logger.LogInformation("Getting documents for patient: {PatientId}", patientId);
        var result = await _documentService.GetPatientDocumentsAsync(patientId);
        return Ok(result);
    }

    /// <summary>
    /// Delete a document
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id)
    {
        _logger.LogInformation("Deleting document: {DocumentId}", id);
        var result = await _documentService.DeleteAsync(id);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }
}

public class UploadDocumentRequest
{
    public string PatientId { get; set; } = string.Empty;
    public DocumentType Type { get; set; }
    public string? Description { get; set; }
    public string? Tags { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public IFormFile File { get; set; } = null!;
}