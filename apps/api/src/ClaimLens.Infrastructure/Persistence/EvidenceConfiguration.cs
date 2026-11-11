using ClaimLens.Domain.Investigations;
using ClaimLens.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimLens.Infrastructure.Persistence;

public sealed class SourceConfiguration : IEntityTypeConfiguration<Source>
{
    public void Configure(EntityTypeBuilder<Source> builder)
    {
        builder.ToTable("sources");
        builder.HasKey(source => source.Id);
        builder.Property(source => source.Url).HasMaxLength(2000).IsRequired();
        builder.HasIndex(source => source.Url).IsUnique();
        builder.Property(source => source.Domain).HasMaxLength(255).IsRequired();
        builder.Property(source => source.Title).HasMaxLength(500).IsRequired();
        builder.Property(source => source.Author).HasMaxLength(200);
        builder.Property(source => source.Publisher).HasMaxLength(200);
        builder.Property(source => source.FlagNote).HasMaxLength(1000);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(source => source.FlaggedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(source => source.Quality)
            .WithOne(quality => quality.Source)
            .HasForeignKey<SourceQuality>(quality => quality.SourceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class SourceQualityConfiguration : IEntityTypeConfiguration<SourceQuality>
{
    public void Configure(EntityTypeBuilder<SourceQuality> builder)
    {
        builder.ToTable("source_qualities");
        builder.HasKey(quality => quality.Id);
        builder.Property(quality => quality.Band).HasConversion<string>().HasMaxLength(32);
        builder.Property(quality => quality.FactorsJson).HasColumnType("jsonb").IsRequired();
        builder.Property(quality => quality.Explanation).IsRequired();
        builder.HasIndex(quality => quality.SourceId).IsUnique();
    }
}

public sealed class EvidenceConfiguration : IEntityTypeConfiguration<Evidence>
{
    public void Configure(EntityTypeBuilder<Evidence> builder)
    {
        builder.ToTable("evidence");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.EvidenceText).IsRequired();
        builder.Property(item => item.AiStance).HasConversion<string>().HasMaxLength(32);
        builder.Property(item => item.Stance).HasConversion<string>().HasMaxLength(32);
        builder.Property(item => item.RelevanceScore).HasPrecision(5, 4);
        builder.Property(item => item.ConfidenceScore).HasPrecision(5, 4);
        builder.Property(item => item.PromptVersion).HasMaxLength(32);
        builder.Property(item => item.ModelUsed).HasMaxLength(64);
        builder.HasIndex(item => new { item.ClaimId, item.Stance });
        builder.HasOne(item => item.Claim)
            .WithMany()
            .HasForeignKey(item => item.ClaimId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(item => item.Source)
            .WithMany()
            .HasForeignKey(item => item.SourceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
