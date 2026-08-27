using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vasl.Domain.Entities;

namespace Vasl.Infrastructure.Data.EFConfigurations;

public class UrlConfiguration : IEntityTypeConfiguration<Url>
{
    public void Configure(EntityTypeBuilder<Url> builder)
    {
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
               .ValueGeneratedNever();

        builder.Property(entity => entity.Code)
               .IsRequired()
               .HasConversion<string>()
               .HasMaxLength(20);

        builder.Property(entity => entity.OriginalUrl)
               .IsRequired()
               .HasConversion<string>()
               .HasMaxLength(2000)
               .IsUnicode();

        builder.Property(entity => entity.CreationTimeUtc)
               .IsRequired();

        builder.Property(entity => entity.ExpirationTimeUtc)
               .IsRequired(false);

        builder.HasIndex(entity => entity.Code);
    }
}