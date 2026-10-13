using OrthoFlowAI.Domain.Entities;

namespace OrthoFlowAI.Domain.Common;

public abstract class BaseEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}

public abstract class TenantEntity : BaseEntity
{
    public string ClinicId { get; set; } = string.Empty;
    public Clinic Clinic { get; set; } = null!;
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ApiResponse<T> SuccessResponse(T data, string message = "Success")
        => new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> FailResponse(string message, List<string>? errors = null)
        => new() { Success = false, Message = message, Errors = errors ?? new() };
}

public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse SuccessResponse(string message = "Success")
        => new() { Success = true, Message = message };

    public new static ApiResponse FailResponse(string message, List<string>? errors = null)
        => new() { Success = false, Message = message, Errors = errors ?? new() };
}
