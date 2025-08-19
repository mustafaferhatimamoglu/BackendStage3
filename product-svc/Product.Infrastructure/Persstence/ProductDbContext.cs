using Microsoft.EntityFrameworkCore;
using Product.Doman.Enttes;
using Product.Doman.Enums;

namespace Product.Infrastructure.Persstence;

public class ProductDbContext : DbContext
{
    public DbSet<Product> Products => Set<Product>();

    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Product>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Price).HasColumnType("decimal(18,2)");
            e.Property(x => x.Status).HasConversion<int>().HasDefaultValue(ProductStatus.Pending);
        });
    }
}

