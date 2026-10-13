using System.Collections.Generic;
using System.Threading.Tasks;
using OrthoFlowAI.Application.DTOs;
using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Enums;

namespace OrthoFlowAI.Application.Services;

public interface IDocumentService
{
    Task<ApiResponse<PagedResult<DocumentResponse>>> GetAllAsync(DocumentFilterRequest filter);
    Task<ApiResponse<DocumentResponse>> GetByIdAsync(string id);
    Task<ApiResponse<DocumentResponse>> CreateAsync(CreateDocumentRequest request, string uploadedById, string fileUrl, string fileType, long fileSize);
    Task<ApiResponse<DocumentResponse>> UpdateAsync(string id, UpdateDocumentRequest request);
    Task<ApiResponse> DeleteAsync(string id);
    Task<ApiResponse<DocumentResponse>> SignAsync(string id, SignDocumentRequest request, string signedById);
    Task<ApiResponse<List<DocumentResponse>>> BulkSignAsync(BulkSignRequest request, string signedById);
    Task<ApiResponse<byte[]>> GenerateDocumentPdfAsync(string id);
    Task<ApiResponse<List<DocumentResponse>>> GetPatientDocumentsAsync(string patientId);
    Task<ApiResponse<int>> GetDocumentCountAsync(string clinicId, DocumentType? type = null, DocumentStatus? status = null);
}