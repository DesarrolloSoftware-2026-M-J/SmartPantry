using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace SmartPantry.Products;

public class ProductAppService :
    CrudAppService<
        Product,                         
        ProductDto,                      
        Guid,                            
        PagedAndSortedResultRequestDto,  
        CreateProductDto,                
        UpdateProductDto>,               
    IProductAppService
{
    public ProductAppService(IRepository<Product, Guid> repository)
        : base(repository)
    {
    }
    public override async Task<ProductDto> CreateAsync(CreateProductDto input)
    {
        var product = new Product(
            GuidGenerator.Create(),
            input.Name,
            input.Brand,
            input.Barcode
        );

        await Repository.InsertAsync(product);

        return MapToGetOutputDto(product);
    }

    // UpdateAsync: se adapta para que las reglas las aplique el dominio
    public override async Task<ProductDto> UpdateAsync(Guid id, UpdateProductDto input)
    {
        var product = await Repository.GetAsync(id);

        product.Update(input.Name, input.Brand, input.Barcode);

        await Repository.UpdateAsync(product);

        return MapToGetOutputDto(product);
    }

    // Mapeo entidad -> DTO (se usa en Get, GetList, Create y Update)
    protected override ProductDto MapToGetOutputDto(Product entity)
    {
        return ToDto(entity);
    }

    protected override ProductDto MapToGetListOutputDto(Product entity)
    {
        return ToDto(entity);
    }

    private static ProductDto ToDto(Product product)
    {
        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Brand = product.Brand,
            Barcode = product.Barcode
        };
    }
}