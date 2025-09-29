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
   - `BacklogItem` (Id, Title, Description, StoryPoints, Priority, Status)
   - `Sprint` (Id, Name, StartDate, EndDate, Goal, Items)
   - `Developer` (Id, Name, Capacity)
   - Use ImmutableArray for collections to work with UseState

2. **Set up main app structure**
   - Dashboard with navigation tabs
   - Backlog view, Sprint Planning view, Sprint Board view
   - Use TabsLayout for navigation

### Phase 2: Backlog Management
3. **Implement backlog state management**
   - UseState<ImmutableArray<BacklogItem>> for backlog items
   - Add/edit/delete operations updating immutable state
   - Auto-increment ID generation

4. **Create backlog UI**
   - Card-based story display
   - Add new story form using TextInput and NumberInput
   - Priority drag-and-drop ordering (simulate with up/down buttons initially)
   - Status badges and story point indicators

### Phase 3: Sprint Planning
5. **Build sprint management**
   - UseState<ImmutableArray<Sprint>> for sprints
   - Create sprint form with date pickers and goal setting
   - Move items from backlog to sprint (update item status)

6. **Sprint planning interface**
   - Two-column layout: Available backlog items | Current sprint
   - Capacity tracking with progress bars
   - Drag-and-drop simulation with move buttons

### Phase 4: Sprint Board (Kanban)
7. **Create Kanban board**
   - Four columns: To Do, In Progress, Review, Done
   - Filter sprint items by status
   - Move items between columns (update item status)
   - Real-time updates using UseState triggers

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