using ClaimLens.Domain.Auth;
using ClaimLens.Domain.Investigations;
using ClaimLens.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace ClaimLens.Infrastructure.Persistence;

public sealed class ClaimLensDbContext : DbContext
{
    public ClaimLensDbContext(DbContextOptions<ClaimLensDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Investigation> Investigations => Set<Investigation>();

    public DbSet<Claim> Claims => Set<Claim>();

    public DbSet<AiRun> AiRuns => Set<AiRun>();

    public DbSet<Source> Sources => Set<Source>();

    public DbSet<SourceQuality> SourceQualities => Set<SourceQuality>();

    public DbSet<Evidence> Evidence => Set<Evidence>();

    public DbSet<AnalysisResult> AnalysisResults => Set<AnalysisResult>();

    public DbSet<EvidenceReview> EvidenceReviews => Set<EvidenceReview>();

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClaimLensDbContext).Assembly);
    }
}
