-- Read-only verification after schema 01 and migrations 04/05/06.
-- Run with psql --set=ON_ERROR_STOP=1 against the explicitly approved DEV database.
-- This is verification, not a migration. It does not read user/OTP values.
BEGIN READ ONLY;

DO $$
DECLARE
    missing TEXT;
BEGIN
    SELECT string_agg(expected.table_name || '.' || expected.column_name, ', ' ORDER BY expected.table_name, expected.column_name)
    INTO missing
    FROM (VALUES
        ('users', 'user_id', 'bigint'),
        ('users', 'password_hash', 'text'),
        ('users', 'email_verified_at', 'timestamp with time zone'),
        ('users', 'token_version', 'bigint'),
        ('password_reset_requests', 'request_id', 'uuid'),
        ('password_reset_requests', 'purpose', 'text'),
        ('password_reset_requests', 'verifier_version', 'smallint'),
        ('password_reset_requests', 'otp_verifier', 'bytea'),
        ('password_reset_requests', 'invalidated_at', 'timestamp with time zone'),
        ('email_verification_requests', 'request_id', 'uuid'),
        ('email_verification_requests', 'user_id', 'bigint'),
        ('email_verification_requests', 'purpose', 'text'),
        ('email_verification_requests', 'verifier_version', 'smallint'),
        ('email_verification_requests', 'otp_verifier', 'bytea'),
        ('email_verification_requests', 'delivered_at', 'timestamp with time zone'),
        ('user_avatars', 'user_id', 'bigint'),
        ('user_avatars', 'image_data', 'bytea'),
        ('user_avatars', 'content_type', 'text'),
        ('user_avatars', 'width', 'smallint'),
        ('user_avatars', 'height', 'smallint')
    ) AS expected(table_name, column_name, data_type)
    WHERE NOT EXISTS (
        SELECT 1 FROM information_schema.columns actual
        WHERE actual.table_schema = 'public' AND actual.table_name = expected.table_name
          AND actual.column_name = expected.column_name AND actual.data_type = expected.data_type
    );
    IF missing IS NOT NULL THEN
        RAISE EXCEPTION 'Auth schema verification failed: missing or incompatible columns: %', missing;
    END IF;

    SELECT string_agg(expected.name, ', ' ORDER BY expected.name) INTO missing
    FROM (VALUES
        ('password_reset_requests', 'password_reset_requests_verifier_version_check'),
        ('password_reset_requests', 'password_reset_requests_purpose_check'),
        ('email_verification_requests', 'email_verification_requests_purpose_check'),
        ('email_verification_requests', 'email_verification_requests_verifier_version_check'),
        ('user_avatars', 'user_avatars_pkey'),
        ('user_avatars', 'user_avatars_image_data_check'),
        ('users', 'ck_users_token_version_nonnegative')
    ) AS expected(table_name, name)
    WHERE NOT EXISTS (
        SELECT 1 FROM pg_constraint c
        JOIN pg_class t ON t.oid = c.conrelid JOIN pg_namespace n ON n.oid = t.relnamespace
        WHERE n.nspname = 'public' AND t.relname = expected.table_name
          AND c.conname = expected.name AND c.convalidated
    );
    IF missing IS NOT NULL THEN
        RAISE EXCEPTION 'Auth schema verification failed: missing/unvalidated constraints: %', missing;
    END IF;

    SELECT string_agg(expected.name, ', ' ORDER BY expected.name) INTO missing
    FROM (VALUES
        ('ix_password_reset_requests_email_created', false),
        ('uq_password_reset_requests_pending_email', true),
        ('ix_email_verification_requests_email_created', false),
        ('uq_email_verification_requests_pending_email', true)
    ) AS expected(name, must_be_unique)
    WHERE NOT EXISTS (
        SELECT 1 FROM pg_class i JOIN pg_namespace n ON n.oid = i.relnamespace
        JOIN pg_index definition ON definition.indexrelid = i.oid
        WHERE n.nspname = 'public' AND i.relname = expected.name
          AND definition.indisvalid AND definition.indisready
          AND (NOT expected.must_be_unique OR (definition.indisunique AND definition.indpred IS NOT NULL))
    );
    IF missing IS NOT NULL THEN
        RAISE EXCEPTION 'Auth schema verification failed: missing/invalid indexes: %', missing;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_trigger tr
        JOIN pg_class t ON t.oid = tr.tgrelid JOIN pg_namespace n ON n.oid = t.relnamespace
        JOIN pg_proc fn ON fn.oid = tr.tgfoid JOIN pg_namespace fn_ns ON fn_ns.oid = fn.pronamespace
        WHERE n.nspname = 'public' AND t.relname = 'users'
          AND tr.tgname = 'tr_users_auth_token_version' AND NOT tr.tgisinternal
          AND tr.tgenabled IN ('O', 'A') AND tr.tgtype = 19
          AND fn_ns.nspname = 'public' AND fn.proname = 'creatorflow_bump_user_token_version'
          AND fn.pronargs = 0 AND fn.prorettype = 'trigger'::regtype
    ) THEN
        RAISE EXCEPTION 'Auth schema verification failed: token-version function/BEFORE UPDATE row trigger unavailable';
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'users' AND column_name = 'token_version'
          AND is_nullable = 'NO' AND column_default = '0'
    ) THEN
        RAISE EXCEPTION 'Auth schema verification failed: token_version must be NOT NULL with default 0';
    END IF;
END;
$$;

SELECT 'auth_schema_verified' AS status;
COMMIT;
