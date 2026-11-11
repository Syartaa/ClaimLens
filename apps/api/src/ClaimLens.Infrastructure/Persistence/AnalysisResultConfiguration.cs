using ClaimLens.Domain.Investigations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimLens.Infrastructure.Persistence;

public sealed class AnalysisResultConfiguration : IEntityTypeConfiguration<AnalysisResult>
{
    public void Configure(EntityTypeBuilder<AnalysisResult> builder)
    {
        builder.ToTable("analysis_results");
        builder.HasKey(result => result.Id);
        builder.Property(result => result.Verdict).HasConversion<string>().HasMaxLength(32);
        builder.Property(result => result.Confidence).HasPrecision(5, 4);
        builder.Property(result => result.Explanation).IsRequired();
        builder.Property(result => result.AlgorithmVersion).HasMaxLength(32).IsRequired();
        builder.HasIndex(result => new { result.ClaimId, result.GeneratedAt });
        builder.HasOne(result => result.Claim)
            .WithMany()
            .HasForeignKey(result => result.ClaimId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
