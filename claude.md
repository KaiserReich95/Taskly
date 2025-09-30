# Backlog/Sprint Planning Board App - Implementation Plan (In-Memory)

## Ivy Framework Documentation
- **Main Documentation**: https://github.com/Ivy-Interactive/Ivy-Framework/tree/main/Ivy.Docs.Shared/Docs
- **Local Documentation Summary**: `C:\Users\ernil\Documents\Administration\Fritid\Taskly\DocumentationSummary`
  - `ivy_technical_specs.json` - Complete technical specifications
  - `ivy-docs-technical-summary.json` - Comprehensive framework overview
- **Framework Note**: Ivy Framework is quite new with limited online help - use the official documentation as primary reference

## Overview
Build a backlog/sprint planning board application using the Ivy Framework with in-memory state management. Data will persist during the session but reset on app restart.

## Implementation Steps

### Phase 1: Core Data Models & Foundation
1. **Create core data models as C# records**
   - `BacklogItem` (Id, Title, Description, StoryPoints, Priority, Status, Type)
   - `Sprint` (Id, Name, StartDate, EndDate, Goal, Items)
   - `Developer` (Id, Name, Capacity)
   - `IssueType` enum (Task, Bug, Story, Epic)
   - Use ImmutableArray for collections to work with UseState

2. **Set up main app structure**
   - Dashboard with navigation tabs
   - Backlog view, Sprint Board view
   - Use TabsLayout for navigation

### Phase 2: Backlog Management
3. **Implement backlog state management**
   - UseState<ImmutableArray<BacklogItem>> for backlog items
   - Add/edit/delete operations updating immutable state
   - Auto-increment ID generation

4. **Create backlog UI**
   - Single-line card-based story display with issue type badges
   - Add new story form using TextInput, NumberInput, and SelectInput for issue type
   - Sprint management: Add/Remove items to/from current sprint
   - Story point indicators and color-coded issue type badges
   - Issue types: Task (gray), Bug (red), Story (blue), Epic (outline)

## ✅ Completed Enhancements (Phase 2+)
- **Issue Type Classification**: Dropdown selection for Task, Bug, Story, Epic with color-coded badges
- **Sprint Management**: Create sprints and add/remove backlog items to/from current sprint
- **Backlog Operations**: Add items to sprint with "Add to Sprint" button, remove with "Remove from Sprint"
- **Enhanced UI**: Single-line cards with multiple badges and action buttons
- **Form Validation**: Required fields and story point limits (1-21)
- **Compact Layout**: Optimized single-line item display with minimal spacing

### Phase 3: Sprint Board (Jira-style Active Sprint)
5. **Create sprint board layout**
   - Three-column Kanban board: "To Do" | "In Progress" | "Done"
   - Items automatically appear in "To Do" when added to sprint from backlog
   - Sprint board items can move between columns using status buttons
   - Real-time updates using UseState triggers

6. **Sprint board functionality**
   - Sprint items display with issue type, title, description, story points
   - Status progression: To Do → In Progress → Done
   - Move items between columns with action buttons
   - Items remain in sprint throughout their lifecycle

### Phase 5: Basic Reporting
8. **Simple metrics dashboard**
   - Story count by status
   - Total story points in sprint vs completed
   - Basic progress indicators

### Phase 6: Enhanced Features
9. **Add polish and usability**
   - Search and filter functionality
   - Export data as JSON using UseDownload
   - Import data from JSON files
   - Theme support and responsive design

## Technical Implementation Details

- **State Management**: All data stored in UseState hooks at app level
- **Data Flow**: Pass state and update functions down to child components
- **Persistence**: None initially - pure in-memory (can add localStorage later)
- **UI**: Ivy widgets (Card, Button, Layout, TextInput, NumberInput, Badge)
- **Navigation**: TabsLayout for main sections

## Key Benefits of This Approach

- **Quick to implement**: No database setup or configuration
- **Easy to test**: Immediate feedback and changes
- **Portable**: Works anywhere without dependencies
- **Evolutionary**: Can add persistence layer later without major refactoring

This approach lets us focus on the core functionality and user experience first, then add persistence when the app structure is solid.