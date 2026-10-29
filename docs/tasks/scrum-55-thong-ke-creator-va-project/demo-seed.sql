-- SCRUM-55 — Dữ liệu mẫu CHỈ DÙNG CHO DEMO LOCAL.
-- KHÔNG chạy trên DB dùng chung/dev/server. Không phải migration.
--
-- Kịch bản: project "Tết 2026" với 3 Creator đã chốt trong requirements:
--   Minh: 12 content (8 PUBLISHED, 4 đang làm, 1 trong số đó quá hạn)
--   Lan :  9 content (5 PUBLISHED, 4 đang làm, 1 trong số đó quá hạn)
--   Huy :  4 content (1 PUBLISHED, 3 đang làm, 1 trong số đó quá hạn)
--   + 1 content ARCHIVED của Minh (để chứng minh không bị tính vào Total).
-- Tổng project: 25 content / 14 Published / 11 đang làm / 3 Overdue.
--
-- Metrics (snapshot mới nhất mỗi platform):
--   Minh: 120.000 views, 6.000 likes, 400 comments, 300 shares → Engagement 5,58%
--   Lan :  80.000 views, 3.000 likes, 200 comments, 100 shares → Engagement 4,13%
--   Huy :  10.000 views,   200 likes,  20 comments,   5 shares → Engagement 2,25%
--   Toàn project: 210.000 views, 9.200 likes, 620 comments, 405 shares → Engagement 4,87%
--
-- Tài khoản demo (mật khẩu chung): Demo@123
--   an@demo.vn   (An, OWNER)
--   minh@demo.vn (Minh, CREATOR)
--   lan@demo.vn  (Lan, CREATOR)
--   huy@demo.vn  (Huy, CREATOR)

BEGIN;

-- BCrypt hash của "Demo@123"
INSERT INTO users (email, password_hash, display_name, email_verified_at)
VALUES
    ('an@demo.vn',   '$2a$11$jGk1zn.ogDK0jecDbapKQ.JYqB239az5R5200GfoDtAldHsvbEWjq', 'An (Owner)', NOW()),
    ('minh@demo.vn', '$2a$11$jGk1zn.ogDK0jecDbapKQ.JYqB239az5R5200GfoDtAldHsvbEWjq', 'Minh',       NOW()),
    ('lan@demo.vn',  '$2a$11$jGk1zn.ogDK0jecDbapKQ.JYqB239az5R5200GfoDtAldHsvbEWjq', 'Lan',        NOW()),
    ('huy@demo.vn',  '$2a$11$jGk1zn.ogDK0jecDbapKQ.JYqB239az5R5200GfoDtAldHsvbEWjq', 'Huy',        NOW());

INSERT INTO projects (project_name, description, owner_id)
SELECT 'Tết 2026', 'Dữ liệu mẫu SCRUM-55 (demo local)', user_id
FROM users WHERE email = 'an@demo.vn';

INSERT INTO project_members (project_id, user_id, role)
SELECT p.project_id, u.user_id,
       CASE WHEN u.email = 'an@demo.vn' THEN 'OWNER'::project_role ELSE 'CREATOR'::project_role END
FROM projects p
JOIN users u ON u.email IN ('an@demo.vn', 'minh@demo.vn', 'lan@demo.vn', 'huy@demo.vn')
WHERE p.project_name = 'Tết 2026';

