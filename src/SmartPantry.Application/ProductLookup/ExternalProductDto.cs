namespace SmartPantry.ProductLookup;

public class ExternalProductDto
{
    public string Barcode { get; set; } = default!;
    public string? Name { get; set; }
    public string? Brand { get; set; }
    public string? ImageUrl { get; set; }
}