using System;
using Volo.Abp.Application.Dtos;

namespace SmartPantry.PantryItems;

public class PantryItemDto : EntityDto<Guid>
{
    public Guid OwnerId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductBrand { get; set; }
    public PantryItemStatus Status { get; set; }
    public DateOnly? ExpirationDate { get; set; }
    public decimal? Quantity { get; set; }
    public string? Unit { get; set; }
}