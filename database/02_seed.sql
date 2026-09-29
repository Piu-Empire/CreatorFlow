-- CreatorFlow - Development Seed Data
-- 02_seed.sql
-- Run after 01_schema.sql

BEGIN;

INSERT INTO platforms(code,name) VALUES
('TIKTOK','TikTok'),
('YOUTUBE','YouTube'),
('FACEBOOK','Facebook'),
('INSTAGRAM','Instagram');

INSERT INTO plans(code,name,description,price_monthly,max_projects,max_members,ai_request_limit) VALUES
('FREE','Free','Goi mien phi cho Creator ca nhan',0,2,5,30),
('PRO','Pro','Goi nang cao cho Creator/nhom nho',199000,10,20,500),
('TEAM','Team','Goi cho Creator Team/Agency',499000,NULL,NULL,2000);

-- DEV ONLY: password_hash values below are placeholders.
INSERT INTO users(email,password_hash,display_name,is_system_admin) VALUES
('owner@creatorflow.dev','DEV_HASH_OWNER','Owner Demo',FALSE),
('manager@creatorflow.dev','DEV_HASH_MANAGER','Manager Demo',FALSE),
('creator@creatorflow.dev','DEV_HASH_CREATOR','Creator Demo',FALSE),
('admin@creatorflow.dev','DEV_HASH_ADMIN','System Admin Demo',TRUE);

INSERT INTO projects(project_name,description,owner_id)
SELECT
  'CreatorFlow Demo Project',
  'Project mau dung cho phat trien va kiem thu',
  user_id
FROM users
WHERE email='owner@creatorflow.dev';

INSERT INTO project_members(project_id,user_id,role)
SELECT p.project_id,u.user_id,'OWNER'::project_role
FROM projects p
JOIN users u ON u.email='owner@creatorflow.dev'
WHERE p.project_name='CreatorFlow Demo Project';

INSERT INTO project_members(project_id,user_id,role)
SELECT p.project_id,u.user_id,'MANAGER'::project_role
FROM projects p
JOIN users u ON u.email='manager@creatorflow.dev'
WHERE p.project_name='CreatorFlow Demo Project';

INSERT INTO project_members(project_id,user_id,role)
SELECT p.project_id,u.user_id,'CREATOR'::project_role
FROM projects p
JOIN users u ON u.email='creator@creatorflow.dev'
WHERE p.project_name='CreatorFlow Demo Project';

INSERT INTO tags(project_id,name)
SELECT project_id,'Marketing'
FROM projects
WHERE project_name='CreatorFlow Demo Project';

INSERT INTO tags(project_id,name)
SELECT project_id,'Short-form'
FROM projects
WHERE project_name='CreatorFlow Demo Project';

INSERT INTO ideas(project_id,title,description,status,created_by)
SELECT
  p.project_id,
  'Series TikTok CreatorFlow',
  'Y tuong demo de kiem thu Idea -> Content',
  'BACKLOG'::idea_status,
  u.user_id
FROM projects p
JOIN users u ON u.email='manager@creatorflow.dev'
WHERE p.project_name='CreatorFlow Demo Project';

INSERT INTO contents(
  project_id,source_idea_id,title,description,script,content_type,
  priority,status,deadline,planned_publish_at,created_by
)
SELECT
  p.project_id,
  i.idea_id,
  'TikTok gioi thieu CreatorFlow',
  'Content mau dung cho Board, Assignment, Review, Calendar va Metrics',
  'Hook -> Gioi thieu van de -> Demo CreatorFlow -> CTA',
  'SHORT_VIDEO',
  'HIGH'::content_priority,
  'SCRIPT'::content_status,
  CURRENT_TIMESTAMP + INTERVAL '7 days',
  CURRENT_TIMESTAMP + INTERVAL '10 days',
  u.user_id
FROM projects p
JOIN ideas i ON i.project_id=p.project_id
            AND i.title='Series TikTok CreatorFlow'
JOIN users u ON u.email='manager@creatorflow.dev'
WHERE p.project_name='CreatorFlow Demo Project';

INSERT INTO content_platforms(
  content_id,platform_id,publication_status,planned_publish_at
)
SELECT
  c.content_id,
  pf.platform_id,
  'PLANNED'::publication_status,
  c.planned_publish_at
FROM contents c
JOIN projects p ON p.project_id=c.project_id
JOIN platforms pf ON pf.code='TIKTOK'
WHERE p.project_name='CreatorFlow Demo Project'
  AND c.title='TikTok gioi thieu CreatorFlow';

INSERT INTO content_tags(content_id,tag_id)
SELECT c.content_id,t.tag_id
FROM contents c
JOIN tags t ON t.project_id=c.project_id
           AND t.name='Marketing'
JOIN projects p ON p.project_id=c.project_id
WHERE p.project_name='CreatorFlow Demo Project'
  AND c.title='TikTok gioi thieu CreatorFlow';

INSERT INTO content_assignments(
  content_id,assignee_id,assigned_by,status,progress_percent,deadline
)
SELECT
  c.content_id,
  creator.user_id,
  manager.user_id,
  'ASSIGNED'::assignment_status,
  0,
  c.deadline
FROM contents c
JOIN projects p ON p.project_id=c.project_id
JOIN users creator ON creator.email='creator@creatorflow.dev'
JOIN users manager ON manager.email='manager@creatorflow.dev'
WHERE p.project_name='CreatorFlow Demo Project'
  AND c.title='TikTok gioi thieu CreatorFlow';

INSERT INTO content_status_history(
  content_id,from_status,to_status,changed_by,note
)
SELECT
  c.content_id,
  'IDEA'::content_status,
  'SCRIPT'::content_status,
  u.user_id,
  'Du lieu mau khoi tao de test Workflow'
FROM contents c
JOIN projects p ON p.project_id=c.project_id
JOIN users u ON u.email='manager@creatorflow.dev'
WHERE p.project_name='CreatorFlow Demo Project'
  AND c.title='TikTok gioi thieu CreatorFlow';

COMMIT;
