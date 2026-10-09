using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities;

namespace SmartPantry.Products;

public class Product : AggregateRoot<Guid>
{
    public string Name { get; private set; }
    public string Brand { get; private set; }
    public string Barcode { get; private set; }

    private Product()
    {
        Name = null!;
        Brand = null!;
        Barcode = null!;
    }

    public Product(Guid id, string name, string brand, string barcode) : base(id)
    {
        Name = string.Empty;
        Brand = string.Empty;
        Barcode = string.Empty;

        SetName(name);
        SetBrand(brand);
        SetBarcode(barcode);
    }

    public void SetName(string name)
    {
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), maxLength: ProductConsts.MaxNameLength).Trim();
    }

    public void SetBrand(string brand)
    {
        Brand = Check.NotNullOrWhiteSpace(brand, nameof(brand), maxLength: ProductConsts.MaxBrandLength).Trim();
    }

    public void SetBarcode(string barcode)
    {
        Barcode = Check.NotNullOrWhiteSpace(barcode, nameof(barcode), maxLength: ProductConsts.MaxBarcodeLength).Trim();
    }

    public void Update(string name, string brand, string barcode)
    {
        var newName = Check.NotNullOrWhiteSpace(name, nameof(name), maxLength: ProductConsts.MaxNameLength).Trim();
        var newBrand = Check.NotNullOrWhiteSpace(brand, nameof(brand), maxLength: ProductConsts.MaxBrandLength).Trim();
        var newBarcode = Check.NotNullOrWhiteSpace(barcode, nameof(barcode), maxLength: ProductConsts.MaxBarcodeLength).Trim();

        Name = newName;
        Brand = newBrand;
        Barcode = newBarcode;
    }

}