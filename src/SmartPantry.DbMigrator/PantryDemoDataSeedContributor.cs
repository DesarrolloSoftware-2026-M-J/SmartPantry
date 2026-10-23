using System;
using System.Threading.Tasks;
using SmartPantry.PantryItems;
using SmartPantry.Products;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Uow;

namespace SmartPantry.DbMigrator;

/// <summary>
/// Datos locales de demostración para RF-15. Solo se ejecuta con el DbMigrator, no en la suite de pruebas.
/// Las fechas son relativas a "hoy" en UTC, el mismo criterio que usa el worker.
/// </summary>
public class PantryDemoDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private static readonly Guid OwnerA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OwnerB = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly IRepository<Product, Guid> _products;
    private readonly IRepository<PantryItem, Guid> _items;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IUnitOfWorkManager _uowManager;

    public PantryDemoDataSeedContributor(
        IRepository<Product, Guid> products,
        IRepository<PantryItem, Guid> items,
        IGuidGenerator guidGenerator,
        IUnitOfWorkManager uowManager)
    {
        _products = products;
        _items = items;
        _guidGenerator = guidGenerator;
        _uowManager = uowManager;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        using var uow = _uowManager.Begin(requiresNew: true, isTransactional: true);

        if (await _items.AnyAsync(x => x.OwnerId == OwnerA || x.OwnerId == OwnerB))
        {
            await uow.CompleteAsync();
            return;
        }

        var yogur = await GetOrCreateProductAsync("Yogur natural", "La Serenísima", "9990000000001");
        var leche = await GetOrCreateProductAsync("Leche entera", "SanCor", "9990000000002");
        var pan = await GetOrCreateProductAsync("Pan lactal", "Fargo", "9990000000003");
        var arroz = await GetOrCreateProductAsync("Arroz largo fino", "Gallo", "9990000000004");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await AddItemAsync(OwnerA, yogur, today.AddDays(10));  
        await AddItemAsync(OwnerA, leche, today.AddDays(3));   
        await AddItemAsync(OwnerA, pan, today.AddDays(-2));    
        await AddItemAsync(OwnerA, arroz, null);              

        await AddItemAsync(OwnerB, yogur, today.AddDays(4));  
        await AddItemAsync(OwnerB, leche, today);              
        var consumido = await AddItemAsync(OwnerB, pan, today.AddDays(1));
        consumido.MarkAsConsumed();                           
        await _items.UpdateAsync(consumido);

        await uow.CompleteAsync();
    }

    private async Task<Product> GetOrCreateProductAsync(string name, string brand, string barcode)
    {
        var product = await _products.FindAsync(x => x.Barcode == barcode);
        if (product != null)
        {
            return product;
        }

        return await _products.InsertAsync(new Product(_guidGenerator.Create(), name, brand, barcode));
    }

    private Task<PantryItem> AddItemAsync(Guid ownerId, Product product, DateOnly? expiration)
    {
        return _items.InsertAsync(new PantryItem(_guidGenerator.Create(), ownerId, product.Id, expiration));
    }
}