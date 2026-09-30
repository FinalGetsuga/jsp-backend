using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Repository.Configurations;

public class ApplicationConfiguration : IEntityTypeConfiguration<Application>
{
    public void Configure(EntityTypeBuilder<Application> builder)
    {
        builder.Property(f => f.RejectionReason).HasMaxLength(1000);
        
        builder.Property(f => f.Status)
            .HasConversion<string>()
            .HasMaxLength(100);

        builder.HasOne(f => f.User)
            .WithMany()
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(f => f.ReviewedByAdmin)
            .WithMany()
            .HasForeignKey(f => f.ReviewdByAdminId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(f => new { f.UserId, f.RenewalYear }).IsUnique();
    }
}