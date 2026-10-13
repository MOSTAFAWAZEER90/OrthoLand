using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Enums;

namespace OrthoFlowAI.Domain.Entities;

public class InventoryItem : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public InventoryCategory Category { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public int Quantity { get; set; }
    public int MinQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? SupplierName { get; set; }
    public string? SupplierPhone { get; set; }
    public DateTime? LastRestockedAt { get; set; }
    public string? BranchId { get; set; }

    public Branch? Branch { get; set; }
}
