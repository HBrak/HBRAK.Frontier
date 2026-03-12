using HBRAK.Frontier.Database.Indexer.Raw.Objects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;


namespace HBRAK.Frontier.Database.Indexer.Raw.Context;

public sealed class FrontierRawDb : DbContext
{
    public FrontierRawDb(DbContextOptions<FrontierRawDb> o) : base(o) { }

    public DbSet<InputLogRow> InputLogs => Set<InputLogRow>();
    public DbSet<UnableToParseLogRow> UnableToParseLogs => Set<UnableToParseLogRow>();
    public DbSet<RawCursor> Cursor => Set<RawCursor>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        ConfigureLog(b.Entity<InputLogRow>());
        ConfigureLog(b.Entity<UnableToParseLogRow>());

        b.Entity<RawCursor>(e => e.HasKey(x => x.Id));
        base.OnModelCreating(b);
    }

    private static void ConfigureLog<T>(EntityTypeBuilder<T> e) where T : LogRowBase
    {
        e.HasKey(x => new { x.TxHash, x.LogIndex });

        e.Property(x => x.TxHash).IsRequired().HasMaxLength(64);
        e.Property(x => x.LogIndex).IsRequired();

        e.Property(x => x.Address).IsRequired().HasMaxLength(64);

        e.Property(x => x.Topic0).IsRequired().HasMaxLength(64);
        e.Property(x => x.Topic1).HasMaxLength(64);
        e.Property(x => x.Topic2).HasMaxLength(64);
        e.Property(x => x.Topic3).HasMaxLength(64);

        e.Property(x => x.Data);
        e.Property(x => x.BlockNumber).IsRequired();
        e.Property(x => x.BlockTime).IsRequired();

        e.HasIndex(x => x.BlockNumber);
        e.HasIndex(x => new { x.Address, x.Topic0 });
    }
}