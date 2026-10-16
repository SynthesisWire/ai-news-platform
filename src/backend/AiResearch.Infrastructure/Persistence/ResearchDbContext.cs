using System.Text.RegularExpressions;
using AiResearch.Domain;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace AiResearch.Infrastructure.Persistence;

public sealed partial class ResearchDbContext(DbContextOptions<ResearchDbContext> options) : DbContext(options)
{
    public DbSet<ResearchItem> ResearchItems => Set<ResearchItem>();
    public DbSet<SourceDocument> SourceDocuments => Set<SourceDocument>();
    public DbSet<Claim> Claims => Set<Claim>();
    public DbSet<Evidence> Evidence => Set<Evidence>();
    public DbSet<Localization> Localizations => Set<Localization>();
    public DbSet<PublicationVersion> PublicationVersions => Set<PublicationVersion>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ResearchEmbedding> ResearchEmbeddings => Set<ResearchEmbedding>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.HasPostgresExtension("vector");
        model.Entity<ResearchItem>(e =>
        {
            e.ToTable("research_items");
            e.HasKey(x => x.Id);
            e.Property(x => x.Slug).HasMaxLength(180);
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.CanonicalTitle).HasMaxLength(500);
            e.Property(x => x.ItemType).HasConversion<string>();
            e.Property(x => x.EvidenceStrength).HasConversion<string>();
            e.Property(x => x.Status).HasConversion<string>();
            e.HasMany(x => x.Claims).WithOne().HasForeignKey(x => x.ResearchItemId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Localizations).WithOne().HasForeignKey(x => x.ResearchItemId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.PublicationVersions).WithOne().HasForeignKey(x => x.ResearchItemId).OnDelete(DeleteBehavior.Restrict);
            e.Navigation(x => x.Claims).HasField("_claims").UsePropertyAccessMode(PropertyAccessMode.Field);
            e.Navigation(x => x.Localizations).HasField("_localizations").UsePropertyAccessMode(PropertyAccessMode.Field);
            e.Navigation(x => x.PublicationVersions).HasField("_publicationVersions").UsePropertyAccessMode(PropertyAccessMode.Field);
            e.Property<NpgsqlTsVector>("SearchVector").HasColumnType("tsvector")
                .HasComputedColumnSql("to_tsvector('simple', canonical_title)", stored: true);
            e.HasIndex("SearchVector").HasMethod("GIN");
        });
        model.Entity<SourceDocument>(e =>
        {
            e.ToTable("source_documents");
            e.HasKey(x => x.Id);
            e.Property(x => x.ContentHash).HasMaxLength(64);
            e.HasIndex(x => new { x.Url, x.ContentHash }).IsUnique();
        });
        model.Entity<Claim>(e =>
        {
            e.ToTable("claims");
            e.HasKey(x => x.Id);
            e.Property(x => x.ClaimType).HasConversion<string>();
            e.Property(x => x.Confidence).HasConversion<string>();
            e.HasMany(x => x.Evidence).WithOne().HasForeignKey(x => x.ClaimId).OnDelete(DeleteBehavior.Restrict);
            e.Navigation(x => x.Evidence).HasField("_evidence").UsePropertyAccessMode(PropertyAccessMode.Field);
        });
        model.Entity<Evidence>(e =>
        {
            e.ToTable("evidence");
            e.HasKey(x => x.Id);
            e.Property(x => x.Kind).HasConversion<string>();
            e.HasOne(x => x.SourceDocument).WithMany().HasForeignKey(x => x.SourceDocumentId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<Localization>(e =>
        {
            e.ToTable("localizations", t => t.HasCheckConstraint("ck_localizations_locale", "locale IN ('en', 'pt-BR')"));
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ResearchItemId, x.Locale }).IsUnique();
            e.Property(x => x.Locale).HasMaxLength(10);
        });
        model.Entity<PublicationVersion>(e =>
        {
            e.ToTable("publication_versions", t =>
            {
                t.HasCheckConstraint("ck_publication_versions_locale", "locale IN ('en', 'pt-BR')");
                t.HasCheckConstraint("ck_publication_versions_version", "version > 0");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Locale).HasMaxLength(10);
            e.Property(x => x.SnapshotJson).HasColumnType("jsonb");
            e.HasIndex(x => new { x.ResearchItemId, x.Locale, x.Version }).IsUnique();
            e.HasIndex(x => new { x.Slug, x.Locale, x.Version }).IsUnique();
            e.HasIndex(x => new { x.Locale, x.PublishedAt });
            e.HasIndex(x => x.OperationId).IsUnique();
        });
        model.Entity<OutboxMessage>(e =>
        {
            e.ToTable("outbox_messages");
            e.HasKey(x => x.Id);
            e.Property(x => x.PayloadJson).HasColumnType("jsonb");
            e.HasIndex(x => x.OccurredAt).HasFilter("processed_at IS NULL");
        });
        model.Entity<ResearchEmbedding>(e =>
        {
            e.ToTable("research_embeddings");
            e.HasKey(x => x.ResearchItemId);
            e.HasOne<ResearchItem>().WithOne().HasForeignKey<ResearchEmbedding>(x => x.ResearchItemId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Embedding).HasColumnType("vector(1536)");
            e.HasIndex(x => x.Embedding).HasMethod("hnsw").HasOperators("vector_cosine_ops");
        });
        foreach (var entity in model.Model.GetEntityTypes())
        foreach (var property in entity.GetProperties())
        {
            property.SetColumnName(SnakeCasePattern().Replace(property.Name, "$1_$2").ToLowerInvariant());
            if (property.IsPrimaryKey() && property.ClrType == typeof(Guid))
                property.ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never;
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        PrepareChanges();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        PrepareChanges();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void PrepareChanges()
    {
        ChangeTracker.DetectChanges();
        if (ChangeTracker.Entries().Any(e => e.Entity is PublicationVersion or SourceDocument
            && e.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Publication versions and source documents are append-only.");
        foreach (var entry in ChangeTracker.Entries<PublicationVersion>().Where(x => x.State == EntityState.Added).ToArray())
        {
            if (!OutboxMessages.Local.Any(x => x.Id == entry.Entity.Id))
                OutboxMessages.Add(new OutboxMessage(entry.Entity));
        }
        // EF's single SaveChanges transaction commits publications and outbox envelopes together.
    }

    [GeneratedRegex("([a-z0-9])([A-Z])", RegexOptions.CultureInvariant)]
    private static partial Regex SnakeCasePattern();
}
