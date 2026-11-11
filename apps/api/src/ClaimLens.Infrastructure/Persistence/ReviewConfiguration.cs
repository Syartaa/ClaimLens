using ClaimLens.Domain.Investigations;
using ClaimLens.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimLens.Infrastructure.Persistence;

public sealed class EvidenceReviewConfiguration : IEntityTypeConfiguration<EvidenceReview>
{
    public void Configure(EntityTypeBuilder<EvidenceReview> builder)
    {
        builder.ToTable("evidence_reviews");
        builder.HasKey(review => review.Id);
        builder.Property(review => review.PreviousStance).HasConversion<string>().HasMaxLength(32);
        builder.Property(review => review.NewStance).HasConversion<string>().HasMaxLength(32);
        builder.Property(review => review.Note).HasMaxLength(1000).IsRequired();
        builder.HasIndex(review => review.EvidenceId);
        builder.HasOne(review => review.Evidence)
            .WithMany(evidence => evidence.Reviews)
            .HasForeignKey(review => review.EvidenceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(review => review.Reviewer)
            .WithMany()
            .HasForeignKey(review => review.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("audit_events");
        builder.HasKey(audit => audit.Id);
        builder.Property(audit => audit.EventType).HasMaxLength(64).IsRequired();
        builder.Property(audit => audit.Message).IsRequired();
        builder.Property(audit => audit.Metadata).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(audit => new { audit.InvestigationId, audit.CreatedAt });
        builder.HasOne(audit => audit.Investigation)
            .WithMany()
            .HasForeignKey(audit => audit.InvestigationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(audit => audit.Actor)
            .WithMany()
            .HasForeignKey(audit => audit.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
