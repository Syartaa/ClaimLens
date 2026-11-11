using ClaimLens.Domain.Investigations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimLens.Infrastructure.Persistence;

public sealed class ClaimConfiguration : IEntityTypeConfiguration<Claim>
{
    public void Configure(EntityTypeBuilder<Claim> builder)
    {
        builder.ToTable("claims");
        builder.HasKey(claim => claim.Id);
        builder.Property(claim => claim.ClaimText).IsRequired();
        builder.Property(claim => claim.NormalizedClaim).IsRequired();
        builder.Property(claim => claim.ClaimType).HasConversion<string>().HasMaxLength(32);
        builder.Property(claim => claim.ImportanceScore).HasPrecision(5, 4);
        builder.HasIndex(claim => new { claim.InvestigationId, claim.Position });
        builder.HasOne(claim => claim.Investigation)
            .WithMany()
            .HasForeignKey(claim => claim.InvestigationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
