using Toka.Domain.Common;
using Toka.UnitTests.Fakes;

namespace Toka.UnitTests.Domain;

public class ProductTests
{
    [Fact]
    public void Reserve_decrements_stock_and_bumps_version()
    {
        var product = TestData.Product(stock: 5);

        product.Reserve(2);

        Assert.Equal(3, product.Stock);
        Assert.Equal(1, product.Version);
    }

    [Fact]
    public void Reserve_more_than_available_fails_and_keeps_stock()
    {
        var product = TestData.Product(stock: 2);

        var ex = Assert.Throws<DomainException>(() => product.Reserve(3));

        Assert.Equal("insufficient_stock", ex.Code);
        Assert.Contains("Solo hay 2 unidad(es)", ex.Message);
        Assert.Equal(2, product.Stock);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Reserve_requires_positive_quantity(int quantity) =>
        Assert.Throws<DomainException>(() => TestData.Product().Reserve(quantity));

    [Fact]
    public void Release_returns_stock()
    {
        var product = TestData.Product(stock: 5);
        product.Reserve(2);

        product.Release(2);

        Assert.Equal(5, product.Stock);
    }
}
