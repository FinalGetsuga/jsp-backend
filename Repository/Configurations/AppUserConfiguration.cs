using Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Repository.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.Property(f => f.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(f => f.LastName).IsRequired().HasMaxLength(100);
        builder.Property(f => f.StudentIndexNumber).HasMaxLength(10);

        builder.HasOne(f => f.Faculty)
            .WithMany(f => f.Students)
            .HasForeignKey(f => f.FacultyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}