-- 24 content "đang sống" (Minh 12, Lan 9, Huy 4): phần đầu PUBLISHED, phần còn lại đang làm,
-- các content cuối cùng có deadline đã qua để thành Overdue.
WITH cfg AS (
    SELECT u.user_id, v.total, v.published, v.overdue
    FROM users u
    JOIN (VALUES
            ('minh@demo.vn', 12, 8, 1),
            ('lan@demo.vn',   9, 5, 1),
            ('huy@demo.vn',   4, 1, 1)
         ) AS v(email, total, published, overdue) ON u.email = v.email
),
numbered AS (
    SELECT cfg.*, gs AS idx
    FROM cfg CROSS JOIN LATERAL generate_series(1, cfg.total) AS gs
)
INSERT INTO contents (project_id, title, status, content_type, priority, deadline, created_by, sprint)
SELECT p.project_id,
       'Tết 2026 - ' || split_part(u.email, '@', 1) || ' #' || n.idx,
       CASE WHEN n.idx <= n.published THEN 'PUBLISHED'::content_status
            ELSE (ARRAY['SCRIPT','PRODUCTION','EDITING','REVIEW','READY'])[1 + ((n.idx - n.published - 1) % 5)]::content_status
       END,
       'Video',
       'MEDIUM'::content_priority,
       CASE WHEN n.idx <= n.published THEN (CURRENT_DATE - 10)::timestamptz
            WHEN n.idx > n.total - n.overdue THEN (CURRENT_DATE - 5)::timestamptz
            ELSE (CURRENT_DATE + 7)::timestamptz
       END,
       n.user_id,
       'Sprint 26'
FROM numbered n
JOIN users u ON u.user_id = n.user_id
CROSS JOIN projects p
WHERE p.project_name = 'Tết 2026';

-- 1 content ARCHIVED của Minh: phải KHÔNG xuất hiện trong Total/Published và metrics.
INSERT INTO contents (project_id, title, status, content_type, priority, deadline, created_by, sprint)
SELECT p.project_id, 'Tết 2026 - minh (đã lưu trữ)', 'ARCHIVED'::content_status, 'Video',
       'LOW'::content_priority, NULL, u.user_id, 'Sprint 25'
FROM projects p JOIN users u ON u.email = 'minh@demo.vn'
WHERE p.project_name = 'Tết 2026';

-- Mỗi content PUBLISHED đăng 1 platform (TikTok) để có chỗ gắn metrics.
INSERT INTO content_platforms (content_id, platform_id, publication_status, post_url, planned_publish_at, published_at)
SELECT c.content_id,
       (SELECT platform_id FROM platforms WHERE code = 'TIKTOK'),
       'PUBLISHED'::publication_status,
       'https://example.com/tet2026/' || c.content_id,
       c.deadline,
       c.deadline
FROM contents c
JOIN projects p ON p.project_id = c.project_id
WHERE p.project_name = 'Tết 2026' AND c.status = 'PUBLISHED';

-- Snapshot metrics đầu tiên cho từng content PUBLISHED.
WITH published AS (
    SELECT c.content_id, u.email,
           ROW_NUMBER() OVER (PARTITION BY c.created_by ORDER BY c.content_id) AS rn
    FROM contents c
    JOIN projects p ON p.project_id = c.project_id
    JOIN users u ON u.user_id = c.created_by
    WHERE p.project_name = 'Tết 2026' AND c.status = 'PUBLISHED'
)
INSERT INTO content_metrics (content_platform_id, views, likes, comments, shares, recorded_by)
SELECT cp.content_platform_id,
       CASE pub.email WHEN 'minh@demo.vn' THEN 15000 WHEN 'lan@demo.vn' THEN 16000 ELSE 10000 END,
       CASE pub.email WHEN 'minh@demo.vn' THEN   750 WHEN 'lan@demo.vn' THEN   600 ELSE   200 END,
       CASE pub.email WHEN 'minh@demo.vn' THEN    50 WHEN 'lan@demo.vn' THEN    40 ELSE    20 END,
       CASE pub.email WHEN 'minh@demo.vn' THEN    37 + (pub.rn % 2) WHEN 'lan@demo.vn' THEN 20 ELSE 5 END,
       (SELECT user_id FROM users WHERE email = pub.email)
FROM published pub
JOIN content_platforms cp ON cp.content_id = pub.content_id;

COMMIT;

-- ---------------------------------------------------------------------------------------------
-- Project thứ hai để chứng minh "đổi Project thì số đổi theo" và số của Minh KHÔNG bị cộng dồn:
-- "Hè 2026" có Minh 3 content (2 PUBLISHED, 1 đang làm). Metrics 30.000 views.
-- Chạy phần này SAU khi đã COMMIT phần trên (idempotent: bỏ qua nếu project đã tồn tại).
-- ---------------------------------------------------------------------------------------------
BEGIN;

