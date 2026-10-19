-- Apply once after database/07_idea_bank.sql.
-- Existing contents keep the displayed Sprint 25; new content defaults to Sprint 26.
BEGIN;

INSERT INTO platforms (code, name, is_active) VALUES
    ('TIKTOK', 'TikTok', TRUE),
    ('YOUTUBE', 'YouTube', TRUE),
    ('FACEBOOK', 'Facebook', TRUE),
    ('INSTAGRAM', 'Instagram', TRUE)
ON CONFLICT (code) DO UPDATE
SET name = EXCLUDED.name,
    is_active = TRUE;

ALTER TABLE contents
    ADD COLUMN sprint VARCHAR(50) NOT NULL DEFAULT 'Sprint 25';

ALTER TABLE contents
    ALTER COLUMN sprint SET DEFAULT 'Sprint 26';

-- Verify the schema and catalog in the same transaction so a mismatch rolls back the migration.
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'contents'
          AND column_name = 'sprint'
          AND data_type = 'character varying'
          AND is_nullable = 'NO'
          AND column_default LIKE '%Sprint 26%'
    ) THEN
        RAISE EXCEPTION 'contents.sprint is missing or does not match the expected schema';
    END IF;

    IF (
        SELECT COUNT(*)
        FROM platforms
        WHERE code IN ('TIKTOK', 'YOUTUBE', 'FACEBOOK', 'INSTAGRAM')
          AND is_active = TRUE
    ) <> 4 THEN
        RAISE EXCEPTION 'The built-in platform catalog is incomplete or inactive';
    END IF;
END $$;

COMMIT;

SELECT sprint, COUNT(*) AS content_count
FROM contents
GROUP BY sprint
ORDER BY sprint;
