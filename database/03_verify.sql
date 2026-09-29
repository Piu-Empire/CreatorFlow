-- CreatorFlow - Verification
-- 03_verify.sql
-- Run after 01_schema.sql and 02_seed.sql

-- 1. Table count: expected 22
SELECT COUNT(*) AS table_count
FROM information_schema.tables
WHERE table_schema='public'
  AND table_type='BASE TABLE';

-- 2. List tables
SELECT table_name
FROM information_schema.tables
WHERE table_schema='public'
  AND table_type='BASE TABLE'
ORDER BY table_name;

-- 3. Seed data
SELECT user_id,email,display_name,account_status,is_system_admin
FROM users
ORDER BY user_id;

SELECT project_id,project_name,owner_id,status
FROM projects
ORDER BY project_id;

SELECT
  pm.project_member_id,
  p.project_name,
  u.email,
  pm.role,
  pm.is_active
FROM project_members pm
JOIN projects p ON p.project_id=pm.project_id
JOIN users u ON u.user_id=pm.user_id
ORDER BY pm.project_member_id;

SELECT platform_id,code,name,is_active
FROM platforms
ORDER BY platform_id;

SELECT plan_id,code,name,price_monthly,max_projects,max_members,ai_request_limit
FROM plans
ORDER BY plan_id;

SELECT idea_id,project_id,title,status,created_by
FROM ideas
ORDER BY idea_id;

SELECT
  content_id,project_id,source_idea_id,title,priority,status,
  deadline,planned_publish_at,created_by
FROM contents
ORDER BY content_id;

SELECT
  cp.content_platform_id,
  c.title AS content_title,
  p.name AS platform_name,
  cp.publication_status,
  cp.planned_publish_at,
  cp.published_at
FROM content_platforms cp
JOIN contents c ON c.content_id=cp.content_id
JOIN platforms p ON p.platform_id=cp.platform_id
ORDER BY cp.content_platform_id;

SELECT
  ca.assignment_id,
  c.title AS content_title,
  assignee.email AS assignee,
  assigner.email AS assigned_by,
  ca.status,
  ca.progress_percent,
  ca.deadline
FROM content_assignments ca
JOIN contents c ON c.content_id=ca.content_id
JOIN users assignee ON assignee.user_id=ca.assignee_id
JOIN users assigner ON assigner.user_id=ca.assigned_by
ORDER BY ca.assignment_id;

SELECT
  h.history_id,
  c.title AS content_title,
  h.from_status,
  h.to_status,
  u.email AS changed_by,
  h.note,
  h.changed_at
FROM content_status_history h
JOIN contents c ON c.content_id=h.content_id
LEFT JOIN users u ON u.user_id=h.changed_by
ORDER BY h.history_id;

-- 4. Foreign keys
SELECT
  tc.table_name,
  kcu.column_name,
  ccu.table_name AS foreign_table_name,
  ccu.column_name AS foreign_column_name
FROM information_schema.table_constraints tc
JOIN information_schema.key_column_usage kcu
  ON tc.constraint_name=kcu.constraint_name
 AND tc.table_schema=kcu.table_schema
JOIN information_schema.constraint_column_usage ccu
  ON ccu.constraint_name=tc.constraint_name
 AND ccu.table_schema=tc.table_schema
WHERE tc.constraint_type='FOREIGN KEY'
  AND tc.table_schema='public'
ORDER BY tc.table_name,kcu.column_name;

-- 5. Enums
SELECT
  t.typname AS enum_name,
  e.enumlabel AS enum_value
FROM pg_type t
JOIN pg_enum e ON t.oid=e.enumtypid
JOIN pg_catalog.pg_namespace n ON n.oid=t.typnamespace
WHERE n.nspname='public'
ORDER BY t.typname,e.enumsortorder;

-- 6. Indexes
SELECT indexname,indexdef
FROM pg_indexes
WHERE schemaname='public'
ORDER BY indexname;

-- 7. Dev data summary
SELECT
  (SELECT COUNT(*) FROM users) AS users,
  (SELECT COUNT(*) FROM projects) AS projects,
  (SELECT COUNT(*) FROM project_members) AS project_members,
  (SELECT COUNT(*) FROM ideas) AS ideas,
  (SELECT COUNT(*) FROM contents) AS contents,
  (SELECT COUNT(*) FROM content_assignments) AS assignments,
  (SELECT COUNT(*) FROM content_status_history) AS status_history,
  (SELECT COUNT(*) FROM platforms) AS platforms,
  (SELECT COUNT(*) FROM plans) AS plans;

-- ============================================================
-- NEGATIVE TESTS
-- Run ONE block at a time.
-- If PostgreSQL returns the expected ERROR, the test passes.
-- ============================================================

-- A. Duplicate email, case-insensitive
-- BEGIN;
-- INSERT INTO users(email,password_hash,display_name)
-- VALUES ('OWNER@CREATORFLOW.DEV','x','Duplicate Owner');
-- ROLLBACK;
-- Expected: duplicate key error

-- B. Duplicate project member
-- BEGIN;
-- INSERT INTO project_members(project_id,user_id,role)
-- SELECT p.project_id,u.user_id,'CREATOR'::project_role
-- FROM projects p
-- JOIN users u ON u.email='creator@creatorflow.dev'
-- WHERE p.project_name='CreatorFlow Demo Project';
-- ROLLBACK;
-- Expected: duplicate key error

-- C. Duplicate content-platform
-- BEGIN;
-- INSERT INTO content_platforms(content_id,platform_id)
-- SELECT c.content_id,pf.platform_id
-- FROM contents c
-- JOIN platforms pf ON pf.code='TIKTOK'
-- WHERE c.title='TikTok gioi thieu CreatorFlow';
-- ROLLBACK;
-- Expected: duplicate key error

-- D. Invitation cannot assign OWNER
-- BEGIN;
-- INSERT INTO project_invitations(
--   project_id,invited_email,invited_by,role,token,expires_at
-- )
-- SELECT
--   p.project_id,
--   'new-owner@example.com',
--   u.user_id,
--   'OWNER'::project_role,
--   'TEST_OWNER_INVITE_TOKEN',
--   CURRENT_TIMESTAMP + INTERVAL '7 days'
-- FROM projects p
-- JOIN users u ON u.email='owner@creatorflow.dev'
-- WHERE p.project_name='CreatorFlow Demo Project';
-- ROLLBACK;
-- Expected: check constraint error

-- E. Progress cannot exceed 100
-- BEGIN;
-- UPDATE content_assignments
-- SET progress_percent=101
-- WHERE assignment_id=(SELECT MIN(assignment_id) FROM content_assignments);
-- ROLLBACK;
-- Expected: check constraint error

-- F. Metrics cannot be negative
-- BEGIN;
-- INSERT INTO content_metrics(content_platform_id,views,likes,comments,shares)
-- SELECT MIN(content_platform_id),-1,0,0,0
-- FROM content_platforms;
-- ROLLBACK;
-- Expected: check constraint error

-- G. Read notification must have read_at
-- BEGIN;
-- INSERT INTO notifications(
--   recipient_user_id,project_id,type,title,message,is_read,read_at
-- )
-- SELECT
--   u.user_id,
--   p.project_id,
--   'SYSTEM'::notification_type,
--   'Constraint test',
--   'Notification test',
--   TRUE,
--   NULL
-- FROM users u
-- CROSS JOIN projects p
-- WHERE u.email='creator@creatorflow.dev'
--   AND p.project_name='CreatorFlow Demo Project';
-- ROLLBACK;