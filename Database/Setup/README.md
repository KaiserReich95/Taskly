# Taskly Database Setup for Supabase

This folder contains the database schema and seed data for the Taskly application using Supabase (PostgreSQL).

## Files

- **schema.sql** - Database schema including tables, indexes, and triggers
- **seed.sql** - Sample seed data for testing
- **README.md** - This file with setup instructions

## Database Schema

### Tables

1. **backlog_items** - Stores all Epics, Stories, Tasks, and Bugs
   - `id` (SERIAL PRIMARY KEY)
   - `title` (VARCHAR 255)
   - `description` (TEXT)
   - `story_points` (INTEGER)
   - `priority` (INTEGER)
   - `status` (ENUM: Backlog, Todo, InProgress, Review, Done)
   - `type` (ENUM: Epic, Story, Task, Bug)
   - `sprint_id` (INTEGER, FK to sprints)
   - `parent_id` (INTEGER, self-referencing FK for hierarchy)
   - `created_at`, `updated_at` (TIMESTAMP)

2. **sprints** - Stores sprint information
   - `id` (SERIAL PRIMARY KEY)
   - `name` (VARCHAR 255)
   - `start_date`, `end_date` (TIMESTAMP)
   - `goal` (TEXT)
   - `is_archived` (BOOLEAN)
   - `created_at`, `updated_at` (TIMESTAMP)

3. **sprint_items** - Junction table for many-to-many relationship
   - `sprint_id`, `item_id` (composite PRIMARY KEY)
   - `added_at` (TIMESTAMP)

4. **developers** - Stores developer information (for future use)
   - `id` (SERIAL PRIMARY KEY)
   - `name` (VARCHAR 255)
   - `capacity` (INTEGER)
   - `created_at`, `updated_at` (TIMESTAMP)

## Setup Instructions

### 1. Create a Supabase Project

1. Go to [Supabase](https://supabase.com)
2. Sign up or log in
3. Create a new project
4. Wait for the project to be provisioned

### 2. Run the Schema

1. In your Supabase project, go to **SQL Editor**
2. Create a new query
3. Copy the contents of `schema.sql` and paste it
4. Click **Run** to execute the schema

### 3. Load Seed Data (Optional)

1. In the SQL Editor, create another new query
2. Copy the contents of `seed.sql` and paste it
3. Click **Run** to insert sample data

### 4. Get Your Connection Details

1. Go to **Settings** > **Database** in your Supabase project
2. Copy your connection string or API credentials
3. You'll need:
   - **Project URL**: `https://your-project.supabase.co`
   - **Anon/Public Key**: Your public API key
   - **Service Role Key**: Your service role key (keep this secret!)

### 5. Configure Your Application

Create a `.env` file in the root of your Taskly project:

```env
SUPABASE_URL=https://your-project.supabase.co
SUPABASE_ANON_KEY=your-anon-key-here
SUPABASE_SERVICE_KEY=your-service-key-here
```

## Hierarchy Structure

The database uses a self-referencing foreign key (`parent_id`) to create the hierarchy:

```
Epic (parent_id = NULL)
  └─ Story (parent_id = Epic.id)
       ├─ Task (parent_id = Story.id)
       └─ Bug (parent_id = Story.id)
```

## Query Examples

### Get all Epics
```sql
SELECT * FROM backlog_items WHERE type = 'Epic' AND parent_id IS NULL;
```

### Get Stories for an Epic
```sql
SELECT * FROM backlog_items WHERE type = 'Story' AND parent_id = 1;
```

### Get Tasks/Bugs for a Story
```sql
SELECT * FROM backlog_items
WHERE (type = 'Task' OR type = 'Bug') AND parent_id = 4;
```

### Get all items in a Sprint
```sql
SELECT bi.* FROM backlog_items bi
JOIN sprint_items si ON bi.id = si.item_id
WHERE si.sprint_id = 1;
```

### Get current (non-archived) Sprint
```sql
SELECT * FROM sprints WHERE is_archived = FALSE LIMIT 1;
```

## Row Level Security (RLS)

To enable RLS in Supabase:

```sql
-- Enable RLS on tables
ALTER TABLE backlog_items ENABLE ROW LEVEL SECURITY;
ALTER TABLE sprints ENABLE ROW LEVEL SECURITY;
ALTER TABLE sprint_items ENABLE ROW LEVEL SECURITY;
ALTER TABLE developers ENABLE ROW LEVEL SECURITY;

-- Create policies (example - adjust based on your auth requirements)
CREATE POLICY "Allow all operations for authenticated users" ON backlog_items
    FOR ALL USING (auth.role() = 'authenticated');

CREATE POLICY "Allow all operations for authenticated users" ON sprints
    FOR ALL USING (auth.role() = 'authenticated');

CREATE POLICY "Allow all operations for authenticated users" ON sprint_items
    FOR ALL USING (auth.role() = 'authenticated');

CREATE POLICY "Allow all operations for authenticated users" ON developers
    FOR ALL USING (auth.role() = 'authenticated');
```

## Next Steps

1. Install Supabase client library in your project:
   ```bash
   dotnet add package Supabase
   ```

2. Create a service class to interact with the database
3. Replace in-memory state with database queries
4. Implement real-time subscriptions using Supabase Realtime

## Notes

- The `updated_at` field is automatically updated via triggers
- All foreign keys have `ON DELETE CASCADE` or `ON DELETE SET NULL` for proper cleanup
- Indexes are created on frequently queried columns for performance
- The schema supports the full Epic → Story → Task/Bug hierarchy
