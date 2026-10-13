using System.Linq.Expressions;
using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Entities;
using OrthoFlowAI.Domain.Interfaces;

namespace OrthoFlowAI.Infrastructure.Services;

public class ClinicService : IClinicService
{
    private readonly IUnitOfWork _unitOfWork;

    public ClinicService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<Clinic>> GetByIdAsync(string id)
    {
        var clinic = await _unitOfWork.Clinics.GetByIdAsync(id);
        if (clinic is null)
        {
            return ApiResponse<Clinic>.FailResponse("Clinic not found.");
        }

        return ApiResponse<Clinic>.SuccessResponse(clinic);
    }

    public async Task<ApiResponse<PagedResult<Clinic>>> GetAllAsync(
        int pageNumber, int pageSize, string? search = null, bool? isActive = null)
    {
        Expression<Func<Clinic, bool>>? filter = null;

        if (!string.IsNullOrWhiteSpace(search) && isActive.HasValue)
        {
            filter = c => c.Name.Contains(search) && c.IsActive == isActive.Value;
        }
        else if (!string.IsNullOrWhiteSpace(search))
        {
            filter = c => c.Name.Contains(search);
        }
        else if (isActive.HasValue)
        {
            filter = c => c.IsActive == isActive.Value;
        }

        var result = await _unitOfWork.Clinics.GetPagedAsync(
            pageNumber,
            pageSize,
            filter,
            q => q.OrderByDescending(c => c.CreatedAt));

        return ApiResponse<PagedResult<Clinic>>.SuccessResponse(result);
    }

    public async Task<ApiResponse<Clinic>> CreateAsync(Clinic clinic)
    {
        clinic.CreatedAt = DateTime.UtcNow;
        clinic.IsActive = true;

        await _unitOfWork.Clinics.AddAsync(clinic);
        await _unitOfWork.CommitAsync();

        return ApiResponse<Clinic>.SuccessResponse(clinic, "Clinic created successfully.");
    }

    public async Task<ApiResponse<Clinic>> UpdateAsync(Clinic clinic)
    {
        var existing = await _unitOfWork.Clinics.GetByIdAsync(clinic.Id);
        if (existing is null)
        {
            return ApiResponse<Clinic>.FailResponse("Clinic not found.");
        }

        existing.Name = clinic.Name;
        existing.Logo = clinic.Logo;
        existing.Phone = clinic.Phone;
        existing.Email = clinic.Email;
        existing.Address = clinic.Address;
        existing.City = clinic.City;
        existing.Country = clinic.Country;
        existing.Website = clinic.Website;
        existing.BusinessHours = clinic.BusinessHours;
        existing.WorkingDays = clinic.WorkingDays;
        existing.PrimaryColor = clinic.PrimaryColor;
        existing.SecondaryColor = clinic.SecondaryColor;
        existing.Language = clinic.Language;

        _unitOfWork.Clinics.Update(existing);
        await _unitOfWork.CommitAsync();

        return ApiResponse<Clinic>.SuccessResponse(existing, "Clinic updated successfully.");
    }

    public async Task<ApiResponse> DeleteAsync(string id)
    {
        var clinic = await _unitOfWork.Clinics.GetByIdAsync(id);
        if (clinic is null)
        {
            return ApiResponse.FailResponse("Clinic not found.");
        }

        clinic.IsDeleted = true;
        _unitOfWork.Clinics.Update(clinic);
        await _unitOfWork.CommitAsync();

        return ApiResponse.SuccessResponse("Clinic deleted successfully.");
    }

    public async Task<ApiResponse<bool>> ToggleActiveAsync(string id)
    {
        var clinic = await _unitOfWork.Clinics.GetByIdAsync(id);
        if (clinic is null)
        {
            return ApiResponse<bool>.FailResponse("Clinic not found.");
        }

        clinic.IsActive = !clinic.IsActive;
        _unitOfWork.Clinics.Update(clinic);
        await _unitOfWork.CommitAsync();

        return ApiResponse<bool>.SuccessResponse(clinic.IsActive, $"Clinic is now {(clinic.IsActive ? "active" : "inactive")}.");
    }
}
