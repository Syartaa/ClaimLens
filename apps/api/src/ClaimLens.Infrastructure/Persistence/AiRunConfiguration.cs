using ClaimLens.Domain.Investigations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimLens.Infrastructure.Persistence;

public sealed class AiRunConfiguration : IEntityTypeConfiguration<AiRun>
{
    public void Configure(EntityTypeBuilder<AiRun> builder)
    {
        builder.ToTable("ai_runs");
        builder.HasKey(run => run.Id);
        builder.Property(run => run.Purpose).HasConversion<string>().HasMaxLength(32);
        builder.Property(run => run.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(run => run.PromptName).HasMaxLength(64);
        builder.Property(run => run.PromptVersion).HasMaxLength(32);
        builder.Property(run => run.ModelUsed).HasMaxLength(64);
        builder.Property(run => run.ErrorCategory).HasMaxLength(64);
        builder.HasIndex(run => new { run.InvestigationId, run.CreatedAt });
    }
}
