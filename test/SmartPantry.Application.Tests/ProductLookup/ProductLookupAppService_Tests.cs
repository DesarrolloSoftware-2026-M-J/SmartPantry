using System.Threading.Tasks;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace SmartPantry.ProductLookup;

public class ProductLookupAppService_Tests
{
    private readonly IExternalProductCatalogClient _catalog;
    private readonly ProductLookupAppService _appService;

    public ProductLookupAppService_Tests()
    {
        _catalog = Substitute.For<IExternalProductCatalogClient>();
        _appService = new ProductLookupAppService(_catalog);
    }

    [Fact]
    public async Task Debe_devolver_Found_con_los_datos_cuando_el_producto_existe()
    {
        _catalog.GetByBarcodeAsync("3017620422003").Returns(
            Task.FromResult<ExternalProductDto?>(new ExternalProductDto
            {
                Barcode = "3017620422003",
                Name = "Nutella",
                Brand = "Nutella, Ferrero",
                ImageUrl = "https://example.com/nutella.jpg"
            }));

        var result = await _appService.GetByBarcodeAsync(
            new BarcodeLookupInputDto { Barcode = "3017620422003" });

        Assert.Equal(ProductLookupStatus.Found, result.Status);
        Assert.Equal("3017620422003", result.Barcode);
        Assert.Equal("Nutella", result.Name);
        Assert.Equal("Nutella, Ferrero", result.Brand);
        Assert.Equal("https://example.com/nutella.jpg", result.ImageUrl);
    }

    [Fact]
    public async Task Debe_devolver_NotFound_cuando_el_producto_no_existe()
    {
        _catalog.GetByBarcodeAsync("0000000000000").Returns(
            Task.FromResult<ExternalProductDto?>(null));

        var result = await _appService.GetByBarcodeAsync(
            new BarcodeLookupInputDto { Barcode = "0000000000000" });

        Assert.Equal(ProductLookupStatus.NotFound, result.Status);
        Assert.Equal("0000000000000", result.Barcode);
        Assert.Null(result.Name);
        Assert.Null(result.Brand);
        Assert.Null(result.ImageUrl);
    }

    [Fact]
    public async Task Debe_mantener_en_null_los_datos_que_el_proveedor_no_informa()
    {
        _catalog.GetByBarcodeAsync("7790000000017").Returns(
            Task.FromResult<ExternalProductDto?>(new ExternalProductDto
            {
                Barcode = "7790000000017",
                Name = null,
                Brand = null,
                ImageUrl = null
            }));

        var result = await _appService.GetByBarcodeAsync(
            new BarcodeLookupInputDto { Barcode = "7790000000017" });

        Assert.Equal(ProductLookupStatus.Found, result.Status);
        Assert.Null(result.Name);
        Assert.Null(result.Brand);
        Assert.Null(result.ImageUrl);
    }

    [Fact]
    public async Task Debe_devolver_RateLimited_cuando_se_supera_el_limite_de_consultas()
    {
        _catalog.GetByBarcodeAsync(Arg.Any<string>())
            .ThrowsAsync(new ExternalCatalogRateLimitException());

        var result = await _appService.GetByBarcodeAsync(
            new BarcodeLookupInputDto { Barcode = "3017620422003" });

        Assert.Equal(ProductLookupStatus.RateLimited, result.Status);
        Assert.Equal("3017620422003", result.Barcode);
    }

    [Fact]
    public async Task Debe_devolver_ServiceUnavailable_cuando_el_proveedor_no_esta_disponible()
    {
        _catalog.GetByBarcodeAsync(Arg.Any<string>())
            .ThrowsAsync(new ExternalCatalogUnavailableException("Proveedor caído"));

        var result = await _appService.GetByBarcodeAsync(
            new BarcodeLookupInputDto { Barcode = "3017620422003" });

        Assert.Equal(ProductLookupStatus.ServiceUnavailable, result.Status);
        Assert.Equal("3017620422003", result.Barcode);
    }
}