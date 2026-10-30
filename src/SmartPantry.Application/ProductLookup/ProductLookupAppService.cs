using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Microsoft.AspNetCore.Authorization;

namespace SmartPantry.ProductLookup;

[Authorize]
public class ProductLookupAppService : SmartPantryAppService, IProductLookupAppService
{
    private readonly IExternalProductCatalogClient _catalogClient;

    public ProductLookupAppService(IExternalProductCatalogClient catalogClient)
    {
        _catalogClient = catalogClient;
    }

    public async Task<ProductLookupResultDto> GetByBarcodeAsync(BarcodeLookupInputDto input)
    {
        try
        {
            var external = await _catalogClient.GetByBarcodeAsync(input.Barcode);

            if (external is null)
            {
                return new ProductLookupResultDto
                {
                    Status = ProductLookupStatus.NotFound,
                    Barcode = input.Barcode
                };
            }

            return new ProductLookupResultDto
            {
                Status = ProductLookupStatus.Found,
                Barcode = external.Barcode,
                Name = external.Name,
                Brand = external.Brand,
                ImageUrl = external.ImageUrl
            };
        }
        catch (ExternalCatalogRateLimitException)
        {
            return new ProductLookupResultDto
            {
                Status = ProductLookupStatus.RateLimited,
                Barcode = input.Barcode
            };
        }
        catch (ExternalCatalogUnavailableException)
        {
            return new ProductLookupResultDto
            {
                Status = ProductLookupStatus.ServiceUnavailable,
                Barcode = input.Barcode
            };
        }
    }
}