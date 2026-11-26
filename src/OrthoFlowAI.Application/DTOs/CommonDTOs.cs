using System.ComponentModel.DataAnnotations;

namespace OrthoFlowAI.Application.DTOs.Common;

public class PaginationRequest
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    // The web UI loads full lists (photos, branches, staff) with pageSize=200, which the
    // old limit of 100 rejected with 400, leaving those screens empty.
    [Range(1, 500)]
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

/// <summary>An id + display name pair for pickers (see /api/lookups).</summary>
public class LookupItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Detail { get; set; }

    public override string ToString() => Name;
}
