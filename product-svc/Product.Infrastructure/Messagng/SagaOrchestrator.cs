using Microsoft.EntityFrameworkCore;
using Product.Doman.Enums;
using Product.Infrastructure.Persstence;

namespace Product.Infrastructure.Messagng;

public class SagaOrchestrator
{
    private readonly RabbtPublsher _publisher;
    private readonly ProductDbContext _db;
    private readonly ILogger<SagaOrchestrator> _logger;

    public SagaOrchestrator(RabbtPublsher publisher, ProductDbContext db, ILogger<SagaOrchestrator> logger)
    {
        _publisher = publisher;
        _db = db;
        _logger = logger;
    }

    public async Task RunCreateSagaAsync(Product.Doman.Enttes.Product product, string? correlationId, CancellationToken ct)
    {
        try
        {
            await _publisher.PublishAsync("products.exchange", "product.created", new
            {
                eventId = Guid.NewGuid(),
                eventType = "product.created",
                occurredAt = DateTime.UtcNow,
                correlationId = correlationId ?? string.Empty,
                payload = new { id = product.Id, name = product.Name, price = product.Price }
            }, ct);

            await Task.Delay(TimeSpan.FromMilliseconds(200), ct);

            var entity = await _db.Products.FirstAsync(p => p.Id == product.Id, ct);
            entity.Status = ProductStatus.Active;
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Saga completed for product {Id}", product.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Saga failed for product {Id}", product.Id);
            var entity = await _db.Products.FirstAsync(p => p.Id == product.Id, ct);
            entity.Status = ProductStatus.Failed;
            await _db.SaveChangesAsync(ct);

            await _publisher.PublishAsync("products.exchange", "product.create.failed", new
            {
                eventId = Guid.NewGuid(),
                eventType = "product.create.failed",
                occurredAt = DateTime.UtcNow,
                correlationId = correlationId ?? string.Empty,
                payload = new { id = product.Id, reason = "ack-timeout" }
            }, ct);
        }
    }
}

