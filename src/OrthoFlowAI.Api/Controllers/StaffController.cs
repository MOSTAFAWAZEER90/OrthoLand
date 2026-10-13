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
public class StaffController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<StaffController> _logger;

    public StaffController(IUnitOfWork unitOfWork, ILogger<StaffController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<StaffResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null, [FromQuery] StaffStatus? status = null)
    {
        var staff = await _unitOfWork.Staff.FindAsync(s =>
            (status == null || s.Status == status.Value) &&
            (string.IsNullOrEmpty(search) ||
             s.User.FirstName.Contains(search) || s.User.LastName.Contains(search)),
            s => s.User);

        var items = staff.Select(s => new StaffResponse
        {
            Id = s.Id,
            UserId = s.UserId,
            FullName = $"{s.User.FirstName} {s.User.LastName}".Trim(),
            Role = s.User.Role,
            Position = s.Position,
            Department = s.Department,
            Status = s.Status,
            HireDate = s.HireDate,
            Salary = s.Salary
        }).OrderBy(s => s.FullName).ToList();

        return Ok(ApiResponse<PagedResult<StaffResponse>>.SuccessResponse(new PagedResult<StaffResponse>
        {
            Items = items,
            TotalCount = items.Count,
            PageNumber = pageNumber,
            PageSize = pageSize
        }));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var staff = (await _unitOfWork.Staff.FindAsync(s => s.Id == id, s => s.User)).FirstOrDefault();
        if (staff == null || staff.IsDeleted) return NotFound(ApiResponse.FailResponse("Staff not found"));

        return Ok(ApiResponse<StaffResponse>.SuccessResponse(new StaffResponse
        {
            Id = staff.Id,
            UserId = staff.UserId,
            FullName = $"{staff.User.FirstName} {staff.User.LastName}".Trim(),
            Role = staff.User.Role,
            Position = staff.Position,
            Department = staff.Department,
            Status = staff.Status,
            HireDate = staff.HireDate,
            Salary = staff.Salary
        }));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Staff>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] StaffDto request)
    {
        var clinicId = HttpContext.Items["ClinicId"]?.ToString() ?? string.Empty;

        // Verify the referenced user belongs to this clinic
        var users = await _unitOfWork.ApplicationUsers.FindAsync(u => u.Id == request.UserId);
        var linkedUser = users.FirstOrDefault();
        if (linkedUser == null)
        {
            return BadRequest(ApiResponse.FailResponse("Linked user not found."));
        }

        var existing = await _unitOfWork.Staff.FindAsync(s => s.UserId == request.UserId);
        if (existing.Any())
        {
            return BadRequest(ApiResponse.FailResponse("This user is already linked as staff."));
        }

        var staff = new Staff
        {
            UserId = request.UserId,
            Position = request.Position,
            Department = request.Department,
            Status = request.Status,
            HireDate = request.HireDate,
            Salary = request.Salary,
            ClinicId = clinicId
        };
        staff.CreatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);

        await _unitOfWork.Staff.AddAsync(staff);
        await _unitOfWork.CommitAsync();

        return CreatedAtAction(nameof(GetById), new { id = staff.Id }, ApiResponse<Staff>.SuccessResponse(staff, "Staff created"));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] StaffUpdateDto request)
    {
        var staff = await _unitOfWork.Staff.GetByIdAsync(id);
        if (staff == null || staff.IsDeleted) return NotFound(ApiResponse.FailResponse("Staff not found"));

        if (request.Position != null) staff.Position = request.Position;
        if (request.Department != null) staff.Department = request.Department;
        if (request.Status.HasValue) staff.Status = request.Status.Value;
        if (request.HireDate.HasValue) staff.HireDate = request.HireDate;
        if (request.Salary.HasValue) staff.Salary = request.Salary;
        staff.UpdatedAt = DateTime.UtcNow;
        staff.UpdatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);

        _unitOfWork.Staff.Update(staff);
        await _unitOfWork.CommitAsync();
        return Ok(ApiResponse.SuccessResponse("Staff updated"));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var staff = await _unitOfWork.Staff.GetByIdAsync(id);
        if (staff == null || staff.IsDeleted) return NotFound(ApiResponse.FailResponse("Staff not found"));

        staff.IsDeleted = true;
        _unitOfWork.Staff.Update(staff);
        await _unitOfWork.CommitAsync();
        return Ok(ApiResponse.SuccessResponse("Staff removed"));
    }
}