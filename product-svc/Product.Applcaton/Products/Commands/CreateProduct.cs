using FluentValidation;
using MediatR;
using Product.Doman.Enttes;
using Product.Doman.Enums;
using Product.Infrastructure.Cache;
using Product.Infrastructure.Messagng;
using Product.Infrastructure.Persstence;

namespace Product.Applcaton.Products.Commands;

public record CreateProductCommand(string Name, string? Description, decimal Price, string? CorrelationId = null) : IRequest<Product>;

public class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Price).GreaterThan(0);
    }
}

public class CreateProductHandler : IRequestHandler<CreateProductCommand, Product>
{
    private readonly ProductDbContext _db;
    private readonly RedsCacheServce _cache;
    private readonly SagaOrchestrator _saga;

    public CreateProductHandler(ProductDbContext db, RedsCacheServce cache, SagaOrchestrator saga)
    {
        _db = db; _cache = cache; _saga = saga;
    }

    public async Task<Product> Handle(CreateProductCommand request, CancellationToken ct)
    {
        var entity = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Status = ProductStatus.Pending
        };

        _db.Products.Add(entity);
        await _db.SaveChangesAsync(ct);

        await _cache.RemoveAsync("products:all", ct);
        await _cache.RemoveAsync($"products:{entity.Id}", ct);

        await _saga.RunCreateSagaAsync(entity, request.CorrelationId, ct);

        await _cache.RemoveAsync($"products:{entity.Id}", ct);

        return entity;
    }
}

