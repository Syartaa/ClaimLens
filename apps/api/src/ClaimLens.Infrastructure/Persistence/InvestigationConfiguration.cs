using ClaimLens.Domain.Investigations;
using ClaimLens.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimLens.Infrastructure.Persistence;

public sealed class InvestigationConfiguration : IEntityTypeConfiguration<Investigation>
{
    public void Configure(EntityTypeBuilder<Investigation> builder)
    {
        builder.ToTable("investigations");
        builder.HasKey(investigation => investigation.Id);
        builder.Property(investigation => investigation.Title).HasMaxLength(200).IsRequired();
        builder.Property(investigation => investigation.OriginalInput).IsRequired();
        builder.Property(investigation => investigation.InputType).HasConversion<string>().HasMaxLength(32);
        builder.Property(investigation => investigation.ProcessingStatus).HasConversion<string>().HasMaxLength(32);
        builder.Property(investigation => investigation.Verdict).HasConversion<string>().HasMaxLength(32);
        builder.Property(investigation => investigation.ConfidenceScore).HasPrecision(5, 4);
        builder.Property(investigation => investigation.FailureCode).HasMaxLength(64);
        builder.Property(investigation => investigation.EvidenceProvider).HasMaxLength(64);
        builder.HasIndex(investigation => new { investigation.UserId, investigation.CreatedAt });
        builder.HasIndex(investigation => new { investigation.ProcessingStatus, investigation.CreatedAt });
        builder.HasOne(investigation => investigation.User)
            .WithMany()
            .HasForeignKey(investigation => investigation.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(investigation => investigation.ApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
