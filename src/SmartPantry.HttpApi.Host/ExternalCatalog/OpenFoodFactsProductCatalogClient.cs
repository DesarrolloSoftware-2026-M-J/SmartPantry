using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using SmartPantry.ProductLookup;

namespace SmartPantry.ExternalCatalog;

public class OpenFoodFactsProductCatalogClient : IExternalProductCatalogClient
{
    private const string Fields = "code,product_name,product_name_es,brands,image_front_url";

    private readonly HttpClient _http;

    public OpenFoodFactsProductCatalogClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<ExternalProductDto?> GetByBarcodeAsync(string barcode)
    {
        var path = $"api/v3/product/{Uri.EscapeDataString(barcode)}?fields={Uri.EscapeDataString(Fields)}";

        HttpResponseMessage response;
        try
        {
            response = await _http.GetAsync(path);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new ExternalCatalogUnavailableException("No se pudo contactar a Open Food Facts.", ex);
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                throw new ExternalCatalogRateLimitException();
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new ExternalCatalogUnavailableException(
                    $"Open Food Facts respondió HTTP {(int)response.StatusCode}.");
            }

            OffProductResponse? body;
            try
            {
                body = await response.Content.ReadFromJsonAsync<OffProductResponse>();
            }
            catch (JsonException ex)
            {
                throw new ExternalCatalogUnavailableException(
                    "Open Food Facts devolvió una respuesta que no pudo interpretarse.", ex);
            }

            if (body?.Product is null)
            {
                return null;
            }

            return new ExternalProductDto
            {
                Barcode = body.Code ?? barcode,
                Name = FirstNonBlank(body.Product.ProductNameEs, body.Product.ProductName),
                Brand = FirstNonBlank(body.Product.Brands),
                ImageUrl = FirstNonBlank(body.Product.ImageFrontUrl)
            };
        }
    }
    private static string? FirstNonBlank(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
}