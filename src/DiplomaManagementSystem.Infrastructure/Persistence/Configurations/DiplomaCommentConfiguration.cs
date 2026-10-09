using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiplomaManagementSystem.Infrastructure.Persistence.Configurations;

internal sealed class DiplomaCommentConfiguration : IEntityTypeConfiguration<DiplomaComment>
{
    public void Configure(EntityTypeBuilder<DiplomaComment> builder)
    {
        builder.ToTable("diploma_comments");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Body)
            .HasMaxLength(2000)
            .IsRequired();

        builder.HasIndex(e => e.DiplomaId);

        builder.HasOne(e => e.Diploma)
            .WithMany(d => d.Comments)
            .HasForeignKey(e => e.DiplomaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(e => e.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
