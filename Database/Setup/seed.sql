-- Taskly Sample Seed Data
-- This file provides sample data for testing the Taskly application

-- Insert sample Epics
INSERT INTO backlog_items (id, title, description, story_points, priority, status, type, parent_id)
VALUES
    (1, 'User Authentication System', 'Implement complete user authentication and authorization', 21, 1, 'Backlog', 'Epic', NULL),
    (2, 'Task Management Features', 'Core task management functionality for the application', 13, 2, 'Backlog', 'Epic', NULL),
    (3, 'Reporting Dashboard', 'Analytics and reporting dashboard for project metrics', 8, 3, 'Backlog', 'Epic', NULL);

-- Insert sample Stories under Epics
INSERT INTO backlog_items (id, title, description, story_points, priority, status, type, parent_id)
VALUES
    -- Stories for Epic 1 (User Authentication)
    (4, 'User Login', 'Allow users to log in with email and password', 5, 1, 'Backlog', 'Story', 1),
    (5, 'User Registration', 'Allow new users to create an account', 5, 2, 'Backlog', 'Story', 1),
    (6, 'Password Reset', 'Enable users to reset forgotten passwords', 3, 3, 'Backlog', 'Story', 1),

    -- Stories for Epic 2 (Task Management)
    (7, 'Create Tasks', 'Users can create new tasks with title and description', 3, 1, 'Backlog', 'Story', 2),
    (8, 'Edit Tasks', 'Users can edit existing task details', 2, 2, 'Backlog', 'Story', 2),
    (9, 'Delete Tasks', 'Users can delete tasks they own', 2, 3, 'Backlog', 'Story', 2),

    -- Stories for Epic 3 (Reporting)
    (10, 'Sprint Velocity Chart', 'Display velocity chart showing completed story points per sprint', 5, 1, 'Backlog', 'Story', 3),
    (11, 'Burndown Chart', 'Show burndown chart for active sprint', 3, 2, 'Backlog', 'Story', 3);

-- Insert sample Tasks and Bugs under Stories
INSERT INTO backlog_items (id, title, description, story_points, priority, status, type, parent_id)
VALUES
    -- Tasks for Story 4 (User Login)
    (12, 'Create login form UI', 'Design and implement the login form interface', 2, 1, 'Backlog', 'Task', 4),
    (13, 'Implement authentication API', 'Backend API endpoint for user authentication', 3, 2, 'Backlog', 'Task', 4),

    -- Tasks for Story 5 (User Registration)
    (14, 'Create registration form', 'Design and implement registration form', 2, 1, 'Backlog', 'Task', 5),
    (15, 'Add email validation', 'Validate email format and check for duplicates', 2, 2, 'Backlog', 'Task', 5),
    (16, 'Fix password strength validation', 'Password validation not working correctly', 1, 3, 'Backlog', 'Bug', 5),

    -- Tasks for Story 7 (Create Tasks)
    (17, 'Design task creation modal', 'Create modal dialog for new task creation', 1, 1, 'Backlog', 'Task', 7),
    (18, 'Implement save task API', 'Backend endpoint to save new tasks', 2, 2, 'Backlog', 'Task', 7),

    -- Tasks for Story 8 (Edit Tasks)
    (19, 'Create edit task form', 'Form to edit existing task details', 1, 1, 'Backlog', 'Task', 8),
    (20, 'Update task API endpoint', 'Backend endpoint to update task data', 1, 2, 'Backlog', 'Task', 8);

-- Create a sample sprint
INSERT INTO sprints (id, name, start_date, end_date, goal, is_archived)
VALUES
    (1, 'Sprint 1', NOW(), NOW() + INTERVAL '14 days', 'Implement basic authentication features', FALSE);

-- Reset sequences to continue from the last inserted ID
SELECT setval('backlog_items_id_seq', (SELECT MAX(id) FROM backlog_items));
SELECT setval('sprints_id_seq', (SELECT MAX(id) FROM sprints));
