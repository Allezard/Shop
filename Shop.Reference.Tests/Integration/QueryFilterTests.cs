using Microsoft.EntityFrameworkCore;
using Shop.Reference.Domain;

namespace Shop.Reference.Tests.Integration;

[Collection(DatabaseCollection.Name)]
public sealed class QueryFilterTests(DatabaseFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await fixture.ResetAsync();
    public async ValueTask DisposeAsync() => await Task.CompletedTask;

    [Fact]
    public async Task Global_filter_hides_deleted_products()
    {
        await using var ctx = fixture.CreateContext();
        var category = new Category { Name = "Тестовая категория" };
        ctx.Categories.Add(category);

        ctx.Products.Add(new Product
        {
            Sku = "VISIBLE", Name = "Видимый", Description = "", Price = 1m, Category = category
        });

        ctx.Products.Add(new Product
        {
            Sku = "HIDDEN", Name = "Скрытый", Description = "", Price = 1m,
            Category = category, IsDeleted = true
        });

        await ctx.SaveChangesAsync();

        var visible = await ctx.Products.ToListAsync();

        Assert.Single(visible);
        Assert.Equal("VISIBLE", visible[0].Sku);

        // IgnoreQueryFilters — для отчётов, которым нужны и удалённые.
        // См. модуль 03, раздел 3.6.
        var all = await ctx.Products.IgnoreQueryFilters().ToListAsync();

        Assert.Equal(2, all.Count);
    }
}