-- SCRUM-50 - Notification nghiep vu: chong tao thong bao trung lap (dedupe).
-- Apply once after database/08_platform_catalog_and_sprint.sql.
--
-- Migration nay KHONG duoc ung dung tu chay. Chay thu cong theo docs/shared-dev-test-runbook.md
-- sau khi review, backup va duyet dung target DB.
--
-- THAY DOI SCHEMA (ghi ro theo yeu cau SCRUM-50):
--   1) notifications: THEM cot dedupe_key VARCHAR(200) NULL.
--      - Vi sao: can mot khoa nghiep vu de chong tao trung vo han khi cung mot su kien bi ghi lap
--        (vi du: quet lai thong bao deadline moi lan nguoi dung mo danh sach thong bao).
--      - Vi sao NULLable: bang dang co du lieu cu; cac dong cu se co dedupe_key = NULL va khong bi anh huong.
--      - Dinh dang khoa do CreatorFlow.Api tao: TYPE:contentId:recipientUserId:eventKey
--        vi du: ASSIGNMENT:12:7:305, DEADLINE_NEAR:12:7:2026-10-19, REVIEW_APPROVED:12:2:5:3
--        (co recipientUserId vi mot su kien co the co nhieu nguoi nhan).
--   2) UNIQUE INDEX co dieu kien ux_notifications_dedupe_key tren (dedupe_key) WHERE dedupe_key IS NOT NULL.
--      - Vi sao co dieu kien: khong index va khong chan nhieu dong NULL cua du lieu cu.
--   Khong backfill du lieu cu, khong doi kieu/ten cot cu, khong doi rang buoc CHECK hien co.
--
-- CANH BAO: moi lan sua file nay phai CHAY LAI tren cac moi truong da co du lieu
--   (script idempotent nho ADD COLUMN IF NOT EXISTS / CREATE INDEX IF NOT EXISTS).
--
-- ROLLBACK (chay thu cong khi can thiet):
--   DROP INDEX IF EXISTS ux_notifications_dedupe_key;
--   ALTER TABLE notifications DROP COLUMN IF EXISTS dedupe_key;
BEGIN;

ALTER TABLE notifications
    ADD COLUMN IF NOT EXISTS dedupe_key VARCHAR(200);

CREATE UNIQUE INDEX IF NOT EXISTS ux_notifications_dedupe_key
    ON notifications(dedupe_key)
    WHERE dedupe_key IS NOT NULL;

-- Verify the schema in the same transaction so a mismatch rolls back the migration.
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'notifications'
          AND column_name = 'dedupe_key'
          AND data_type = 'character varying'
          AND character_maximum_length = 200
          AND is_nullable = 'YES'
    ) THEN
        RAISE EXCEPTION 'notifications.dedupe_key is missing or does not match the expected schema';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM pg_indexes
        WHERE schemaname = 'public'
          AND tablename = 'notifications'
          AND indexname = 'ux_notifications_dedupe_key'
    ) THEN
        RAISE EXCEPTION 'unique index ux_notifications_dedupe_key on notifications.dedupe_key is missing';
    END IF;
END $$;

COMMIT;

SELECT COUNT(*) AS notifications_without_dedupe_key
FROM notifications
WHERE dedupe_key IS NULL;
