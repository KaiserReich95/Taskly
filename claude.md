# Backlog/Sprint Planning Board App - Implementation Plan (In-Memory)

## Ivy Framework Documentation
- **Main Documentation**: https://github.com/Ivy-Interactive/Ivy-Framework/tree/main/Ivy.Docs.Shared/Docs
- **Local Documentation Summary**: `C:\Users\ernil\Documents\Administration\Fritid\Taskly\DocumentationSummary`
  - `ivy_technical_specs.json` - Complete technical specifications
  - `ivy-docs-technical-summary.json` - Comprehensive framework overview
- **Framework Note**: Ivy Framework is quite new with limited online help - use the official documentation as primary reference

## Overview
Build a backlog/sprint planning board application using the Ivy Framework with in-memory state management. Data will persist during the session but reset on app restart.

## Current Architecture
The app uses a three-tab layout:
1. **Backlog Tab**: Manage product backlog items and sprints
2. **Sprint Board Tab**: Kanban-style board for active sprint
3. **Sprint Archive Tab**: View and restore archived sprints

## Implementation Status

### ✅ Phase 1: Core Data Models & Foundation (COMPLETED)
**Data Models:**
- `BacklogItem` (Id, Title, Description, StoryPoints, Priority, Status, Type, SprintId)
- `Sprint` (Id, Name, StartDate, EndDate, Goal, ItemIds)
- `Developer` (Id, Name, Capacity) - for future use
- `ItemStatus` enum (Backlog, Todo, InProgress, Review, Done)
- `IssueType` enum (Task, Bug, Story, Epic)

**App Structure:**
- Three-tab layout using TabsLayout
- Shared state management across tabs using UseState
- ImmutableArray for collections

### ✅ Phase 2: Backlog Management (COMPLETED)
**State Management:**
- UseState<ImmutableArray<BacklogItem>> for backlog items
- UseState<Sprint> for current sprint
- UseState<ImmutableArray<Sprint>> for archived sprints
- Auto-increment ID generation

**UI Features:**
- ✅ FloatingPanel modal for adding backlog items
- ✅ Single-line card-based display with issue type badges
- ✅ Color-coded badges: Task (gray), Bug (red), Story (blue), Epic (outline)
- ✅ Sprint creation and management
- ✅ Add/Remove items to/from current sprint
- ✅ Story point indicators (1-21 validation)
- ✅ Delete backlog items

### ✅ Phase 3: Sprint Board (Jira-style Active Sprint) (COMPLETED)
**Kanban Board:**
- ✅ Three-column layout: "To Do" | "In Progress" | "Done"
- ✅ Items automatically appear in "To Do" when added to sprint
- ✅ Bidirectional status movement with buttons:
  - To Do → "Start" → In Progress
  - In Progress → "← Reverse" (red/destructive) or "Complete" → Done
  - Done → "← Reverse" (red/destructive) → In Progress
- ✅ Real-time updates using UseState triggers
- ✅ Archive Sprint button on Sprint Board tab

**Sprint Board Features:**
- ✅ Display issue type, title, description, story points
- ✅ Move items forward and backward through workflow
- ✅ Sprint summary with item counts by status

### ✅ Phase 3.5: Sprint Archive (COMPLETED)
**Archive Features:**
- ✅ Archive current sprint from Backlog or Sprint Board tabs
- ✅ View all archived sprints with statistics:
  - Completion metrics (items completed, story points completed)
  - Sprint duration and goals
  - All items with final status
- ✅ **Restore Sprint**: Click "Make Current Sprint" on any archived sprint
  - Automatically archives current sprint if one exists
  - Makes selected sprint the active sprint
  - Allows switching between sprints without losing data

### 🚧 Phase 4: Hierarchical Epic Structure (PLANNED - NEXT)
**Goal:** Enforce proper Agile hierarchy: Epic → Story → Task/Bug

**Changes Required:**
1. **Add Hierarchy to Data Model:**
   - Add `ParentId` (int?) to BacklogItem for parent-child relationships
   - Epics have no parent
   - Stories have Epic as parent
   - Tasks/Bugs have Story as parent

2. **Modify Product Backlog Tab:**
   - Default view: List of Epics only
   - "Add Epic" button (modal restricted to Epic type only)
   - Each Epic card shows:
     - Epic details (title, description, story points)
     - Number of child Stories
     - Progress indicators
     - "Open Epic" button

3. **Epic Detail View:**
   - Breadcrumb: "Backlog > [Epic Name]"
   - "Back to Backlog" button
   - List of Stories within this Epic
   - "Add Story" button (modal restricted to Story type only)
   - Each Story card shows:
     - Story details
     - Number of child Tasks/Bugs
     - "Open Story" button

4. **Story Detail View:**
   - Breadcrumb: "Backlog > [Epic Name] > [Story Name]"
   - "Back to Epic" button
   - List of Tasks and Bugs within this Story
   - "Add Task/Bug" button (modal restricted to Task or Bug types only)

5. **Implementation Approach:**
   - Add `UseState<BacklogItem?>` for currently opened Epic
   - Add `UseState<BacklogItem?>` for currently opened Story
   - Conditionally render based on state:
     ```
     if (openedStory != null) → Show Story Detail View
     else if (openedEpic != null) → Show Epic Detail View
     else → Show Epic List View
     ```
   - Update modal to filter issue type options based on context
   - Add breadcrumb navigation component

**Benefits:**
- Enforces proper work breakdown structure
- Better organization of large features
- Clearer hierarchy: Epic (feature) → Story (user story) → Task/Bug (implementation)
- Easier epic progress tracking
- Aligns with standard Agile/Scrum practices

### Phase 5: Basic Reporting (FUTURE)
- Story count by status
- Total story points in sprint vs completed
- Epic progress tracking
- Velocity charts

### Phase 6: Enhanced Features (FUTURE)
- Search and filter functionality
- Export data as JSON using UseDownload
- Import data from JSON files
- Theme support and responsive design
- Developer assignment and capacity tracking

## Technical Implementation Details

**State Management:**
- All data stored in UseState hooks at app level
- Pass state and update functions down to child components
- ImmutableArray for all collections to work with Ivy's reactivity

**UI Components:**
- FloatingPanel for modal dialogs
- Card for item display
- Button with variants: Primary, Secondary, Destructive, Outline
- Badge with variants: Primary, Secondary, Destructive, Outline
- Layout (Vertical, Horizontal) for structure
- TextInput, NumberInput, SelectInput for forms

**Navigation:**
- TabsLayout for main sections
- Conditional rendering for hierarchical navigation
- State-based view switching

**Persistence:**
- Pure in-memory (session-based)
- Can add localStorage or database later without major refactoring

## Key Design Decisions

1. **Three-tab structure** separates concerns:
   - Backlog: Planning and organization
   - Sprint Board: Active work execution
   - Sprint Archive: Historical tracking

2. **Bidirectional workflow** allows error correction and reopening work

3. **Sprint archive/restore** enables sprint comparison and switching

4. **Hierarchical Epic structure** (next phase) enforces proper Agile breakdown

5. **In-memory state** keeps implementation simple, focusing on UX first
