using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Product.Applcaton.Products.Commands;
using Product.Doman.Enums;
using Product.Infrastructure.Cache;
using Product.Infrastructure.Messagng;
using Product.Infrastructure.Persstence;

public class ProductCreateAndCacheTests
{
    private class MemoryDistributedCacheAdapter : IDistributedCache
    {
        private readonly MemoryDistributedCache _cache = new(new MemoryDistributedCacheOptions());
        public byte[]? Get(string key) => _cache.Get(key);
        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => _cache.GetAsync(key, token);
        public void Refresh(string key) => _cache.Refresh(key);
        public Task RefreshAsync(string key, CancellationToken token = default) => _cache.RefreshAsync(key, token);
        public void Remove(string key) => _cache.Remove(key);
        public Task RemoveAsync(string key, CancellationToken token = default) => _cache.RemoveAsync(key, token);
        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => _cache.Set(key, value, options);
        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) => _cache.SetAsync(key, value, options, token);
    }

    private class FakePublisher : BuldngBlocks.Messagng.IMessagePublisher
    {
        public List<(string ex, string key, object msg)> Sent { get; } = new();
        public Task PublishAsync(string exchange, string routingKey, object message, CancellationToken ct = default)
        {
            Sent.Add((exchange, routingKey, message));
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task CreateProduct_SetsPending_ThenSagaMarksActive_AndInvalidatesCaches()
    {
        var options = new DbContextOptionsBuilder<ProductDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new ProductDbContext(options);

        var cache = new RedsCacheServce(new MemoryDistributedCacheAdapter());
        await cache.SetAsync("products:all", new[] { new { x = 1 } }, TimeSpan.FromMinutes(5));

        var fake = new FakePublisher();
        var publisher = new RabbtPublsher(fake);
        var saga = new SagaOrchestrator(publisher, db, new LoggerFactory().CreateLogger<SagaOrchestrator>());

        var handler = new CreateProductHandler(db, cache, saga);

        var created = await handler.Handle(new CreateProductCommand("P1", "D", 10m), CancellationToken.None);

        created.Status.Should().Be(ProductStatus.Active);
        (await cache.GetAsync<object>("products:all")).Should().BeNull();
        (await cache.GetAsync<object>($"products:{created.Id}")).Should().BeNull();
        fake.Sent.Any(s => s.key == "product.created").Should().BeTrue();
    }

    [Fact]
    public async Task UpdateProduct_PublishesUpdated_AndInvalidatesCaches()
    {
        var options = new DbContextOptionsBuilder<ProductDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new ProductDbContext(options);

        var entity = new Product.Doman.Enttes.Product { Name = "P", Price = 1m, Status = ProductStatus.Pending };
        db.Products.Add(entity);
        await db.SaveChangesAsync();

        var cache = new RedsCacheServce(new MemoryDistributedCacheAdapter());
        await cache.SetAsync("products:all", new[] { new { x = 1 } }, TimeSpan.FromMinutes(5));
        await cache.SetAsync($"products:{entity.Id}", new { id = entity.Id }, TimeSpan.FromMinutes(5));

        var fake = new FakePublisher();
        var publisher = new RabbtPublsher(fake);
        var handler = new UpdateProductHandler(db, cache, publisher);

        var updated = await handler.Handle(new UpdateProductCommand(entity.Id, "P2", "D2", 2m, "corr-1"), CancellationToken.None);

        updated.Name.Should().Be("P2");
        (await cache.GetAsync<object>("products:all")).Should().BeNull();
        (await cache.GetAsync<object>($"products:{entity.Id}")).Should().BeNull();
        fake.Sent.Any(s => s.key == "product.updated").Should().BeTrue();
    }
}

