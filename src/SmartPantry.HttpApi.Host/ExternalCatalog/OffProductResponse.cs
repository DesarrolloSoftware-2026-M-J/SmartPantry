using System.Text.Json.Serialization;

namespace SmartPantry.ExternalCatalog;

internal class OffProductResponse
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("product")]
    public OffProduct? Product { get; set; }
}

internal class OffProduct
{
    [JsonPropertyName("product_name")]
    public string? ProductName { get; set; }

    [JsonPropertyName("product_name_es")]
    public string? ProductNameEs { get; set; }

    [JsonPropertyName("brands")]
    public string? Brands { get; set; }

    [JsonPropertyName("image_front_url")]
    public string? ImageFrontUrl { get; set; }
}