using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrthoFlowAI.Application.DTOs;
using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Entities;
using OrthoFlowAI.Domain.Enums;
using OrthoFlowAI.Domain.Interfaces;

namespace OrthoFlowAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PhotosController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PhotosController> _logger;
    private readonly IWebHostEnvironment _env;

    public PhotosController(IUnitOfWork unitOfWork, ILogger<PhotosController> logger, IWebHostEnvironment env)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _env = env;
    }

    /// <summary>
    /// Get photos for a patient with optional filter
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<PhotoResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PhotoFilterRequest filter)
    {
        _logger.LogInformation("Getting photos for patient {PatientId}", filter.PatientId);

        var query = _unitOfWork.Photos.Query()
            .Where(p => !p.IsDeleted);

        if (!string.IsNullOrEmpty(filter.PatientId))
        {
            query = query.Where(p => p.PatientId == filter.PatientId);
        }
        if (filter.Category.HasValue)
        {
            query = query.Where(p => p.Category == filter.Category.Value);
        }
        if (filter.VisitDateFrom.HasValue)
        {
            query = query.Where(p => p.VisitDate >= filter.VisitDateFrom.Value);
        }
        if (filter.VisitDateTo.HasValue)
        {
            query = query.Where(p => p.VisitDate <= filter.VisitDateTo.Value);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        var result = new PagedResult<PhotoResponse>
        {
            Items = items.Select(ToResponse).ToList(),
            TotalCount = total,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };

        return Ok(ApiResponse<PagedResult<PhotoResponse>>.SuccessResponse(result, "Photos retrieved"));
    }

    /// <summary>
    /// Get a single photo by id
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PhotoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        var photo = await _unitOfWork.Photos.GetByIdAsync(id);
        if (photo == null || photo.IsDeleted)
        {
            return NotFound(ApiResponse.FailResponse("Photo not found"));
        }
        return Ok(ApiResponse<PhotoResponse>.SuccessResponse(ToResponse(photo), "Photo retrieved"));
    }

    /// <summary>
    /// Serve the actual image file for a photo
    /// </summary>
    [HttpGet("{id}/file")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFile(string id)
    {
        var photo = await _unitOfWork.Photos.GetByIdAsync(id);
        if (photo == null || photo.IsDeleted)
        {
            return NotFound();
        }

        if (string.IsNullOrEmpty(photo.Url))
        {
            return NotFound();
        }

        var root = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var filePath = Path.Combine(root, photo.Url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound();
        }

        var stream = System.IO.File.OpenRead(filePath);
        var contentType = photo.FileType ?? "application/octet-stream";
        return File(stream, contentType);
    }

    /// <summary>
    /// Upload a new patient photo (multipart/form-data)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PhotoResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> Upload([FromForm] UploadPhotoRequest form)
    {
        if (string.IsNullOrEmpty(form.PatientId))
        {
            return BadRequest(ApiResponse.FailResponse("PatientId is required"));
        }

        var patient = await _unitOfWork.Patients.GetByIdAsync(form.PatientId);
        if (patient == null || patient.IsDeleted)
        {
            return BadRequest(ApiResponse.FailResponse("Patient not found"));
        }

        IFormFile? file = null;
        if (Request.HasFormContentType && Request.Form.Files.Count > 0)
        {
            file = Request.Form.Files[0];
        }

        string url = string.Empty;
        string fileName = string.Empty;
        var fileType = form.FileType;

        if (file != null && file.Length > 0)
        {
            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(extension))
            {
                extension = guessExtension(fileType);
            }

            var safeName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var clinicId = HttpContext.Items["ClinicId"]?.ToString() ?? "default";
            var relativeFolder = $"uploads/photos/{clinicId}";

            var root = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var folder = Path.Combine(root, relativeFolder);
            Directory.CreateDirectory(folder);

            var filePath = Path.Combine(folder, safeName);
            await using (var stream = System.IO.File.Create(filePath))
            {
                await file.CopyToAsync(stream);
            }

            url = $"/{relativeFolder}/{safeName}";
            fileName = file.FileName;
            fileType = file.ContentType;
        }
        else if (string.IsNullOrEmpty(form.BeforePhotoUrl) && string.IsNullOrEmpty(form.AfterPhotoUrl))
        {
            return BadRequest(ApiResponse.FailResponse("No file provided. Attach an image or provide an existing URL."));
        }

        var clinicIdForEntity = HttpContext.Items["ClinicId"]?.ToString() ?? string.Empty;

        var photo = new Photo
        {
            PatientId = form.PatientId,
            DoctorId = form.DoctorId,
            Url = url,
            FileName = string.IsNullOrEmpty(fileName) ? (form.FileName ?? $"{form.Category}.jpg") : fileName,
            FileType = fileType,
            FileSize = file?.Length ?? 0,
            Category = form.Category,
            Description = form.Description,
            VisitDate = form.VisitDate ?? DateTime.UtcNow,
            BeforePhotoUrl = form.BeforePhotoUrl,
            AfterPhotoUrl = form.AfterPhotoUrl,
            ClinicId = clinicIdForEntity
        };

        photo.CreatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);

        await _unitOfWork.Photos.AddAsync(photo);
        await _unitOfWork.CommitAsync();

        _logger.LogInformation("Photo uploaded for patient {PatientId}: {FileName}", form.PatientId, photo.FileName);
        return CreatedAtAction(nameof(GetById), new { id = photo.Id }, ApiResponse<PhotoResponse>.SuccessResponse(ToResponse(photo), "Photo uploaded"));
    }

    /// <summary>
    /// Get before/after comparisons for a patient
    /// </summary>
    [HttpGet("comparisons")]
    [ProducesResponseType(typeof(ApiResponse<List<BeforeAfterComparisonResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetComparisons([FromQuery] string patientId)
    {
        var photos = await _unitOfWork.Photos.FindAsync(
            p => p.PatientId == patientId && !p.IsDeleted && (p.Category == PhotoCategory.PreTreatment || p.Category == PhotoCategory.PostTreatment));

        var comparisons = photos
            .Select(p => new BeforeAfterComparisonResponse
            {
                PhotoId = p.Id,
                BeforePhotoUrl = p.BeforePhotoUrl ?? (p.Category == PhotoCategory.PreTreatment ? p.Url : null),
                AfterPhotoUrl = p.AfterPhotoUrl ?? (p.Category == PhotoCategory.PostTreatment ? p.Url : null),
                ComparisonScore = p.ComparisonScore,
                Category = p.Category,
                VisitDate = p.VisitDate,
                Description = p.Description
            })
            .ToList();

        return Ok(ApiResponse<List<BeforeAfterComparisonResponse>>.SuccessResponse(comparisons, "Comparisons retrieved"));
    }

    /// <summary>
    /// Update photo metadata
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PhotoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdatePhotoRequest request)
    {
        var photo = await _unitOfWork.Photos.GetByIdAsync(id);
        if (photo == null || photo.IsDeleted)
        {
            return NotFound(ApiResponse.FailResponse("Photo not found"));
        }

        if (request.Category.HasValue) photo.Category = request.Category.Value;
        if (request.Description != null) photo.Description = request.Description;
        if (request.VisitDate.HasValue) photo.VisitDate = request.VisitDate;
        if (request.BeforePhotoUrl != null) photo.BeforePhotoUrl = request.BeforePhotoUrl;
        if (request.AfterPhotoUrl != null) photo.AfterPhotoUrl = request.AfterPhotoUrl;
        if (request.ComparisonScore.HasValue) photo.ComparisonScore = request.ComparisonScore;

        photo.UpdatedAt = DateTime.UtcNow;
        photo.UpdatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);

        _unitOfWork.Photos.Update(photo);
        await _unitOfWork.CommitAsync();

        return Ok(ApiResponse<PhotoResponse>.SuccessResponse(ToResponse(photo), "Photo updated"));
    }

    /// <summary>
    /// Delete a photo (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id)
    {
        var photo = await _unitOfWork.Photos.GetByIdAsync(id);
        if (photo == null || photo.IsDeleted)
        {
            return NotFound(ApiResponse.FailResponse("Photo not found"));
        }

        photo.IsDeleted = true;
        photo.UpdatedAt = DateTime.UtcNow;
        photo.UpdatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);

        _unitOfWork.Photos.Update(photo);
        await _unitOfWork.CommitAsync();

        _logger.LogInformation("Photo deleted: {PhotoId}", id);
        return Ok(ApiResponse.SuccessResponse("Photo deleted"));
    }

    private static PhotoResponse ToResponse(Photo p) => new()
    {
        Id = p.Id,
        PatientId = p.PatientId,
        DoctorId = p.DoctorId,
        Url = p.Url,
        FileName = p.FileName,
        FileType = p.FileType,
        FileSize = p.FileSize,
        Category = p.Category,
        Description = p.Description,
        VisitDate = p.VisitDate,
        BeforePhotoUrl = p.BeforePhotoUrl,
        AfterPhotoUrl = p.AfterPhotoUrl,
        ComparisonScore = p.ComparisonScore,
        CreatedAt = p.CreatedAt
    };

    private static string guessExtension(string? fileType) => fileType?.ToLowerInvariant() switch
    {
        "image/jpeg" or "image/jpg" => ".jpg",
        "image/png" => ".png",
        "image/webp" => ".webp",
        "image/gif" => ".gif",
        _ => ".jpg"
    };
}

public class UpdatePhotoRequest
{
    public PhotoCategory? Category { get; set; }
    public string? Description { get; set; }
    public DateTime? VisitDate { get; set; }
    public string? BeforePhotoUrl { get; set; }
    public string? AfterPhotoUrl { get; set; }
    public double? ComparisonScore { get; set; }
}
