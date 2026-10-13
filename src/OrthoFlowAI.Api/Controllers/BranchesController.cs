using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrthoFlowAI.Application.DTOs;
using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Entities;
using OrthoFlowAI.Domain.Interfaces;

namespace OrthoFlowAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BranchesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BranchesController> _logger;

    public BranchesController(IUnitOfWork unitOfWork, ILogger<BranchesController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<Branch>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50)
    {
        var branches = await _unitOfWork.Branches.GetAllAsync();
        var items = branches.OrderBy(b => b.Name).ToList();

        return Ok(ApiResponse<PagedResult<Branch>>.SuccessResponse(new PagedResult<Branch>
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
        var branch = await _unitOfWork.Branches.GetByIdAsync(id);
        if (branch == null || branch.IsDeleted) return NotFound(ApiResponse.FailResponse("Branch not found"));
        return Ok(ApiResponse<Branch>.SuccessResponse(branch));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Branch>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] BranchDto request)
    {
        var clinicId = HttpContext.Items["ClinicId"]?.ToString() ?? string.Empty;
        var branch = new Branch
        {
            Name = request.Name,
            Address = request.Address,
            Phone = request.Phone,
            Email = request.Email,
            IsMainBranch = request.IsMainBranch,
            OperatingHours = request.OperatingHours,
            ClinicId = clinicId
        };
        branch.CreatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);

        await _unitOfWork.Branches.AddAsync(branch);
        await _unitOfWork.CommitAsync();

        return CreatedAtAction(nameof(GetById), new { id = branch.Id }, ApiResponse<Branch>.SuccessResponse(branch, "Branch created"));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] BranchDto request)
    {
        var branch = await _unitOfWork.Branches.GetByIdAsync(id);
        if (branch == null || branch.IsDeleted) return NotFound(ApiResponse.FailResponse("Branch not found"));

        branch.Name = request.Name;
        branch.Address = request.Address;
        branch.Phone = request.Phone;
        branch.Email = request.Email;
        branch.IsMainBranch = request.IsMainBranch;
        branch.OperatingHours = request.OperatingHours;
        branch.UpdatedAt = DateTime.UtcNow;
        branch.UpdatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);

        _unitOfWork.Branches.Update(branch);
        await _unitOfWork.CommitAsync();
        return Ok(ApiResponse<Branch>.SuccessResponse(branch, "Branch updated"));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var branch = await _unitOfWork.Branches.GetByIdAsync(id);
        if (branch == null || branch.IsDeleted) return NotFound(ApiResponse.FailResponse("Branch not found"));

        branch.IsDeleted = true;
        _unitOfWork.Branches.Update(branch);
        await _unitOfWork.CommitAsync();
        return Ok(ApiResponse.SuccessResponse("Branch deleted"));
    }
}
