using System;
using System.Threading.Tasks;
using Shouldly;
using Volo.Abp.Modularity;
using Volo.Abp.Validation;
using Xunit;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Entities;

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

    [Fact]
    public async Task Should_Register_List_Update_Get_And_Delete_Product()
    {
        var created = await _productAppService.CreateAsync(new CreateProductDto
        {
            Name = "Yerba Mate",
            Brand = "Taragüí",
            Barcode = "7790000000001"
        });

        var list = await _productAppService.GetListAsync(new PagedAndSortedResultRequestDto
        {
            MaxResultCount = 1000,
            Sorting = "Name"
        });
        list.TotalCount.ShouldBeGreaterThan(0);
        list.Items.ShouldContain(p => p.Id == created.Id);

        var updated = await _productAppService.UpdateAsync(created.Id, new UpdateProductDto
        {
            Name = "  Yerba Mate Suave  ",
            Brand = "Taragüí",
            Barcode = "7790000000001"
        });
        updated.Name.ShouldBe("Yerba Mate Suave");

        var fetched = await _productAppService.GetAsync(created.Id);
        fetched.Name.ShouldBe("Yerba Mate Suave");

        await _productAppService.DeleteAsync(created.Id);

        await Assert.ThrowsAnyAsync<EntityNotFoundException>(async () =>
        {
            await _productAppService.GetAsync(created.Id);
        });
    }

}