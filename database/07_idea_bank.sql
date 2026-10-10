-- SCRUM Idea Bank: tag, ghi chú cho Idea. Apply only after review, backup and approval of the target database.
-- Prerequisites: schema 01 (bảng ideas, tags). Not applied by the app.
BEGIN;

-- Ghi chú nội bộ của Idea (tách khỏi description là mô tả ý tưởng).
ALTER TABLE ideas ADD COLUMN IF NOT EXISTS note TEXT;

-- Tag của Idea dùng chung bảng tags (theo Project) với Content: UNIQUE(project_id, name) đã có sẵn.
-- Việc tag phải cùng Project với Idea do IdeaRepository đảm bảo (chỉ gắn tag của project_id của Idea).
CREATE TABLE IF NOT EXISTS idea_tags (
    idea_id BIGINT NOT NULL REFERENCES ideas(idea_id) ON DELETE CASCADE,
    tag_id  BIGINT NOT NULL REFERENCES tags(tag_id)   ON DELETE CASCADE,
    PRIMARY KEY (idea_id, tag_id)
);

CREATE INDEX IF NOT EXISTS ix_idea_tags_tag ON idea_tags(tag_id);
CREATE INDEX IF NOT EXISTS ix_ideas_project_updated ON ideas(project_id, updated_at DESC);

COMMIT;
