using Microsoft.EntityFrameworkCore;
using Vasl.Domain.Entities;

namespace Vasl.Infrastructure.Data;

public class VaslDbContext : DbContext
{
    public DbSet<Url> Urls { get; set; }

    public VaslDbContext(DbContextOptions<VaslDbContext> options) : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
        base.OnModelCreating(modelBuilder);
    }
}