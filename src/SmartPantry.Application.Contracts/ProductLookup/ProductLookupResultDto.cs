namespace SmartPantry.ProductLookup;

public class ProductLookupResultDto
{
    public ProductLookupStatus Status { get; set; }

    public string Barcode { get; set; } = default!;
    public string? Name { get; set; }
    public string? Brand { get; set; }
    public string? ImageUrl { get; set; }
}