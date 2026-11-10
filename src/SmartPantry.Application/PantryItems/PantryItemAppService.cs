using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using SmartPantry.Products;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;

namespace SmartPantry.PantryItems;

[Authorize]
public class PantryItemAppService : SmartPantryAppService, IPantryItemAppService
{
    private readonly IRepository<PantryItem, Guid> _pantryItemRepository;
    private readonly IRepository<Product, Guid> _productRepository;

    public PantryItemAppService(
        IRepository<PantryItem, Guid> pantryItemRepository,
        IRepository<Product, Guid> productRepository)
    {
        _pantryItemRepository = pantryItemRepository;
        _productRepository = productRepository;
    }

    public async Task<PagedResultDto<PantryItemDto>> GetListAsync(GetPantryItemListDto input)
    {
        var currentUserId = CurrentUser.GetId();

        var itemQueryable = await _pantryItemRepository.GetQueryableAsync();
        var productQueryable = await _productRepository.GetQueryableAsync();

        var baseQuery = from item in itemQueryable
                        where item.OwnerId == currentUserId
                        where !input.Status.HasValue || item.Status == input.Status.Value
                        join product in productQueryable on item.ProductId equals product.Id
                        select new { item, product };

        var totalCount = await AsyncExecuter.CountAsync(baseQuery);

        var query = baseQuery
            .OrderBy(x => x.item.ExpirationDate == null)
            .ThenBy(x => x.item.ExpirationDate)
            .ThenBy(x => x.item.Id)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount);

        var resultList = await AsyncExecuter.ToListAsync(query);

        var dtos = resultList.Select(x => MapToDto(x.item, x.product)).ToList();

        return new PagedResultDto<PantryItemDto>(totalCount, dtos);
    }

    public async Task<PantryItemDto> GetAsync(Guid id)
    {
        var item = await GetOwnedItemAsync(id);
        var product = await _productRepository.GetAsync(item.ProductId);

        return MapToDto(item, product);
    }

    public async Task<PantryItemDto> CreateAsync(CreatePantryItemDto input)
    {
        var currentUserId = CurrentUser.GetId();

        var product = await _productRepository.GetAsync(input.ProductId);

        var pantryItem = new PantryItem(
            GuidGenerator.Create(),
            currentUserId,
            input.ProductId,
            input.ExpirationDate,
            input.Quantity,
            input.Unit
        );

        await _pantryItemRepository.InsertAsync(pantryItem);

        return MapToDto(pantryItem, product);
    }

    public async Task<PantryItemDto> UpdateAsync(Guid id, UpdatePantryItemDto input)
    {
        var item = await GetOwnedItemAsync(id);

        item.UpdateDetails(input.Quantity, input.Unit, input.ExpirationDate);
        await _pantryItemRepository.UpdateAsync(item);

        var product = await _productRepository.GetAsync(item.ProductId);
        return MapToDto(item, product);
    }

    public async Task<PantryItemDto> MarkAsConsumedAsync(Guid id)
    {
        var item = await GetOwnedItemAsync(id);

        item.MarkAsConsumed();
        await _pantryItemRepository.UpdateAsync(item);

        var product = await _productRepository.GetAsync(item.ProductId);
        return MapToDto(item, product);
    }

    public async Task DeleteAsync(Guid id)
    {
        var item = await GetOwnedItemAsync(id);
        await _pantryItemRepository.DeleteAsync(item);
    }

    
    private async Task<PantryItem> GetOwnedItemAsync(Guid id)
    {
        var item = await _pantryItemRepository.FindAsync(id);
        var currentUserId = CurrentUser.GetId();

        if (item == null || item.OwnerId != currentUserId)
        {
            throw new EntityNotFoundException(typeof(PantryItem), id);
        }

        return item;
    }

    private static PantryItemDto MapToDto(PantryItem item, Product product)
    {
        return new PantryItemDto
        {
            Id = item.Id,
            OwnerId = item.OwnerId,
            ProductId = item.ProductId,
            ProductName = product.Name,
            ProductBrand = product.Brand,
            Status = item.Status,
            ExpirationDate = item.ExpirationDate,
            Quantity = item.Quantity,
            Unit = item.Unit
        };
    }
}