using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Product.Doman.Enttes;
using Product.Infrastructure.Cache;
using Product.Infrastructure.Messagng;
using Product.Infrastructure.Persstence;

namespace Product.Applcaton.Products.Commands;

public record UpdateProductCommand(Guid Id, string Name, string? Description, decimal Price, string? CorrelationId = null) : IRequest<Product>;

public class UpdateProductValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Price).GreaterThan(0);
    }
}

public class UpdateProductHandler : IRequestHandler<UpdateProductCommand, Product>
{
    private readonly ProductDbContext _db;
    private readonly RedsCacheServce _cache;
    private readonly RabbtPublsher _publisher;

    public UpdateProductHandler(ProductDbContext db, RedsCacheServce cache, RabbtPublsher publisher)
    {
        _db = db; _cache = cache; _publisher = publisher;
    }

    public async Task<Product> Handle(UpdateProductCommand request, CancellationToken ct)
    {
        var entity = await _db.Products.FirstOrDefaultAsync(p => p.Id == request.Id, ct)
            ?? throw new KeyNotFoundException("Product not found");

        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.Price = request.Price;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        await _cache.RemoveAsync("products:all", ct);
        await _cache.RemoveAsync($"products:{entity.Id}", ct);

        await _publisher.PublishAsync("products.exchange", "product.updated", new
        {
            eventId = Guid.NewGuid(),
            eventType = "product.updated",
            occurredAt = DateTime.UtcNow,
            correlationId = request.CorrelationId ?? string.Empty,
            payload = new { id = entity.Id, name = entity.Name, price = entity.Price }
        }, ct);

        return entity;
    }
}

