using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartPantry.PantryItems;
using SmartPantry.Products;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Modularity;
using Volo.Abp.Uow;
using Xunit;

namespace SmartPantry.ExpirationWarnings;

/// <summary>
/// Criterio de fecha: "hoy" es un DateOnly que representa el día UTC. El worker lo calcula con
/// DateOnly.FromDateTime(DateTime.UtcNow); acá se fija una fecha para no depender del reloj.
/// Umbral: 3 días, inclusive. Ver wiki, RF-15.
/// </summary>
public abstract class ExpirationWarningProcessor_Tests<TStartupModule>
    : SmartPantryApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private static readonly DateOnly Today = new(2026, 10, 21);
    private static readonly Guid Owner = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private readonly ExpirationWarningProcessor _processor;
    private readonly IRepository<Product, Guid> _products;
    private readonly IRepository<PantryItem, Guid> _items;
    private readonly IRepository<ExpirationWarning, Guid> _warnings;
    private readonly IUnitOfWorkManager _uowManager;
    private int _barcodeCounter;

    protected ExpirationWarningProcessor_Tests()
    {
        _processor = GetRequiredService<ExpirationWarningProcessor>();
        _products = GetRequiredService<IRepository<Product, Guid>>();
        _items = GetRequiredService<IRepository<PantryItem, Guid>>();
        _warnings = GetRequiredService<IRepository<ExpirationWarning, Guid>>();
        _uowManager = GetRequiredService<IUnitOfWorkManager>();
    }


    [Theory]
    [InlineData(10, false)]  
    [InlineData(4, false)]   
    [InlineData(3, true)]    
    [InlineData(1, true)]    
    [InlineData(0, true)]   
    [InlineData(-1, true)]   
    [InlineData(-30, true)]  
    public async Task Debe_avisar_solo_si_el_vencimiento_esta_dentro_del_umbral_o_ya_paso(
        int daysRemaining, bool expectedWarning)
    {
        await AddItemAsync(Today.AddDays(daysRemaining));

        var result = await _processor.ProcessAsync(Today);

        var warnings = await GetWarningsAsync();
        Assert.Equal(expectedWarning ? 1 : 0, warnings.Count);
        Assert.Equal(expectedWarning ? 1 : 0, result.Created);
        Assert.Equal(0, result.Failed);
        Assert.All(warnings, w => Assert.True(w.IsActive));
    }


    [Fact]
    public async Task No_debe_avisar_si_el_item_no_tiene_fecha_de_vencimiento()
    {
        await AddItemAsync(null);

        var result = await _processor.ProcessAsync(Today);

        Assert.Empty(await GetWarningsAsync());
        Assert.Equal(0, result.Created);
    }

    [Fact]
    public async Task No_debe_avisar_si_el_item_esta_consumido()
    {
        await AddItemAsync(Today.AddDays(1), consumed: true);

        var result = await _processor.ProcessAsync(Today);

        Assert.Empty(await GetWarningsAsync());
        Assert.Equal(0, result.Created);
    }

  

    [Fact]
    public async Task Debe_dejar_una_sola_advertencia_por_item_al_ejecutar_dos_veces()
    {
        await AddItemAsync(Today.AddDays(2));

        var first = await _processor.ProcessAsync(Today);
        var second = await _processor.ProcessAsync(Today);

        Assert.Single(await GetWarningsAsync());
        Assert.Equal(1, first.Created);
        Assert.Equal(0, second.Created);
        Assert.Equal(0, second.Updated);
        Assert.Equal(0, second.Deactivated);
    }

    [Fact]
    public async Task Debe_resolver_una_mezcla_de_casos_y_mantener_el_resultado_al_repetir()
    {
        await AddItemAsync(Today.AddDays(10));               
        await AddItemAsync(Today.AddDays(3));                 
        await AddItemAsync(Today);                            
        await AddItemAsync(Today.AddDays(-5));               
        await AddItemAsync(null);                             
        await AddItemAsync(Today.AddDays(1), consumed: true); 

        var first = await _processor.ProcessAsync(Today);
        await _processor.ProcessAsync(Today);

        Assert.Equal(3, first.Created);
        Assert.Equal(3, (await GetWarningsAsync()).Count);
    }

 

    [Fact]
    public async Task Debe_actualizar_la_misma_advertencia_si_cambia_la_fecha_dentro_del_umbral()
    {
        var itemId = await AddItemAsync(Today.AddDays(1));
        await _processor.ProcessAsync(Today);
        var original = Assert.Single(await GetWarningsAsync());

        await UpdateItemAsync(itemId, i => i.SetExpirationDate(Today.AddDays(2)));
        var result = await _processor.ProcessAsync(Today);

        var updated = Assert.Single(await GetWarningsAsync());
        Assert.Equal(original.Id, updated.Id);
        Assert.Equal(Today.AddDays(2), updated.ExpirationDate);
        Assert.True(updated.IsActive);
        Assert.Equal(1, result.Updated);
        Assert.Equal(0, result.Created);
    }

    [Fact]
    public async Task Debe_desactivar_la_advertencia_si_la_fecha_sale_del_umbral()
    {
        var itemId = await AddItemAsync(Today.AddDays(2));
        await _processor.ProcessAsync(Today);
        var original = Assert.Single(await GetWarningsAsync());

        await UpdateItemAsync(itemId, i => i.SetExpirationDate(Today.AddDays(10)));
        var result = await _processor.ProcessAsync(Today);

        var warning = Assert.Single(await GetWarningsAsync());
        Assert.Equal(original.Id, warning.Id);
        Assert.False(warning.IsActive);
        Assert.Equal(1, result.Deactivated);
    }

    [Fact]
    public async Task Debe_reactivar_la_misma_advertencia_si_la_fecha_vuelve_al_umbral()
    {
        var itemId = await AddItemAsync(Today.AddDays(2));
        await _processor.ProcessAsync(Today);
        var original = Assert.Single(await GetWarningsAsync());

        await UpdateItemAsync(itemId, i => i.SetExpirationDate(Today.AddDays(10)));
        await _processor.ProcessAsync(Today);
        Assert.False(Assert.Single(await GetWarningsAsync()).IsActive);

        await UpdateItemAsync(itemId, i => i.SetExpirationDate(Today.AddDays(1)));
        var result = await _processor.ProcessAsync(Today);

        var reactivated = Assert.Single(await GetWarningsAsync());
        Assert.Equal(original.Id, reactivated.Id);
        Assert.True(reactivated.IsActive);
        Assert.Equal(Today.AddDays(1), reactivated.ExpirationDate);
        Assert.Equal(1, result.Updated);
        Assert.Equal(0, result.Created);
    }

    [Fact]
    public async Task Debe_desactivar_la_advertencia_si_el_item_se_consume()
    {
        var itemId = await AddItemAsync(Today.AddDays(2));
        await _processor.ProcessAsync(Today);
        Assert.True(Assert.Single(await GetWarningsAsync()).IsActive);

        await UpdateItemAsync(itemId, i => i.MarkAsConsumed());
        var result = await _processor.ProcessAsync(Today);

        Assert.False(Assert.Single(await GetWarningsAsync()).IsActive);
        Assert.Equal(1, result.Deactivated);
    }


    [Fact]
    public async Task La_base_debe_impedir_dos_advertencias_del_mismo_tipo_para_un_item()
    {
        var itemId = await AddItemAsync(Today.AddDays(1));
        await InsertWarningAsync(itemId);

        await Assert.ThrowsAnyAsync<Exception>(() => InsertWarningAsync(itemId));

        Assert.Single(await GetWarningsAsync());
    }


    private async Task<T> InUowAsync<T>(Func<Task<T>> action)
    {
        using var uow = _uowManager.Begin(requiresNew: true, isTransactional: true);
        var result = await action();
        await uow.CompleteAsync();
        return result;
    }

    private Task<Guid> AddItemAsync(DateOnly? expiration, bool consumed = false)
    {
        return InUowAsync(async () =>
        {
            _barcodeCounter++;
            var product = await _products.InsertAsync(new Product(
                Guid.NewGuid(), "Producto de prueba", "Marca de prueba", $"999000000{_barcodeCounter:D4}"));

            var item = new PantryItem(Guid.NewGuid(), Owner, product.Id, expiration);
            if (consumed)
            {
                item.MarkAsConsumed();
            }

            await _items.InsertAsync(item);
            return item.Id;
        });
    }

    private Task<bool> UpdateItemAsync(Guid itemId, Action<PantryItem> change)
    {
        return InUowAsync(async () =>
        {
            var item = await _items.GetAsync(itemId);
            change(item);
            await _items.UpdateAsync(item);
            return true;
        });
    }

    private Task<bool> InsertWarningAsync(Guid itemId)
    {
        return InUowAsync(async () =>
        {
            await _warnings.InsertAsync(new ExpirationWarning(
                Guid.NewGuid(), itemId, ExpirationWarningType.Expiration, Today.AddDays(1)));
            return true;
        });
    }

    private Task<List<ExpirationWarning>> GetWarningsAsync()
    {
        return InUowAsync(() => _warnings.GetListAsync());
    }
}