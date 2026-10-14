using System.Threading.Tasks;

namespace SmartPantry.ProductLookup;

public interface IExternalProductCatalogClient
{    Task<ExternalProductDto?> GetByBarcodeAsync(string barcode);
}