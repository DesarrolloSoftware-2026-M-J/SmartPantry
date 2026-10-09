using System;
using Shouldly;
using Xunit;

namespace SmartPantry.Products;
public class ProductDomainTests
{
    [Fact]
    public void Should_Create_Valid_Product_And_Normalize_Texts()
    {
        var id = Guid.NewGuid();
        var name = "  Galletitas   ";
        var brand = " Serranitas ";
        var barcode = " 123456 ";

        var product = new Product(id, name, brand, barcode);

        product.Name.ShouldBe("Galletitas");
        product.Brand.ShouldBe("Serranitas");
        product.Barcode.ShouldBe("123456");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Should_Reject_Empty_Name(string invalidName)
    {
        var id = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() =>
        {
            new Product(id, invalidName, "Serranitas", "123456");
        });
    }

    [Fact]
    public void Should_Update_Product_And_Keep_Normalization()
    {
        var product = new Product(Guid.NewGuid(), "Galletitas", "Serranitas", "123456");

        product.Update("  Galletitas de agua  ", " Terrabusi ", " 654321 ");

        product.Name.ShouldBe("Galletitas de agua");
        product.Brand.ShouldBe("Terrabusi");
        product.Barcode.ShouldBe("654321");
    }

    [Fact]
    public void Should_Reject_Invalid_Update_And_Keep_Previous_State()
    {
        var product = new Product(Guid.NewGuid(), "Galletitas", "Serranitas", "123456");

        Assert.Throws<ArgumentException>(() =>
        {
            product.Update("Nombre nuevo", "   ", "654321");
        });

        product.Name.ShouldBe("Galletitas");
        product.Brand.ShouldBe("Serranitas");
        product.Barcode.ShouldBe("123456");
    }

}