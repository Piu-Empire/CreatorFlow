-- Apply only after review, backup and approval of the target database.
-- Prerequisites: schema 01 and migrations 04/05. Not applied by the app.
BEGIN;

ALTER TABLE users
    ADD COLUMN token_version BIGINT NOT NULL DEFAULT 0,
    ADD CONSTRAINT ck_users_token_version_nonnegative CHECK (token_version >= 0);

CREATE FUNCTION creatorflow_bump_user_token_version()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    IF NEW.password_hash IS DISTINCT FROM OLD.password_hash
       OR NEW.account_status IS DISTINCT FROM OLD.account_status
       OR NEW.email IS DISTINCT FROM OLD.email
       OR NEW.email_verified_at IS DISTINCT FROM OLD.email_verified_at
       OR NEW.is_system_admin IS DISTINCT FROM OLD.is_system_admin THEN
        NEW.token_version := OLD.token_version + 1;
    ELSE
        NEW.token_version := OLD.token_version;
    END IF;
    RETURN NEW;
END;
$$;

CREATE TRIGGER tr_users_auth_token_version
BEFORE UPDATE ON users
FOR EACH ROW EXECUTE FUNCTION creatorflow_bump_user_token_version();

COMMIT;
