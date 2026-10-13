using System.ComponentModel.DataAnnotations;

namespace OrthoFlowAI.Application.DTOs.Common;

public class PaginationRequest
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    [MaxLength(500)]
    public string? SearchTerm { get; set; }

    [MaxLength(100)]
    public string? SortBy { get; set; }

    public bool SortDescending { get; set; }
}

public class PaginatedApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<T> Data { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;
    public List<string> Errors { get; set; } = new();

    public static PaginatedApiResponse<T> SuccessResponse(
        List<T> data, int totalCount, int pageNumber, int pageSize, string message = "Success")
        => new()
        {
            Success = true,
            Message = message,
            Data = data,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

    public static PaginatedApiResponse<T> FailResponse(string message, List<string>? errors = null)
        => new()
        {
            Success = false,
            Message = message,
            Errors = errors ?? new()
        };
}
