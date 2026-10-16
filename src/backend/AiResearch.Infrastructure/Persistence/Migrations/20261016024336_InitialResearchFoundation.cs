using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;
using Pgvector;

#nullable disable

namespace AiResearch.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialResearchFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    aggregate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    causation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "text", nullable: false),
                    schema_version = table.Column<int>(type: "integer", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    producer = table.Column<string>(type: "text", nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "research_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    canonical_title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    item_type = table.Column<string>(type: "text", nullable: false),
                    evidence_strength = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    is_synthetic = table.Column<bool>(type: "boolean", nullable: false),
                    topics = table.Column<string[]>(type: "text[]", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    search_vector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: true, computedColumnSql: "to_tsvector('simple', canonical_title)", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_research_items", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "source_documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    url = table.Column<string>(type: "text", nullable: false),
                    publisher = table.Column<string>(type: "text", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    content_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    snapshot_uri = table.Column<string>(type: "text", nullable: false),
                    is_synthetic = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_source_documents", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "claims",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    research_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    statement = table.Column<string>(type: "text", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: false),
                    confidence = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_claims", x => x.id);
                    table.ForeignKey(
                        name: "FK_claims_research_items_research_item_id",
                        column: x => x.research_item_id,
                        principalTable: "research_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "localizations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    research_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locale = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    summary = table.Column<string>(type: "text", nullable: false),
                    why_it_matters = table.Column<string>(type: "text", nullable: false),
                    limitations = table.Column<string[]>(type: "text[]", nullable: false),
                    is_reviewed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_localizations", x => x.id);
                    table.CheckConstraint("ck_localizations_locale", "locale IN ('en', 'pt-BR')");
                    table.ForeignKey(
                        name: "FK_localizations_research_items_research_item_id",
                        column: x => x.research_item_id,
                        principalTable: "research_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "publication_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    research_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false),
                    locale = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    operation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    change_note = table.Column<string>(type: "text", nullable: false),
                    snapshot_json = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_publication_versions", x => x.id);
                    table.CheckConstraint("ck_publication_versions_locale", "locale IN ('en', 'pt-BR')");
                    table.CheckConstraint("ck_publication_versions_version", "version > 0");
                    table.ForeignKey(
                        name: "FK_publication_versions_research_items_research_item_id",
                        column: x => x.research_item_id,
                        principalTable: "research_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "research_embeddings",
                columns: table => new
                {
                    research_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    model = table.Column<string>(type: "text", nullable: false),
                    embedding = table.Column<Vector>(type: "vector(1536)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_research_embeddings", x => x.research_item_id);
                    table.ForeignKey(
                        name: "FK_research_embeddings_research_items_research_item_id",
                        column: x => x.research_item_id,
                        principalTable: "research_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "evidence",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false),
                    excerpt = table.Column<string>(type: "text", nullable: false),
                    locator = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evidence", x => x.id);
                    table.ForeignKey(
                        name: "FK_evidence_claims_claim_id",
                        column: x => x.claim_id,
                        principalTable: "claims",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_evidence_source_documents_source_document_id",
                        column: x => x.source_document_id,
                        principalTable: "source_documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_claims_research_item_id",
                table: "claims",
                column: "research_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_evidence_claim_id",
                table: "evidence",
                column: "claim_id");

            migrationBuilder.CreateIndex(
                name: "IX_evidence_source_document_id",
                table: "evidence",
                column: "source_document_id");

            migrationBuilder.CreateIndex(
                name: "IX_localizations_research_item_id_locale",
                table: "localizations",
                columns: new[] { "research_item_id", "locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_occurred_at",
                table: "outbox_messages",
                column: "occurred_at",
                filter: "processed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_publication_versions_locale_published_at",
                table: "publication_versions",
                columns: new[] { "locale", "published_at" });

            migrationBuilder.CreateIndex(
                name: "IX_publication_versions_operation_id",
                table: "publication_versions",
                column: "operation_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_publication_versions_research_item_id_locale_version",
                table: "publication_versions",
                columns: new[] { "research_item_id", "locale", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_publication_versions_slug_locale_version",
                table: "publication_versions",
                columns: new[] { "slug", "locale", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_research_embeddings_embedding",
                table: "research_embeddings",
                column: "embedding")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_research_items_search_vector",
                table: "research_items",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_research_items_slug",
                table: "research_items",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_source_documents_url_content_hash",
                table: "source_documents",
                columns: new[] { "url", "content_hash" },
                unique: true);

            // Defense in depth: application guards cannot be bypassed by ordinary SQL UPDATE/DELETE.
            migrationBuilder.Sql("""
                CREATE FUNCTION reject_immutable_record_change() RETURNS trigger
                LANGUAGE plpgsql AS $$
                BEGIN
                    RAISE EXCEPTION '% is append-only; create a new version instead', TG_TABLE_NAME
                        USING ERRCODE = '55000';
                END;
                $$;
                CREATE TRIGGER immutable_publication_versions
                    BEFORE UPDATE OR DELETE ON publication_versions
                    FOR EACH ROW EXECUTE FUNCTION reject_immutable_record_change();
                CREATE TRIGGER immutable_source_documents
                    BEFORE UPDATE OR DELETE ON source_documents
                    FOR EACH ROW EXECUTE FUNCTION reject_immutable_record_change();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "evidence");

            migrationBuilder.DropTable(
                name: "localizations");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "publication_versions");

            migrationBuilder.DropTable(
                name: "research_embeddings");

            migrationBuilder.DropTable(
                name: "claims");

            migrationBuilder.DropTable(
                name: "source_documents");

            migrationBuilder.DropTable(
                name: "research_items");

            migrationBuilder.Sql("DROP FUNCTION reject_immutable_record_change();");
        }
    }
}
