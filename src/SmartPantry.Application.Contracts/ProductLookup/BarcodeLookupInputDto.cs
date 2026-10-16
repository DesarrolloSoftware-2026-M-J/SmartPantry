using System.ComponentModel.DataAnnotations;

namespace SmartPantry.ProductLookup;

public class BarcodeLookupInputDto
{
    [Required]
    [RegularExpression(@"^\d{8,14}$", ErrorMessage = "El código de barras debe tener entre 8 y 14 dígitos.")]
    public string Barcode { get; set; } = default!;
}