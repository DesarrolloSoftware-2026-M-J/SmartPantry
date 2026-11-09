using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Shouldly;
using SmartPantry.Products;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Modularity;
using Volo.Abp.Security.Claims;
using Xunit;

namespace SmartPantry.PantryItems;

public abstract class PantryItemAppServiceTests<TModule> : SmartPantryApplicationTestBase<TModule>
    where TModule : IAbpModule
{
    private readonly IPantryItemAppService _pantryItemAppService;
    private readonly IProductAppService _productAppService;
    private readonly ICurrentPrincipalAccessor _currentPrincipalAccessor;

    protected PantryItemAppServiceTests()
    {
        _pantryItemAppService = GetRequiredService<IPantryItemAppService>();
        _productAppService = GetRequiredService<IProductAppService>();
        _currentPrincipalAccessor = GetRequiredService<ICurrentPrincipalAccessor>();
    }

    private async Task<ProductDto> CreateSampleProductAsync(string barcode = "7791234567890")
    {
        return await _productAppService.CreateAsync(new CreateProductDto
        {
            Name = "Leche Entera",
            Brand = "La Serenísima",
            Barcode = barcode
        });
    }

    [Fact]
    public async Task Should_Create_And_Get_PantryItem()
    {
        var product = await CreateSampleProductAsync("7790001");

        var created = await _pantryItemAppService.CreateAsync(new CreatePantryItemDto
        {
            ProductId = product.Id,
            ExpirationDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            Quantity = 2m,
            Unit = "litros"
        });

        created.ShouldNotBeNull();
        created.ProductName.ShouldBe(product.Name);
        created.Quantity.ShouldBe(2m);
        created.Unit.ShouldBe("litros");
        created.Status.ShouldBe(PantryItemStatus.Active);

        var fetched = await _pantryItemAppService.GetAsync(created.Id);
        fetched.ShouldNotBeNull();
        fetched.Id.ShouldBe(created.Id);
        fetched.ProductName.ShouldBe(product.Name);
    }

    [Fact]
    public async Task Should_Update_Details()
    {
        var product = await CreateSampleProductAsync("7790002");
        var item = await _pantryItemAppService.CreateAsync(new CreatePantryItemDto
        {
            ProductId = product.Id,
            Quantity = 1,
            Unit = "paquete"
        });

        var newDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));
        var updated = await _pantryItemAppService.UpdateAsync(item.Id, new UpdatePantryItemDto
        {
            Quantity = 5m,
            Unit = "cajas",
            ExpirationDate = newDate
        });

        updated.Quantity.ShouldBe(5m);
        updated.Unit.ShouldBe("cajas");
        updated.ExpirationDate.ShouldBe(newDate);
    }

    [Fact]
    public async Task Should_Mark_As_Consumed()
    {
        var product = await CreateSampleProductAsync("7790003");
        var item = await _pantryItemAppService.CreateAsync(new CreatePantryItemDto
        {
            ProductId = product.Id
        });

        var consumed = await _pantryItemAppService.MarkAsConsumedAsync(item.Id);
        consumed.Status.ShouldBe(PantryItemStatus.Consumed);

        var fetched = await _pantryItemAppService.GetAsync(item.Id);
        fetched.Status.ShouldBe(PantryItemStatus.Consumed);
    }

    [Fact]
    public async Task Should_Delete_PantryItem()
    {
        var product = await CreateSampleProductAsync("7790004");
        var item = await _pantryItemAppService.CreateAsync(new CreatePantryItemDto
        {
            ProductId = product.Id
        });

        await _pantryItemAppService.DeleteAsync(item.Id);

        await Assert.ThrowsAsync<EntityNotFoundException>(async () =>
        {
            await _pantryItemAppService.GetAsync(item.Id);
        });
    }

    [Fact]
    public async Task Should_Not_Allow_Access_Or_Modify_Another_User_Item()
    {
        var product = await CreateSampleProductAsync("7790005");
        var item = await _pantryItemAppService.CreateAsync(new CreatePantryItemDto
        {
            ProductId = product.Id,
            Quantity = 3m
        });

        var otherUserId = Guid.NewGuid();
        var otherPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(AbpClaimTypes.UserId, otherUserId.ToString()),
            new Claim(AbpClaimTypes.UserName, "other_user")
        }));

        using (_currentPrincipalAccessor.Change(otherPrincipal))
        {
            await Assert.ThrowsAsync<EntityNotFoundException>(async () =>
            {
                await _pantryItemAppService.GetAsync(item.Id);
            });

            await Assert.ThrowsAsync<EntityNotFoundException>(async () =>
            {
                await _pantryItemAppService.UpdateAsync(item.Id, new UpdatePantryItemDto { Quantity = 99 });
            });

            await Assert.ThrowsAsync<EntityNotFoundException>(async () =>
            {
                await _pantryItemAppService.MarkAsConsumedAsync(item.Id);
            });

            await Assert.ThrowsAsync<EntityNotFoundException>(async () =>
            {
                await _pantryItemAppService.DeleteAsync(item.Id);
            });

            var list = await _pantryItemAppService.GetListAsync(new GetPantryItemListDto());
            list.Items.ShouldNotContain(x => x.Id == item.Id);
        }
    }
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Should_Reject_Zero_Or_Negative_Quantity(int quantity)
    {
        var product = await CreateSampleProductAsync("7790006");

        await Assert.ThrowsAsync<Volo.Abp.Validation.AbpValidationException>(async () =>
        {
            await _pantryItemAppService.CreateAsync(new CreatePantryItemDto
            {
                ProductId = product.Id,
                Quantity = quantity
            });
        });
    }

    [Fact]
    public async Task Should_List_Only_Current_User_Items_With_Product_Name()
    {
        var product = await CreateSampleProductAsync("7790007");
        await _pantryItemAppService.CreateAsync(new CreatePantryItemDto
        {
            ProductId = product.Id,
            Quantity = 1m
        });

        var list = await _pantryItemAppService.GetListAsync(new GetPantryItemListDto());

        list.TotalCount.ShouldBeGreaterThan(0);
        list.Items.ShouldAllBe(x => x.ProductName == product.Name);
    }
}