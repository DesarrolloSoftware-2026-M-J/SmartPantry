using System;
using System.ComponentModel.DataAnnotations;

namespace SmartPantry.PantryItems;

public class CreatePantryItemDto
{
    [Required]
    public Guid ProductId { get; set; }

    public DateOnly? ExpirationDate { get; set; }

    [Range(0.001, 1000000)]
    public decimal? Quantity { get; set; }

    [StringLength(32)]
    public string? Unit { get; set; }
}