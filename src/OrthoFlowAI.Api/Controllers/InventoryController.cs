using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrthoFlowAI.Application.DTOs;
using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Entities;
using OrthoFlowAI.Domain.Enums;
using OrthoFlowAI.Domain.Interfaces;

namespace OrthoFlowAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<InventoryController> _logger;

    public InventoryController(IUnitOfWork unitOfWork, ILogger<InventoryController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<InventoryItem>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null, [FromQuery] InventoryCategory? category = null, [FromQuery] bool? lowStock = null)
    {
        var query = _unitOfWork.InventoryItems.Query();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(i => i.Name.Contains(search) || (i.Brand != null && i.Brand.Contains(search)));
        }
        if (category.HasValue)
        {
            query = query.Where(i => i.Category == category.Value);
        }
        if (lowStock == true)
        {
            query = query.Where(i => i.Quantity <= i.MinQuantity);
        }

        var total = query.Count();
        var items = query.OrderBy(i => i.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var result = new PagedResult<InventoryItem>
        {
            Items = items,
            TotalCount = total,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return Ok(ApiResponse<PagedResult<InventoryItem>>.SuccessResponse(result, "Inventory retrieved"));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var item = await _unitOfWork.InventoryItems.GetByIdAsync(id);
        if (item == null || item.IsDeleted) return NotFound(ApiResponse.FailResponse("Item not found"));
        return Ok(ApiResponse<InventoryItem>.SuccessResponse(item));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<InventoryItem>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] InventoryDto request)
    {
        var clinicId = HttpContext.Items["ClinicId"]?.ToString() ?? string.Empty;
        var item = new InventoryItem
        {
            Name = request.Name,
            Category = request.Category,
            Brand = request.Brand,
            Model = request.Model,
            Quantity = request.Quantity,
            MinQuantity = request.MinQuantity,
            UnitPrice = request.UnitPrice,
            SupplierName = request.SupplierName,
            SupplierPhone = request.SupplierPhone,
            BranchId = request.BranchId,
            ClinicId = clinicId
        };
        item.CreatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);

        await _unitOfWork.InventoryItems.AddAsync(item);
        await _unitOfWork.CommitAsync();

        return CreatedAtAction(nameof(GetById), new { id = item.Id }, ApiResponse<InventoryItem>.SuccessResponse(item, "Item created"));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] InventoryDto request)
    {
        var item = await _unitOfWork.InventoryItems.GetByIdAsync(id);
        if (item == null || item.IsDeleted) return NotFound(ApiResponse.FailResponse("Item not found"));

        item.Name = request.Name;
        item.Category = request.Category;
        item.Brand = request.Brand;
        item.Model = request.Model;
        item.Quantity = request.Quantity;
        item.MinQuantity = request.MinQuantity;
        item.UnitPrice = request.UnitPrice;
        item.SupplierName = request.SupplierName;
        item.SupplierPhone = request.SupplierPhone;
        item.BranchId = request.BranchId;
        item.UpdatedAt = DateTime.UtcNow;
        item.UpdatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);

        _unitOfWork.InventoryItems.Update(item);
        await _unitOfWork.CommitAsync();
        return Ok(ApiResponse<InventoryItem>.SuccessResponse(item, "Item updated"));
    }

    [HttpPost("{id}/restock")]
    public async Task<IActionResult> Restock(string id, [FromBody] RestockRequest request)
    {
        var item = await _unitOfWork.InventoryItems.GetByIdAsync(id);
        if (item == null || item.IsDeleted) return NotFound(ApiResponse.FailResponse("Item not found"));

        item.Quantity += request.Quantity;
        item.LastRestockedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.InventoryItems.Update(item);
        await _unitOfWork.CommitAsync();
        return Ok(ApiResponse<InventoryItem>.SuccessResponse(item, "Stock updated"));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var item = await _unitOfWork.InventoryItems.GetByIdAsync(id);
        if (item == null || item.IsDeleted) return NotFound(ApiResponse.FailResponse("Item not found"));

        item.IsDeleted = true;
        _unitOfWork.InventoryItems.Update(item);
        await _unitOfWork.CommitAsync();
        return Ok(ApiResponse.SuccessResponse("Item deleted"));
    }
}
