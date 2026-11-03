CREATE EXTENSION IF NOT EXISTS vector;

CREATE TABLE research_items (
    id uuid PRIMARY KEY,
    slug varchar(180) NOT NULL UNIQUE,
    canonical_title varchar(500) NOT NULL,
    item_type text NOT NULL,
    evidence_strength text NOT NULL,
    status text NOT NULL,
    is_synthetic boolean NOT NULL,
    topics text[] NOT NULL,
    created_at timestamptz NOT NULL,
    search_vector tsvector GENERATED ALWAYS AS (to_tsvector('simple', canonical_title)) STORED
);
CREATE INDEX research_items_search_vector_idx ON research_items USING gin(search_vector);
CREATE TABLE source_documents (
    id uuid PRIMARY KEY,
    title text NOT NULL,
    url text NOT NULL,
    publisher text NOT NULL,
    published_at timestamptz NOT NULL,
    content_hash varchar(64) NOT NULL,
    snapshot_uri text NOT NULL,
    is_synthetic boolean NOT NULL,
    UNIQUE(url, content_hash)
);
CREATE TABLE claims (
    id uuid PRIMARY KEY,
    research_item_id uuid NOT NULL REFERENCES research_items(id) ON DELETE RESTRICT,
    statement text NOT NULL,
    claim_type text NOT NULL,
    confidence text NOT NULL
);
CREATE INDEX claims_research_item_idx ON claims(research_item_id);
CREATE TABLE evidence (
    id uuid PRIMARY KEY,
    claim_id uuid NOT NULL REFERENCES claims(id) ON DELETE RESTRICT,
    source_document_id uuid NOT NULL REFERENCES source_documents(id) ON DELETE RESTRICT,
    kind text NOT NULL,
    excerpt text NOT NULL,
    locator text NOT NULL
);
CREATE INDEX evidence_claim_idx ON evidence(claim_id);
CREATE INDEX evidence_source_idx ON evidence(source_document_id);
CREATE TABLE localizations (
    id uuid PRIMARY KEY,
    research_item_id uuid NOT NULL REFERENCES research_items(id) ON DELETE RESTRICT,
    locale varchar(10) NOT NULL CHECK (locale IN ('en', 'pt-BR')),
    title text NOT NULL,
    summary text NOT NULL,
    why_it_matters text NOT NULL,
    limitations text[] NOT NULL,
    is_reviewed boolean NOT NULL,
    UNIQUE(research_item_id, locale)
);
CREATE TABLE publication_versions (
    id uuid PRIMARY KEY,
    research_item_id uuid NOT NULL REFERENCES research_items(id) ON DELETE RESTRICT,
    slug text NOT NULL,
    locale varchar(10) NOT NULL CHECK (locale IN ('en', 'pt-BR')),
    version integer NOT NULL CHECK (version > 0),
    operation_id uuid NOT NULL UNIQUE,
    published_at timestamptz NOT NULL,
    change_note text NOT NULL,
    snapshot_json jsonb NOT NULL,
    UNIQUE(research_item_id, locale, version),
    UNIQUE(slug, locale, version)
);
CREATE INDEX publication_versions_locale_published_idx ON publication_versions(locale, published_at);
CREATE TABLE outbox_messages (
    id uuid PRIMARY KEY REFERENCES publication_versions(id) ON DELETE RESTRICT,
    aggregate_id uuid NOT NULL,
    causation_id uuid NOT NULL,
    correlation_id uuid NOT NULL,
    event_type text NOT NULL,
    schema_version integer NOT NULL,
    occurred_at timestamptz NOT NULL,
    producer text NOT NULL,
    payload_json jsonb NOT NULL,
    processed_at timestamptz,
    attempts integer NOT NULL,
    last_error text
);
CREATE INDEX outbox_messages_pending_idx ON outbox_messages(occurred_at) WHERE processed_at IS NULL;
CREATE TABLE research_embeddings (
    research_item_id uuid PRIMARY KEY REFERENCES research_items(id) ON DELETE RESTRICT,
    model text NOT NULL,
    embedding vector(1536)
);
CREATE INDEX research_embeddings_vector_idx ON research_embeddings USING hnsw(embedding vector_cosine_ops);

CREATE FUNCTION reject_immutable_record_change() RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
    RAISE EXCEPTION '% is append-only; create a new version instead', TG_TABLE_NAME
        USING ERRCODE = '55000';
END;
$$;
CREATE TRIGGER immutable_publication_versions BEFORE UPDATE OR DELETE ON publication_versions
    FOR EACH ROW EXECUTE FUNCTION reject_immutable_record_change();
CREATE TRIGGER immutable_source_documents BEFORE UPDATE OR DELETE ON source_documents
    FOR EACH ROW EXECUTE FUNCTION reject_immutable_record_change();
