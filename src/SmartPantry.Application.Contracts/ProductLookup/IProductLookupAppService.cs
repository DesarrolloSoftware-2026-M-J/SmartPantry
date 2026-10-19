using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace SmartPantry.ProductLookup;

public interface IProductLookupAppService : IApplicationService
{
    Task<ProductLookupResultDto> GetByBarcodeAsync(BarcodeLookupInputDto input);
}