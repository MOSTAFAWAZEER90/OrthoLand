using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrthoFlowAI.Application.DTOs;
using OrthoFlowAI.Application.Services;
using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Entities;
using OrthoFlowAI.Domain.Enums;
using OrthoFlowAI.Domain.Interfaces;
using OrthoFlowAI.Infrastructure.Data;

namespace OrthoFlowAI.Infrastructure.Services;

public class DocumentService : IDocumentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(IUnitOfWork unitOfWork, ApplicationDbContext context, ILogger<DocumentService> logger)
    {
        _unitOfWork = unitOfWork;
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<PagedResult<DocumentResponse>>> GetAllAsync(DocumentFilterRequest filter)
    {
        try
        {
            var clinicId = filter.ClinicId ?? _unitOfWork.GetCurrentClinicId();
            if (string.IsNullOrEmpty(clinicId))
                return ApiResponse<PagedResult<DocumentResponse>>.FailResponse("Clinic context not found");

            var query = _context.Documents
                .Include(d => d.Patient)
                .Where(d => !d.IsDeleted && d.ClinicId == clinicId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(d => d.FileName.Contains(filter.SearchTerm) ||
                                        d.Description!.Contains(filter.SearchTerm) ||
                                        d.Patient.FirstName.Contains(filter.SearchTerm) ||
                                        d.Patient.LastName.Contains(filter.SearchTerm));
            }

            if (!string.IsNullOrEmpty(filter.PatientId))
                query = query.Where(d => d.PatientId == filter.PatientId);

            if (filter.Type.HasValue)
                query = query.Where(d => d.Type == filter.Type.Value);

            if (filter.Status.HasValue)
                query = query.Where(d => d.Status == filter.Status.Value);

            if (filter.IsSigned.HasValue)
                query = query.Where(d => d.IsSigned == filter.IsSigned.Value);

            if (filter.FromDate.HasValue)
                query = query.Where(d => d.CreatedAt >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(d => d.CreatedAt <= filter.ToDate.Value);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(d => d.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(d => MapToResponse(d))
                .ToListAsync();

            var result = new PagedResult<DocumentResponse>
            {
                Items = items,
                TotalCount = total,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };

            return ApiResponse<PagedResult<DocumentResponse>>.SuccessResponse(result, "Documents retrieved");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting documents");
            return ApiResponse<PagedResult<DocumentResponse>>.FailResponse($"Error: {ex.Message}");
        }
    }

    public async Task<ApiResponse<DocumentResponse>> GetByIdAsync(string id)
    {
        try
        {
            var clinicId = _unitOfWork.GetCurrentClinicId();
            if (string.IsNullOrEmpty(clinicId))
                return ApiResponse<DocumentResponse>.FailResponse("Clinic context not found");

            var document = await _context.Documents
                .Include(d => d.Patient)
                .FirstOrDefaultAsync(d => d.Id == id && d.ClinicId == clinicId && !d.IsDeleted);

            if (document == null)
                return ApiResponse<DocumentResponse>.FailResponse("Document not found");

            return ApiResponse<DocumentResponse>.SuccessResponse(MapToResponse(document), "Document retrieved");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document {Id}", id);
            return ApiResponse<DocumentResponse>.FailResponse($"Error: {ex.Message}");
        }
    }

    public async Task<ApiResponse<DocumentResponse>> CreateAsync(CreateDocumentRequest request, string uploadedById, string fileUrl, string fileType, long fileSize)
    {
        try
        {
            var clinicId = _unitOfWork.GetCurrentClinicId();
            if (string.IsNullOrEmpty(clinicId))
                return ApiResponse<DocumentResponse>.FailResponse("Clinic context not found");

            var patient = await _unitOfWork.Patients.GetByIdAsync(request.PatientId);
            if (patient == null || patient.IsDeleted || patient.ClinicId != clinicId)
                return ApiResponse<DocumentResponse>.FailResponse("Patient not found");

            var document = new Document
            {
                PatientId = request.PatientId,
                UploadedById = uploadedById,
                FileName = request.FileName,
                FileUrl = fileUrl,
                FileType = fileType,
                FileSize = fileSize,
                Type = request.Type,
                Description = request.Description,
                Tags = request.Tags,
                ExpiryDate = request.ExpiryDate,
                Status = DocumentStatus.Draft,
                ClinicId = clinicId
            };
            document.CreatedBy = uploadedById;

            await _unitOfWork.Documents.AddAsync(document);
            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Document created: {Id} for patient {PatientId}", document.Id, request.PatientId);
            return ApiResponse<DocumentResponse>.SuccessResponse(MapToResponse(document), "Document created");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating document");
            return ApiResponse<DocumentResponse>.FailResponse($"Error: {ex.Message}");
        }
    }

    public async Task<ApiResponse<DocumentResponse>> UpdateAsync(string id, UpdateDocumentRequest request)
    {
        try
        {
            var clinicId = _unitOfWork.GetCurrentClinicId();
            if (string.IsNullOrEmpty(clinicId))
                return ApiResponse<DocumentResponse>.FailResponse("Clinic context not found");

            var document = await _unitOfWork.Documents.GetByIdAsync(id);

            if (document == null || document.IsDeleted || document.ClinicId != clinicId)
                return ApiResponse<DocumentResponse>.FailResponse("Document not found");

            if (request.FileName != null) document.FileName = request.FileName;
            if (request.Type.HasValue) document.Type = request.Type.Value;
            if (request.Description != null) document.Description = request.Description;
            if (request.ExpiryDate.HasValue) document.ExpiryDate = request.ExpiryDate;
            if (request.Tags != null) document.Tags = request.Tags;
            if (request.Status.HasValue) document.Status = request.Status.Value;

            document.UpdatedAt = DateTime.UtcNow;
            document.UpdatedBy = _unitOfWork.GetCurrentUserId();

            _unitOfWork.Documents.Update(document);
            await _unitOfWork.CommitAsync();

            return ApiResponse<DocumentResponse>.SuccessResponse(MapToResponse(document), "Document updated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document {Id}", id);
            return ApiResponse<DocumentResponse>.FailResponse($"Error: {ex.Message}");
        }
    }

    public async Task<ApiResponse> DeleteAsync(string id)
    {
        try
        {
            var clinicId = _unitOfWork.GetCurrentClinicId();
            if (string.IsNullOrEmpty(clinicId))
                return ApiResponse.FailResponse("Clinic context not found");

            var document = await _unitOfWork.Documents.GetByIdAsync(id);

            if (document == null || document.IsDeleted || document.ClinicId != clinicId)
                return ApiResponse.FailResponse("Document not found");

            document.IsDeleted = true;
            document.UpdatedAt = DateTime.UtcNow;
            document.UpdatedBy = _unitOfWork.GetCurrentUserId();

            _unitOfWork.Documents.Update(document);
            await _unitOfWork.CommitAsync();

            return ApiResponse.SuccessResponse("Document deleted");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {Id}", id);
            return ApiResponse.FailResponse($"Error: {ex.Message}");
        }
    }

    public async Task<ApiResponse<DocumentResponse>> SignAsync(string id, SignDocumentRequest request, string signedById)
    {
        try
        {
            var clinicId = _unitOfWork.GetCurrentClinicId();
            if (string.IsNullOrEmpty(clinicId))
                return ApiResponse<DocumentResponse>.FailResponse("Clinic context not found");

            var document = await _context.Documents
                .Include(d => d.Patient)
                .FirstOrDefaultAsync(d => d.Id == id && d.ClinicId == clinicId && !d.IsDeleted);

            if (document == null)
                return ApiResponse<DocumentResponse>.FailResponse("Document not found");

            if (document.IsSigned)
                return ApiResponse<DocumentResponse>.FailResponse("Document already signed");

            document.IsSigned = true;
            document.SignedAt = DateTime.UtcNow;
            document.SignedById = signedById;
            document.SignatureData = request.SignatureData;
            document.SignatureImageUrl = request.SignatureImageUrl;
            document.IpAddress = request.IpAddress;
            document.UserAgent = request.UserAgent;
            document.Status = DocumentStatus.Signed;
            document.UpdatedAt = DateTime.UtcNow;
            document.UpdatedBy = signedById;

            _unitOfWork.Documents.Update(document);
            await _unitOfWork.CommitAsync();

            await CreateSignNotification(document);

            _logger.LogInformation("Document {Id} signed by {SignedById}", id, signedById);
            return ApiResponse<DocumentResponse>.SuccessResponse(MapToResponse(document), "Document signed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error signing document {Id}", id);
            return ApiResponse<DocumentResponse>.FailResponse($"Error: {ex.Message}");
        }
    }

    public async Task<ApiResponse<List<DocumentResponse>>> BulkSignAsync(BulkSignRequest request, string signedById)
    {
        try
        {
            var clinicId = _unitOfWork.GetCurrentClinicId();
            if (string.IsNullOrEmpty(clinicId))
                return ApiResponse<List<DocumentResponse>>.FailResponse("Clinic context not found");

            var documents = await _context.Documents
                .Where(d => request.DocumentIds.Contains(d.Id) && d.ClinicId == clinicId && !d.IsDeleted && !d.IsSigned)
                .ToListAsync();

            if (!documents.Any())
                return ApiResponse<List<DocumentResponse>>.FailResponse("No valid documents to sign");

            var results = new List<DocumentResponse>();

            foreach (var document in documents)
            {
                document.IsSigned = true;
                document.SignedAt = DateTime.UtcNow;
                document.SignedById = signedById;
                document.SignatureData = request.SignatureData;
                document.SignatureImageUrl = request.SignatureImageUrl;
                document.Status = DocumentStatus.Signed;
                document.UpdatedAt = DateTime.UtcNow;
                document.UpdatedBy = signedById;

                _unitOfWork.Documents.Update(document);
                results.Add(MapToResponse(document));

                await CreateSignNotification(document);
            }

            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Bulk signed {Count} documents by {SignedById}", documents.Count, signedById);
            return ApiResponse<List<DocumentResponse>>.SuccessResponse(results, $"{documents.Count} documents signed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk signing documents");
            return ApiResponse<List<DocumentResponse>>.FailResponse($"Error: {ex.Message}");
        }
    }

    public async Task<ApiResponse<byte[]>> GenerateDocumentPdfAsync(string id)
    {
        try
        {
            var document = await GetByIdAsync(id);
            if (!document.Success || document.Data == null)
                return ApiResponse<byte[]>.FailResponse("Document not found");

            var pdfContent = $@"
                DOCUMENT: {document.Data.FileName}
                TYPE: {document.Data.Type}
                PATIENT: {document.Data.PatientName}
                UPLOADED: {document.Data.CreatedAt:yyyy-MM-dd}
                STATUS: {document.Data.Status}
                {(document.Data.IsSigned ? $"SIGNED: {document.Data.SignedAt:yyyy-MM-dd HH:mm} BY {document.Data.SignedByName}" : "NOT SIGNED")}
                DESCRIPTION: {document.Data.Description ?? "N/A"}
            ";

            var bytes = System.Text.Encoding.UTF8.GetBytes(pdfContent);
            return ApiResponse<byte[]>.SuccessResponse(bytes, "PDF generated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for document {Id}", id);
            return ApiResponse<byte[]>.FailResponse($"Error: {ex.Message}");
        }
    }

    public async Task<ApiResponse<List<DocumentResponse>>> GetPatientDocumentsAsync(string patientId)
    {
        try
        {
            var clinicId = _unitOfWork.GetCurrentClinicId();
            if (string.IsNullOrEmpty(clinicId))
                return ApiResponse<List<DocumentResponse>>.FailResponse("Clinic context not found");

            var documents = await _context.Documents
                .Where(d => d.PatientId == patientId && d.ClinicId == clinicId && !d.IsDeleted)
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => MapToResponse(d))
                .ToListAsync();

            return ApiResponse<List<DocumentResponse>>.SuccessResponse(documents, "Patient documents retrieved");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting patient documents for {PatientId}", patientId);
            return ApiResponse<List<DocumentResponse>>.FailResponse($"Error: {ex.Message}");
        }
    }

    public async Task<ApiResponse<int>> GetDocumentCountAsync(string clinicId, DocumentType? type = null, DocumentStatus? status = null)
    {
        try
        {
            var query = _context.Documents.Where(d => d.ClinicId == clinicId && !d.IsDeleted);

            if (type.HasValue)
                query = query.Where(d => d.Type == type.Value);

            if (status.HasValue)
                query = query.Where(d => d.Status == status.Value);

            var count = await query.CountAsync();
            return ApiResponse<int>.SuccessResponse(count, "Document count retrieved");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document count");
            return ApiResponse<int>.FailResponse($"Error: {ex.Message}");
        }
    }

    private DocumentResponse MapToResponse(Document d) => new()
    {
        Id = d.Id,
        PatientId = d.PatientId,
        PatientName = d.Patient != null ? $"{d.Patient.FirstName} {d.Patient.LastName}" : "",
        UploadedById = d.UploadedById,
        FileName = d.FileName,
        FileUrl = d.FileUrl,
        FileType = d.FileType,
        FileSize = d.FileSize,
        Type = d.Type,
        Description = d.Description,
        IsSigned = d.IsSigned,
        SignedAt = d.SignedAt,
        SignedById = d.SignedById,
        SignatureImageUrl = d.SignatureImageUrl,
        Status = d.Status,
        ExpiryDate = d.ExpiryDate,
        Tags = d.Tags,
        CreatedAt = d.CreatedAt,
        UpdatedAt = d.UpdatedAt ?? DateTime.UtcNow
    };

    private async Task CreateSignNotification(Document document)
    {
        if (document.Patient == null || string.IsNullOrEmpty(document.Patient.UserId))
            return;

        var notification = new Notification
        {
            UserId = document.Patient.UserId,
            ClinicId = document.ClinicId,
            Title = "Document Signed",
            Message = $"Your document '{document.FileName}' has been signed.",
            Type = NotificationType.TreatmentUpdate,
            Channel = NotificationChannel.InApp,
            IsRead = false,
            RelatedEntityId = document.Id,
            RelatedEntityType = "Document"
        };
        notification.CreatedBy = "system";
        await _unitOfWork.Notifications.AddAsync(notification);
        await _unitOfWork.CommitAsync();
    }
}