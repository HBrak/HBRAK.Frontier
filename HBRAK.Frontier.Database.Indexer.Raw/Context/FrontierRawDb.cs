using HBRAK.Frontier.Database.Indexer.Raw.Objects;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;


namespace HBRAK.Frontier.Database.Indexer.Raw.Context;

public sealed class FrontierRawDb : DbContext
{
    public FrontierRawDb(DbContextOptions<FrontierRawDb> o) : base(o) { }

    public DbSet<BlockRow> Blocks => Set<BlockRow>();
    public DbSet<TxRow> Txs => Set<TxRow>();
    public DbSet<LogRow> Logs => Set<LogRow>();
    public DbSet<RawCursor> Cursor => Set<RawCursor>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<BlockRow>(e =>
        {
            e.HasKey(x => x.Number);
            e.HasIndex(x => x.Hash).IsUnique();
            e.Property(x => x.Hash).HasMaxLength(66);
            e.Property(x => x.ParentHash).HasMaxLength(66);
        });

        b.Entity<TxRow>(e =>
        {
            e.HasKey(x => x.Hash);
            e.Property(x => x.Hash).HasMaxLength(66);
            e.HasIndex(x => new { x.BlockNumber, x.IndexInBlock });
            e.Property(x => x.From).HasMaxLength(42);
            e.Property(x => x.To).HasMaxLength(42);
        });

        b.Entity<LogRow>(e =>
        {
            e.HasKey(x => new { x.TxHash, x.LogIndex });
            e.Property(x => x.TxHash).HasMaxLength(66);
            e.Property(x => x.Address).HasMaxLength(42);
            e.Property(x => x.Topic0).HasMaxLength(66);
            e.HasIndex(x => new { x.Address, x.Topic0 });
            e.HasIndex(x => x.BlockNumber);
            e.Property(x => x.Topics)
             .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<string[]>(v, (JsonSerializerOptions?)null) ?? Array.Empty<string>());
        });

        b.Entity<RawCursor>(e => e.HasKey(x => x.Id));

        b.ApplyConfigurationsFromAssembly(typeof(FrontierRawDb).Assembly);
        base.OnModelCreating(b);
    }
}