INSERT INTO projects (project_name, description, owner_id)
SELECT 'Hè 2026', 'Project thứ hai cho demo SCRUM-55', user_id
FROM users WHERE email = 'an@demo.vn' AND NOT EXISTS (SELECT 1 FROM projects WHERE project_name = 'Hè 2026');

INSERT INTO project_members (project_id, user_id, role)
SELECT p.project_id, u.user_id,
       CASE WHEN u.email = 'an@demo.vn' THEN 'OWNER'::project_role ELSE 'CREATOR'::project_role END
FROM projects p
JOIN users u ON u.email IN ('an@demo.vn', 'minh@demo.vn')
WHERE p.project_name = 'Hè 2026'
ON CONFLICT (project_id, user_id) DO NOTHING;

INSERT INTO contents (project_id, title, status, content_type, priority, deadline, created_by, sprint)
SELECT p.project_id, 'Hè 2026 - minh #' || gs,
       CASE WHEN gs <= 2 THEN 'PUBLISHED'::content_status ELSE 'EDITING'::content_status END,
       'Video', 'MEDIUM'::content_priority, (CURRENT_DATE + 10)::timestamptz, u.user_id, 'Sprint 26'
FROM projects p
JOIN users u ON u.email = 'minh@demo.vn'
CROSS JOIN generate_series(1, 3) gs
WHERE p.project_name = 'Hè 2026'
  AND NOT EXISTS (SELECT 1 FROM contents c WHERE c.project_id = p.project_id);

INSERT INTO content_platforms (content_id, platform_id, publication_status, published_at)
SELECT c.content_id, (SELECT platform_id FROM platforms WHERE code = 'YOUTUBE'),
       'PUBLISHED'::publication_status, c.deadline
FROM contents c
JOIN projects p ON p.project_id = c.project_id
WHERE p.project_name = 'Hè 2026' AND c.status = 'PUBLISHED'
  AND NOT EXISTS (SELECT 1 FROM content_platforms cp WHERE cp.content_id = c.content_id);

INSERT INTO content_metrics (content_platform_id, views, likes, comments, shares, recorded_by)
SELECT cp.content_platform_id, 15000, 450, 30, 20, u.user_id
FROM content_platforms cp
JOIN contents c ON c.content_id = cp.content_id
JOIN projects p ON p.project_id = c.project_id
JOIN users u ON u.email = 'minh@demo.vn'
WHERE p.project_name = 'Hè 2026' AND c.status = 'PUBLISHED'
  AND NOT EXISTS (SELECT 1 FROM content_metrics m WHERE m.content_platform_id = cp.content_platform_id);

COMMIT;

-- Đối chiếu nhanh sau khi seed (phải khớp bảng thống kê trên app):
--   Minh 12 / 4 đang làm / 8 published / 1 overdue, 120000 views
--   Lan   9 / 4 đang làm / 5 published / 1 overdue,  80000 views
--   Huy   4 / 3 đang làm / 1 published / 1 overdue,  10000 views
--   Project 25 / 11 / 14 / 3, 210000 views
SELECT u.display_name,
       COUNT(c.content_id)                                                   AS total,
       COUNT(*) FILTER (WHERE c.status IN ('SCRIPT','PRODUCTION','EDITING','REVIEW','READY')) AS in_progress,
       COUNT(*) FILTER (WHERE c.status = 'PUBLISHED')                        AS published,
       COUNT(*) FILTER (WHERE c.status NOT IN ('PUBLISHED','ARCHIVED')
                          AND c.deadline < CURRENT_DATE + 1)                 AS overdue_raw
FROM contents c
JOIN projects p ON p.project_id = c.project_id
JOIN users u ON u.user_id = c.created_by
WHERE p.project_name = 'Tết 2026' AND c.status <> 'ARCHIVED'
GROUP BY u.display_name
ORDER BY total DESC;
