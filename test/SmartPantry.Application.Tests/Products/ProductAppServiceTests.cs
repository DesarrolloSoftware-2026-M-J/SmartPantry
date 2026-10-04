using System;
using System.Threading.Tasks;
using Shouldly;
using Volo.Abp.Modularity;
using Volo.Abp.Validation;
using Xunit;

namespace SmartPantry.Products;
public abstract class ProductAppServiceTests<TModule> : SmartPantryApplicationTestBase<TModule>
    where TModule : IAbpModule
{
    private readonly IProductAppService _productAppService;

    protected ProductAppServiceTests()
    {
        _productAppService = GetRequiredService<IProductAppService>();
    }

    [Fact]
    public async Task Should_Create_And_Get_Product()
    {
        var input = new CreateProductDto
        {
            Name = "Fideos Tirabuzón",
            Brand = "Matarazzo",
            Barcode = "7791234567891"
        };

        var createdProduct = await _productAppService.CreateAsync(input);

        createdProduct.ShouldNotBeNull();
        createdProduct.Id.ShouldNotBe(Guid.Empty);
        createdProduct.Name.ShouldBe(input.Name);

        var fetchedProduct = await _productAppService.GetAsync(createdProduct.Id);

        fetchedProduct.ShouldNotBeNull();
        fetchedProduct.Id.ShouldBe(createdProduct.Id);
        fetchedProduct.Name.ShouldBe(input.Name);
    }

    [Fact]
    public async Task Should_Not_Allow_Create_With_Empty_Name()
    {
        var invalidInput = new CreateProductDto
        {
            Name = "",
            Brand = "Matarazzo",
            Barcode = "7791234567891"
        };

        await Assert.ThrowsAsync<AbpValidationException>(async () =>
        {
            await _productAppService.CreateAsync(invalidInput);
        });
    }
}