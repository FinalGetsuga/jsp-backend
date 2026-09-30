using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Repository.Configurations;

public class ApplicationDocumentConfiguration : IEntityTypeConfiguration<ApplicationDocument>
{
    public void Configure(EntityTypeBuilder<ApplicationDocument> builder)
    {
        builder.Property(f => f.StorageKey).IsRequired().HasMaxLength(500);
        builder.Property(f => f.ContentType).IsRequired().HasMaxLength(100);
        builder.Property(f => f.Checksum).IsRequired().HasMaxLength(64);

        builder.Property(f => f.Type)
            .HasConversion<string>()
            .HasMaxLength(30);
        
        builder.HasOne(f => f.Application)
            .WithMany(f => f.Documents)
            .HasForeignKey(f => f.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(f => new { f.ApplicationId, f.Type }).IsUnique();
    }
}