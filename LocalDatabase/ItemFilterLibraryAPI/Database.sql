-- ItemFilterLibrary Local Database Schema
-- SQLite Database for Self-Hosted Template Management

-- Template Types Table
CREATE TABLE IF NOT EXISTS template_types (
    type_id TEXT PRIMARY KEY,
    description TEXT NOT NULL,
    content_type TEXT NOT NULL,
    max_versions INTEGER DEFAULT 10,
    max_size_bytes INTEGER DEFAULT 1048576,
    created_at INTEGER DEFAULT (strftime('%s', 'now')),
    updated_at INTEGER DEFAULT (strftime('%s', 'now'))
);

-- Users Table (simplified local authentication)
CREATE TABLE IF NOT EXISTS users (
    user_id TEXT PRIMARY KEY,
    username TEXT NOT NULL UNIQUE,
    display_name TEXT,
    is_admin BOOLEAN DEFAULT 0,
    created_at INTEGER DEFAULT (strftime('%s', 'now')),
    updated_at INTEGER DEFAULT (strftime('%s', 'now'))
);

-- Templates Table
CREATE TABLE IF NOT EXISTS templates (
    template_id TEXT PRIMARY KEY,
    type_id TEXT NOT NULL,
    user_id TEXT NOT NULL,
    name TEXT NOT NULL,
    is_public BOOLEAN DEFAULT 0,
    is_active BOOLEAN DEFAULT 1,
    version INTEGER DEFAULT 1,
    content_type TEXT NOT NULL,
    version_count INTEGER DEFAULT 1,
    created_at INTEGER DEFAULT (strftime('%s', 'now')),
    updated_at INTEGER DEFAULT (strftime('%s', 'now')),
    FOREIGN KEY (type_id) REFERENCES template_types(type_id),
    FOREIGN KEY (user_id) REFERENCES users(user_id)
);

-- Template Versions Table
CREATE TABLE IF NOT EXISTS template_versions (
    version_id TEXT PRIMARY KEY,
    template_id TEXT NOT NULL,
    version_number INTEGER NOT NULL,
    content TEXT NOT NULL,
    content_compressed BLOB,
    created_at INTEGER DEFAULT (strftime('%s', 'now')),
    FOREIGN KEY (template_id) REFERENCES templates(template_id) ON DELETE CASCADE,
    UNIQUE(template_id, version_number)
);

-- User Sessions Table (for local authentication)
CREATE TABLE IF NOT EXISTS user_sessions (
    session_id TEXT PRIMARY KEY,
    user_id TEXT NOT NULL,
    access_token TEXT NOT NULL,
    refresh_token TEXT NOT NULL,
    access_token_expires INTEGER NOT NULL,
    refresh_token_expires INTEGER NOT NULL,
    created_at INTEGER DEFAULT (strftime('%s', 'now')),
    updated_at INTEGER DEFAULT (strftime('%s', 'now')),
    FOREIGN KEY (user_id) REFERENCES users(user_id)
);

-- Insert default template types
INSERT OR IGNORE INTO template_types (type_id, description, content_type, max_versions, max_size_bytes) VALUES
    ('itemfilterlibrary', 'Item Filter Library', 'application/json', 10, 1048576),
    ('wheresmycraftat', 'Where''s My Craft At', 'application/json', 5, 524288),
    ('reagent', 'ReAgent', 'application/json', 10, 1048576);

-- Insert default admin user
INSERT OR IGNORE INTO users (user_id, username, display_name, is_admin) VALUES
    ('admin', 'admin', 'Administrator', 1);

-- Indexes for performance
CREATE INDEX IF NOT EXISTS idx_templates_type_id ON templates(type_id);
CREATE INDEX IF NOT EXISTS idx_templates_user_id ON templates(user_id);
CREATE INDEX IF NOT EXISTS idx_templates_is_public ON templates(is_public);
CREATE INDEX IF NOT EXISTS idx_templates_updated_at ON templates(updated_at);
CREATE INDEX IF NOT EXISTS idx_template_versions_template_id ON template_versions(template_id);
CREATE INDEX IF NOT EXISTS idx_user_sessions_user_id ON user_sessions(user_id);
CREATE INDEX IF NOT EXISTS idx_user_sessions_access_token ON user_sessions(access_token);

-- Views for easier querying
CREATE VIEW IF NOT EXISTS templates_with_details AS
SELECT 
    t.template_id,
    t.type_id,
    t.user_id,
    t.name,
    t.is_public,
    t.is_active,
    t.version,
    t.content_type,
    t.version_count,
    t.created_at,
    t.updated_at,
    u.display_name as creator_name,
    tt.description as type_description,
    tv.content as latest_content
FROM templates t
JOIN users u ON t.user_id = u.user_id
JOIN template_types tt ON t.type_id = tt.type_id
LEFT JOIN template_versions tv ON t.template_id = tv.template_id AND tv.version_number = t.version
WHERE t.is_active = 1;

-- View for public templates
CREATE VIEW IF NOT EXISTS public_templates AS
SELECT * FROM templates_with_details WHERE is_public = 1; 