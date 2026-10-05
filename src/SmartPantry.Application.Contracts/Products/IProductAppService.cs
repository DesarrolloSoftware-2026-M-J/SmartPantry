using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace SmartPantry.Products;

public interface IProductAppService : IApplicationService
{
    Task<ProductDto> GetAsync(Guid id);
    Task<ProductDto> CreateAsync(CreateProductDto input);
}
