using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;
using SmartPantry.Products;

namespace SmartPantry;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class ProductMapper : MapperBase<Product, ProductDto>
{
    public override partial ProductDto Map(Product source);

    public override partial void Map(Product source, ProductDto destination);
}
