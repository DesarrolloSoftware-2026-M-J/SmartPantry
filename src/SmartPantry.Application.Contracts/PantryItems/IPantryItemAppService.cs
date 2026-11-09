using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace SmartPantry.PantryItems;

public interface IPantryItemAppService : IApplicationService
{
    Task<PagedResultDto<PantryItemDto>> GetListAsync(GetPantryItemListDto input);
    Task<PantryItemDto> GetAsync(Guid id);
    Task<PantryItemDto> CreateAsync(CreatePantryItemDto input);
    Task<PantryItemDto> UpdateAsync(Guid id, UpdatePantryItemDto input);
    Task<PantryItemDto> MarkAsConsumedAsync(Guid id);
    Task DeleteAsync(Guid id);
}