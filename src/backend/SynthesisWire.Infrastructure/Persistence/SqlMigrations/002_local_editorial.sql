CREATE TABLE source_captures (
    id uuid PRIMARY KEY,
    requested_url text NOT NULL,
    final_url text NOT NULL,
    media_type text NOT NULL,
    body bytea NOT NULL CHECK (octet_length(body) BETWEEN 1 AND 524288),
    content_hash varchar(64) NOT NULL,
    material_hash varchar(64) NOT NULL,
    fetched_at timestamptz NOT NULL,
    is_synthetic boolean NOT NULL,
    UNIQUE(requested_url, content_hash)
);
CREATE INDEX source_captures_material_idx ON source_captures(material_hash);
CREATE TRIGGER immutable_source_captures BEFORE UPDATE OR DELETE ON source_captures
    FOR EACH ROW EXECUTE FUNCTION reject_immutable_record_change();
CREATE TABLE editorial_drafts (
    id uuid PRIMARY KEY,
    capture_id uuid NOT NULL UNIQUE REFERENCES source_captures(id) ON DELETE RESTRICT,
    revision integer NOT NULL DEFAULT 0 CHECK (revision >= 0),
    state text NOT NULL DEFAULT 'Draft' CHECK (state IN ('Draft','InReview','Reviewed','Published')),
    payload_json jsonb,
    author text NOT NULL,
    reviewer text,
    research_item_id uuid NOT NULL UNIQUE,
    en_operation_id uuid NOT NULL UNIQUE,
    pt_operation_id uuid NOT NULL UNIQUE,
    created_at timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE editorial_events (
    id uuid PRIMARY KEY,
    draft_id uuid NOT NULL REFERENCES editorial_drafts(id) ON DELETE RESTRICT,
    revision integer NOT NULL,
    action text NOT NULL,
    actor text NOT NULL,
    occurred_at timestamptz NOT NULL DEFAULT now()
);
CREATE TRIGGER immutable_editorial_events BEFORE UPDATE OR DELETE ON editorial_events
    FOR EACH ROW EXECUTE FUNCTION reject_immutable_record_change();
