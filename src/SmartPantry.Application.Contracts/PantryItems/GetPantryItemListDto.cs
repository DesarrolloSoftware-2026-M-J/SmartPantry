using Volo.Abp.Application.Dtos;

namespace SmartPantry.PantryItems;

public class GetPantryItemListDto : PagedAndSortedResultRequestDto
{
    public PantryItemStatus? Status { get; set; }
}