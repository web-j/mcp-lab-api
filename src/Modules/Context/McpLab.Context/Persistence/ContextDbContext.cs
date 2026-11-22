using Microsoft.EntityFrameworkCore;

namespace McpLab.Context.Persistence;

/// <summary>Schema <c>context</c> (ADR 0004): eventos, notas, resumos semanais e nomes dos apps conectados.</summary>
internal sealed class ContextDbContext(DbContextOptions<ContextDbContext> options) : DbContext(options)
{
    public const string Schema = "context";

    public DbSet<ActivityEvent> Events => Set<ActivityEvent>();
    public DbSet<ContextNote> Notes => Set<ContextNote>();
    public DbSet<WeeklyRollup> WeeklyRollups => Set<WeeklyRollup>();
    public DbSet<ConnectedClient> Clients => Set<ConnectedClient>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.HasDefaultSchema(Schema);

        model.Entity<ActivityEvent>(e =>
        {
            e.ToTable("events");
            e.HasKey(x => x.Id);
            e.Property(x => x.Module).HasMaxLength(40);
            e.Property(x => x.Tool).HasMaxLength(80);
            e.Property(x => x.ClientId).HasMaxLength(100);
            e.Property(x => x.Summary).HasMaxLength(120);
            e.Property(x => x.EntityRef).HasMaxLength(200);
            e.Property(x => x.Metrics).HasColumnType("jsonb");
            e.Property(x => x.IdempotencyKey).HasMaxLength(200);
            e.HasIndex(x => new { x.UserId, x.OccurredAt }).IsDescending(false, true);
            e.HasIndex(x => new { x.UserId, x.Tool, x.IdempotencyKey })
                .IsUnique()
                .HasFilter("idempotency_key IS NOT NULL");
        });

        model.Entity<ContextNote>(e =>
        {
            e.ToTable("notes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Kind).HasMaxLength(20);
            e.Property(x => x.Text).HasMaxLength(280);
            e.Property(x => x.Module).HasMaxLength(40);
            e.Property(x => x.Status).HasMaxLength(10);
            e.Property(x => x.ClientId).HasMaxLength(100);
            e.HasIndex(x => new { x.UserId, x.Status, x.Pinned, x.UpdatedAt });
        });

        model.Entity<WeeklyRollup>(e =>
        {
            e.ToTable("weekly_rollups");
            e.HasKey(x => new { x.UserId, x.Module, x.IsoWeek });
            e.Property(x => x.Module).HasMaxLength(40);
            e.Property(x => x.IsoWeek).HasMaxLength(8);
            e.Property(x => x.Metrics).HasColumnType("jsonb");
        });

        model.Entity<ConnectedClient>(e =>
        {
            e.ToTable("clients");
            e.HasKey(x => x.ClientId);
            e.Property(x => x.ClientId).HasMaxLength(100);
            e.Property(x => x.Name).HasMaxLength(120);
        });

        model.UseSnakeCaseNames();
    }
}
