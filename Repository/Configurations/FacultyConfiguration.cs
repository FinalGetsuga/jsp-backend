using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Repository.Configurations;

public class FacultyConfiguration : IEntityTypeConfiguration<Faculty>
{
    public void Configure(EntityTypeBuilder<Faculty> builder)
    {
        builder.Property(f => f.Name).IsRequired().HasMaxLength(200);
        builder.Property(f => f.ShortCode).IsRequired().HasMaxLength(20);
        builder.Property(f => f.EmailDomainSegment).IsRequired().HasMaxLength(100);
        
        builder.HasIndex(f => f.ShortCode).IsUnique();
        builder.HasIndex(f => f.EmailDomainSegment).IsUnique();
    }
}