-- Taskly Database Schema for Supabase
-- This schema creates tables for managing Epics, Stories, Tasks, Bugs, and Sprints

-- Enable UUID extension
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Enum types
CREATE TYPE issue_type AS ENUM ('Epic', 'Story', 'Task', 'Bug');
CREATE TYPE item_status AS ENUM ('Backlog', 'Todo', 'InProgress', 'Review', 'Done');

-- Backlog Items Table (Epics, Stories, Tasks, Bugs)
CREATE TABLE backlog_items (
    id SERIAL PRIMARY KEY,
    title VARCHAR(255) NOT NULL,
    description TEXT,
    story_points INTEGER NOT NULL DEFAULT 0,
    priority INTEGER NOT NULL DEFAULT 0,
    status item_status NOT NULL DEFAULT 'Backlog',
    type issue_type NOT NULL,
    sprint_id INTEGER,
    parent_id INTEGER,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    FOREIGN KEY (parent_id) REFERENCES backlog_items(id) ON DELETE CASCADE
);

-- Sprints Table
CREATE TABLE sprints (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    start_date TIMESTAMP WITH TIME ZONE NOT NULL,
    end_date TIMESTAMP WITH TIME ZONE NOT NULL,
    goal TEXT,
    item_ids INTEGER[] DEFAULT '{}',  -- Array of backlog item IDs
    is_archived BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Developers Table (for future use)
CREATE TABLE developers (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    capacity INTEGER NOT NULL DEFAULT 40,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Indexes for better query performance
CREATE INDEX idx_backlog_items_type ON backlog_items(type);
CREATE INDEX idx_backlog_items_status ON backlog_items(status);
CREATE INDEX idx_backlog_items_sprint_id ON backlog_items(sprint_id);
CREATE INDEX idx_backlog_items_parent_id ON backlog_items(parent_id);
CREATE INDEX idx_sprints_is_archived ON sprints(is_archived);

-- Function to update the updated_at timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Triggers to automatically update updated_at
CREATE TRIGGER update_backlog_items_updated_at
    BEFORE UPDATE ON backlog_items
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_sprints_updated_at
    BEFORE UPDATE ON sprints
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_developers_updated_at
    BEFORE UPDATE ON developers
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Add foreign key constraint for sprint_id in backlog_items
ALTER TABLE backlog_items
ADD CONSTRAINT fk_backlog_items_sprint
FOREIGN KEY (sprint_id) REFERENCES sprints(id) ON DELETE SET NULL;
