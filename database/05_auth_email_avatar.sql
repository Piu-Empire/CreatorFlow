-- SCRUM-19/20/21. Review and approve the target environment before applying.
-- Prerequisites: 01_schema.sql and 04_password_reset_requests.sql.
-- Option B: existing accounts remain unverified until they prove email ownership.
BEGIN;

ALTER TABLE users ADD COLUMN email_verified_at TIMESTAMPTZ;

-- Verifier v1 cannot be upgraded without its plaintext OTP. Preserve history/quota.
UPDATE password_reset_requests SET invalidated_at = clock_timestamp()
WHERE verifier_version = 1 AND consumed_at IS NULL AND invalidated_at IS NULL;
ALTER TABLE password_reset_requests DROP CONSTRAINT password_reset_requests_verifier_version_check;
ALTER TABLE password_reset_requests
    ALTER COLUMN verifier_version SET DEFAULT 2,
    ADD CONSTRAINT password_reset_requests_verifier_version_check CHECK
        (verifier_version = 2 OR (verifier_version = 1 AND
            (consumed_at IS NOT NULL OR invalidated_at IS NOT NULL))),
    ADD COLUMN purpose TEXT NOT NULL DEFAULT 'PASSWORD_RESET'
        CHECK (purpose = 'PASSWORD_RESET');

CREATE TABLE email_verification_requests (
    request_id UUID PRIMARY KEY,
    user_id BIGINT NOT NULL REFERENCES users(user_id) ON DELETE RESTRICT,
    email_normalized VARCHAR(255) NOT NULL,
    purpose TEXT NOT NULL DEFAULT 'EMAIL_VERIFICATION' CHECK (purpose = 'EMAIL_VERIFICATION'),
    verifier_version SMALLINT NOT NULL DEFAULT 2 CHECK (verifier_version = 2),
    otp_verifier BYTEA NOT NULL CHECK (octet_length(otp_verifier) = 32),
    created_at TIMESTAMPTZ NOT NULL,
    expires_at TIMESTAMPTZ NOT NULL,
    failed_attempts SMALLINT NOT NULL DEFAULT 0 CHECK (failed_attempts BETWEEN 0 AND 5),
    delivered_at TIMESTAMPTZ,
    consumed_at TIMESTAMPTZ,
    invalidated_at TIMESTAMPTZ,
    CHECK (expires_at = created_at + INTERVAL '10 minutes'),
    CHECK (delivered_at IS NULL OR delivered_at >= created_at),
    CHECK (consumed_at IS NULL OR (delivered_at IS NOT NULL AND invalidated_at IS NULL)),
    CHECK (consumed_at IS NULL OR consumed_at >= created_at),
    CHECK (invalidated_at IS NULL OR invalidated_at >= created_at)
);
CREATE INDEX ix_email_verification_requests_email_created
    ON email_verification_requests(email_normalized, created_at DESC);
CREATE UNIQUE INDEX uq_email_verification_requests_pending_email
    ON email_verification_requests(email_normalized)
    WHERE consumed_at IS NULL AND invalidated_at IS NULL;

-- bytea is practical without a backend/storage service, not optimal production storage.
CREATE TABLE user_avatars (
    user_id BIGINT PRIMARY KEY REFERENCES users(user_id) ON DELETE RESTRICT,
    image_data BYTEA NOT NULL CHECK (octet_length(image_data) BETWEEN 1 AND 2097152),
    content_type TEXT NOT NULL DEFAULT 'image/png' CHECK (content_type = 'image/png'),
    width SMALLINT NOT NULL CHECK (width BETWEEN 1 AND 512),
    height SMALLINT NOT NULL CHECK (height BETWEEN 1 AND 512),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);
COMMIT;
