using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Identty;

public class AppDbContext : DbContext
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<RefreshToken>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Token).IsRequired();
            e.HasIndex(x => x.Token).IsUnique();
        });
    }
}

