-- Approved Forgot/Reset Password migration. Apply only to an approved database.
-- Run after 01_schema.sql; never applied automatically by the application.
BEGIN;

CREATE TABLE password_reset_requests (
    request_id UUID PRIMARY KEY,
    email_normalized VARCHAR(255) NOT NULL,
    user_id BIGINT REFERENCES users(user_id) ON DELETE RESTRICT,
    verifier_version SMALLINT NOT NULL DEFAULT 1 CHECK (verifier_version = 1),
    otp_verifier BYTEA NOT NULL CHECK (octet_length(otp_verifier) = 32),
    created_at TIMESTAMPTZ NOT NULL,
    expires_at TIMESTAMPTZ NOT NULL,
    failed_attempts SMALLINT NOT NULL DEFAULT 0 CHECK (failed_attempts BETWEEN 0 AND 5),
    delivered_at TIMESTAMPTZ,
    consumed_at TIMESTAMPTZ,
    invalidated_at TIMESTAMPTZ,
    CHECK (expires_at = created_at + INTERVAL '10 minutes'),
    CHECK (delivered_at IS NULL OR delivered_at >= created_at),
    CHECK (consumed_at IS NULL OR
        (delivered_at IS NOT NULL AND user_id IS NOT NULL AND invalidated_at IS NULL)),
    CHECK (consumed_at IS NULL OR consumed_at >= created_at),
    CHECK (invalidated_at IS NULL OR invalidated_at >= created_at)
);

CREATE INDEX ix_password_reset_requests_email_created
    ON password_reset_requests(email_normalized, created_at DESC);
CREATE UNIQUE INDEX uq_password_reset_requests_pending_email
    ON password_reset_requests(email_normalized)
    WHERE consumed_at IS NULL AND invalidated_at IS NULL;

COMMIT;
