[App(icon: Icons.Calendar)]
public class TasklyApp : ViewBase
{
    // Core data models
    public record BacklogItem(
        int Id,
        string Title,
        string Description,
        int StoryPoints,
        int Priority,
        ItemStatus Status,
        IssueType Type,
        int? SprintId = null,
        int? ParentId = null
    );

    public record Sprint(
        int Id,
        string Name,
        DateTime StartDate,
        DateTime EndDate,
        string Goal,
        ImmutableArray<int> ItemIds
    );

    public record Developer(
        int Id,
        string Name,
        int Capacity
    );

    public enum ItemStatus
    {
        Backlog,
        Todo,
        InProgress,
        Review,
        Done
    }

    public enum IssueType
    {
        Task,
        Bug,
        Story,
        Epic
    }

    private static Badge GetIssueTypeBadge(IssueType type)
    {
        return type switch
        {
            IssueType.Task => new Badge("Task").Secondary(),
            IssueType.Bug => new Badge("Bug").Destructive(),
            IssueType.Story => new Badge("Story").Primary(),
            IssueType.Epic => new Badge("Epic").Outline(),
            _ => new Badge(type.ToString()).Secondary()
        };
    }

    public override object? Build()
    {
        var selectedTab = UseState(0);

        // Shared state management for all tabs
        var backlogItems = UseState(ImmutableArray<BacklogItem>.Empty);
        var currentSprint = UseState(() => (Sprint)null!);
        var archivedSprints = UseState(ImmutableArray<Sprint>.Empty);

        return Layout.Vertical(
            Text.H1("Taskly - Backlog & Sprint Planning"),
            new TabsLayout(
                onSelect: e => { selectedTab.Set(e.Value); return ValueTask.CompletedTask; },
                onClose: null,
                onRefresh: null,
                onReorder: null,
                selectedIndex: selectedTab.Value,
                new Tab("Backlog", BuildBacklogView(backlogItems, currentSprint, archivedSprints)),
                new Tab("Sprint Board", BuildSprintBoardView(backlogItems, currentSprint, archivedSprints)),
                new Tab("Sprint Archive", BuildSprintArchiveView(archivedSprints, backlogItems, currentSprint))
            )
        );
    }

    private object BuildBacklogView(IState<ImmutableArray<BacklogItem>> backlogItems, IState<Sprint> currentSprint, IState<ImmutableArray<Sprint>> archivedSprints)
    {
        var nextId = UseState(1);

        // Hierarchy navigation state
        var openedEpic = UseState<BacklogItem?>(() => null);
        var openedStory = UseState<BacklogItem?>(() => null);

        // State for adding new items
        var newTitle = UseState("");
        var newDescription = UseState("");
        var newStoryPoints = UseState(1);
        var newIssueType = UseState(IssueType.Epic); // Default to Epic for top level

        // Modal state
        var isAddItemModalOpen = UseState(false);
        var addTaskToStory = UseState<BacklogItem?>(() => null); // Track which story's Add Task/Bug modal is open

        // Sprint management
        var nextSprintId = UseState(1);

        // State for creating new sprint
        var newSprintName = UseState("");
        var newSprintGoal = UseState("");


        // Helper methods for CRUD operations
        void AddItem()
        {
            if (!string.IsNullOrWhiteSpace(newTitle.Value))
            {
                // Determine ParentId based on hierarchy context
                int? parentId = null;
                if (addTaskToStory.Value != null)
                {
                    parentId = addTaskToStory.Value.Id; // Task/Bug belongs to Story (modal within Epic view)
                }
                else if (openedStory.Value != null)
                {
                    parentId = openedStory.Value.Id; // Task/Bug belongs to Story (Story detail view)
                }
                else if (openedEpic.Value != null)
                {
                    parentId = openedEpic.Value.Id; // Story belongs to Epic
                }
                // else: Epic has no parent

                var newItem = new BacklogItem(
                    Id: nextId.Value,
                    Title: newTitle.Value,
                    Description: newDescription.Value,
                    StoryPoints: newStoryPoints.Value,
                    Priority: backlogItems.Value.Length + 1,
                    Status: ItemStatus.Backlog,
                    Type: newIssueType.Value,
                    ParentId: parentId
                );

                backlogItems.Set(backlogItems.Value.Add(newItem));
                nextId.Set(nextId.Value + 1);

                // Clear form
                newTitle.Set("");
                newDescription.Set("");
                newStoryPoints.Set(1);

                // Reset to default type based on context
                if (addTaskToStory.Value != null)
                {
                    newIssueType.Set(IssueType.Task);
                    addTaskToStory.Set((BacklogItem?)null); // Close the add task modal
                }
                else if (openedStory.Value != null)
                {
                    newIssueType.Set(IssueType.Task);
                }
                else if (openedEpic.Value != null)
                {
                    newIssueType.Set(IssueType.Story);
                }
                else
                {
                    newIssueType.Set(IssueType.Epic);
                }

                // Close modal
                isAddItemModalOpen.Set(false);
            }
        }

        void CreateSprint()
        {
            if (!string.IsNullOrWhiteSpace(newSprintName.Value))
            {
                var sprint = new Sprint(
                    Id: nextSprintId.Value,
                    Name: newSprintName.Value,
                    StartDate: DateTime.Now,
                    EndDate: DateTime.Now.AddDays(14), // 2-week sprint
                    Goal: newSprintGoal.Value,
                    ItemIds: ImmutableArray<int>.Empty
                );

                currentSprint.Set(sprint);
                nextSprintId.Set(nextSprintId.Value + 1);

                // Clear form
                newSprintName.Set("");
                newSprintGoal.Set("");
            }
        }

        void RemoveItemFromSprint(int itemId)
        {
            if (currentSprint.Value != null)
            {
                // Update item status and remove from sprint
                var updatedItems = backlogItems.Value
                    .Select(item => item.Id == itemId ?
                        item with { Status = ItemStatus.Backlog, SprintId = null } : item)
                    .ToImmutableArray();

                // Update sprint by removing item
                var updatedSprint = currentSprint.Value with
                {
                    ItemIds = currentSprint.Value.ItemIds.Remove(itemId)
                };

                backlogItems.Set(updatedItems);
                currentSprint.Set(updatedSprint);
            }
        }

        void ArchiveSprint()
        {
            if (currentSprint.Value != null)
            {
                // Add current sprint to archived sprints
                archivedSprints.Set(archivedSprints.Value.Add(currentSprint.Value));

                // Clear current sprint
                currentSprint.Set((Sprint)null!);
            }
        }

        // Conditional rendering based on hierarchy navigation
        return openedStory.Value != null ?
            // STORY DETAIL VIEW: Show Tasks and Bugs within the Story
            Layout.Vertical(
                isAddItemModalOpen.Value ?
                    new FloatingPanel(
                        new Card(
                            Layout.Vertical(
                                Text.H3("Add Task/Bug to Story"),
                                newTitle.ToTextInput().Placeholder("Enter title..."),
                                newDescription.ToTextInput().Placeholder("Enter description..."),
                                newStoryPoints.ToNumberInput().Min(1).Max(21),
                                new SelectInput<IssueType>(
                                    value: newIssueType.Value,
                                    onChange: e => { newIssueType.Set(e.Value); return ValueTask.CompletedTask; },
                                    options: new[] {
                                        new Option<IssueType>("Task", IssueType.Task),
                                        new Option<IssueType>("Bug", IssueType.Bug)
                                    }
                                ),
                                Layout.Horizontal(
                                    new Button("Cancel", () => isAddItemModalOpen.Set(false)).Secondary(),
                                    new Button("Add Item", AddItem).Primary()
                                ).Gap(8)
                            )
                        )
                    ) : null,

                BuildStoryDetailView(openedStory.Value, openedEpic.Value!, backlogItems, openedEpic, openedStory, currentSprint, isAddItemModalOpen)
            ) :
        openedEpic.Value != null ?
            // EPIC DETAIL VIEW: Show Stories within the Epic
            Layout.Vertical(
                // Modal for adding Story to Epic
                isAddItemModalOpen.Value ?
                    new FloatingPanel(
                        new Card(
                            Layout.Vertical(
                                Text.H3("Add Story to Epic"),
                                newTitle.ToTextInput().Placeholder("Enter title..."),
                                newDescription.ToTextInput().Placeholder("Enter description..."),
                                newStoryPoints.ToNumberInput().Min(1).Max(21),
                                Text.P($"Type: Story"),
                                Layout.Horizontal(
                                    new Button("Cancel", () => isAddItemModalOpen.Set(false)).Secondary(),
                                    new Button("Add Item", AddItem).Primary()
                                ).Gap(8)
                            )
                        )
                    ) : null,

                // Modal for adding Task/Bug to Story (within Epic view)
                addTaskToStory.Value != null ?
                    new FloatingPanel(
                        new Card(
                            Layout.Vertical(
                                Text.H3($"Add Task/Bug to Story: {addTaskToStory.Value.Title}"),
                                newTitle.ToTextInput().Placeholder("Enter title..."),
                                newDescription.ToTextInput().Placeholder("Enter description..."),
                                newStoryPoints.ToNumberInput().Min(1).Max(21),
                                new SelectInput<IssueType>(
                                    value: newIssueType.Value,
                                    onChange: e => { newIssueType.Set(e.Value); return ValueTask.CompletedTask; },
                                    options: new[] {
                                        new Option<IssueType>("Task", IssueType.Task),
                                        new Option<IssueType>("Bug", IssueType.Bug)
                                    }
                                ),
                                Layout.Horizontal(
                                    new Button("Cancel", () => addTaskToStory.Set((BacklogItem?)null)).Secondary(),
                                    new Button("Add Item", AddItem).Primary()
                                ).Gap(8)
                            )
                        )
                    ) : null,

                BuildEpicDetailView(openedEpic.Value, backlogItems, openedEpic, openedStory, currentSprint, isAddItemModalOpen, addTaskToStory, newIssueType)
            ) :
            // EPIC LIST VIEW: Show all Epics with Sprint Management
            Layout.Vertical(
                Text.H2("Product Backlog"),

                new Button("+ Add Backlog Item", () => { newIssueType.Set(IssueType.Epic); isAddItemModalOpen.Set(true); }).Primary(),

                isAddItemModalOpen.Value ?
                    new FloatingPanel(
                        new Card(
                            Layout.Vertical(
                                Text.H3("Add Epic"),
                                newTitle.ToTextInput().Placeholder("Enter title..."),
                                newDescription.ToTextInput().Placeholder("Enter description..."),
                                newStoryPoints.ToNumberInput().Min(1).Max(21),
                                Text.P($"Type: Epic"),
                                Layout.Horizontal(
                                    new Button("Cancel", () => isAddItemModalOpen.Set(false)).Secondary(),
                                    new Button("Add Item", AddItem).Primary()
                                ).Gap(8)
                            )
                        )
                    ) : null,

                    // Sprint management section (only shown at Epic List level)
                    currentSprint.Value == null ?
                        new Card(
                            Layout.Vertical(
                                Text.H3("Create New Sprint"),
                                newSprintName.ToTextInput().Placeholder("Sprint name (e.g., Sprint 1)"),
                                newSprintGoal.ToTextInput().Placeholder("Sprint goal (optional)"),
                                new Button("Create Sprint", CreateSprint).Primary()
                            )
                        ) :
                        new Card(
                            Layout.Vertical(
                                Layout.Horizontal(
                                    Layout.Vertical(
                                        Text.H3($"Current Sprint: {currentSprint.Value.Name}"),
                                        !string.IsNullOrEmpty(currentSprint.Value.Goal) ?
                                            Text.P($"Goal: {currentSprint.Value.Goal}") : null,
                                        Text.Small($"Items in sprint: {currentSprint.Value.ItemIds.Length}")
                                    ).Width(Size.Grow()),
                                    new Button("Archive Sprint", ArchiveSprint).Secondary()
                                ),

                                // Display sprint items
                                currentSprint.Value.ItemIds.Length > 0 ?
                                    Layout.Vertical(
                                        Text.H4("Sprint Items:"),
                                        Layout.Vertical(
                                            backlogItems.Value
                                                .Where(item => currentSprint.Value.ItemIds.Contains(item.Id))
                                                .OrderBy(x => x.Id)
                                                .Select(item => new Card(
                                                    Layout.Horizontal(
                                                        GetIssueTypeBadge(item.Type),
                                                        Text.Strong(!string.IsNullOrEmpty(item.Description) ?
                                                            $"{item.Title} - {item.Description}" : item.Title)
                                                            .Width(Size.Grow()),
                                                        new Badge(item.Status.ToString()).Secondary(),
                                                        new Badge($"{item.StoryPoints} pts").Primary(),
                                                        new Button("Remove from Sprint", () => RemoveItemFromSprint(item.Id)).Secondary().Small()
                                                    )
                                                ))
                                                .ToArray()
                                        ).Gap(4)
                                    ).Gap(4) :
                                    Text.P("No items in sprint yet. Use 'Add to Sprint' buttons below to add items.")
                            ).Gap(4)
                        ),

                    BuildEpicListViewSection(backlogItems, openedEpic, currentSprint)
                );
    }

    // EPIC LIST VIEW: Shows only Epics (top-level items)
    private object BuildEpicListViewSection(IState<ImmutableArray<BacklogItem>> backlogItems, IState<BacklogItem?> openedEpic, IState<Sprint> currentSprint)
    {
        // Get only Epics (items with no parent)
        var epics = backlogItems.Value.Where(item => item.Type == IssueType.Epic && item.ParentId == null).ToArray();

        // Helper to add/remove epic items from sprint
        void AddItemToSprint(int itemId)
        {
            if (currentSprint.Value != null)
            {
                var updatedItems = backlogItems.Value
                    .Select(item => item.Id == itemId ?
                        item with { Status = ItemStatus.Todo, SprintId = currentSprint.Value.Id } : item)
                    .ToImmutableArray();

                var updatedSprint = currentSprint.Value with
                {
                    ItemIds = currentSprint.Value.ItemIds.Add(itemId)
                };

                backlogItems.Set(updatedItems);
                currentSprint.Set(updatedSprint);
            }
        }

        void RemoveItemFromSprint(int itemId)
        {
            if (currentSprint.Value != null)
            {
                var updatedItems = backlogItems.Value
                    .Select(item => item.Id == itemId ?
                        item with { Status = ItemStatus.Backlog, SprintId = null } : item)
                    .ToImmutableArray();

                var updatedSprint = currentSprint.Value with
                {
                    ItemIds = currentSprint.Value.ItemIds.Remove(itemId)
                };

                backlogItems.Set(updatedItems);
                currentSprint.Set(updatedSprint);
            }
        }

        void DeleteItem(int id)
        {
            backlogItems.Set(backlogItems.Value.Where(item => item.Id != id).ToImmutableArray());
        }

        return Layout.Vertical(
            Text.H3($"Epics ({epics.Length})"),
            epics.Length == 0 ?
                new Card(Text.P("No epics yet. Click '+ Add Backlog Item' to create your first epic.")) :
                Layout.Vertical(
                    epics
                        .OrderBy(x => x.Id)
                        .Select(epic =>
                        {
                            bool isInSprint = epic.SprintId != null;
                            var storiesCount = backlogItems.Value.Count(item => item.ParentId == epic.Id && item.Type == IssueType.Story);

                            return new Card(
                                Layout.Horizontal(
                                    // Issue type badge
                                    GetIssueTypeBadge(epic.Type),

                                    // Title and description
                                    Text.Strong(!string.IsNullOrEmpty(epic.Description) ?
                                        $"{epic.Title} - {epic.Description}" : epic.Title)
                                        .Width(Size.Grow()),

                                    // Story count indicator
                                    new Badge($"{storiesCount} stories").Secondary(),

                                    // Story points
                                    new Badge($"{epic.StoryPoints} pts").Primary(),

                                    // Sprint status
                                    isInSprint ?
                                        new Badge("In Sprint").Secondary() :
                                        new Badge("Backlog").Outline(),

                                    // Open Epic button
                                    new Button("Open", () => openedEpic.Set(epic)).Primary().Small(),

                                    // Sprint management
                                    !isInSprint && currentSprint.Value != null ?
                                        new Button("Add to Sprint", () => AddItemToSprint(epic.Id)).Primary().Small() :
                                    isInSprint ?
                                        new Button("Remove from Sprint", () => RemoveItemFromSprint(epic.Id)).Secondary().Small() :
                                    null,

                                    // Delete button
                                    new Button("Delete", () => DeleteItem(epic.Id)).Destructive().Small()
                                )
                            );
                        }).ToArray()
                ).Gap(4)
        );
    }

    // EPIC DETAIL VIEW: Shows Stories within an Epic
    private object BuildEpicDetailView(BacklogItem epic, IState<ImmutableArray<BacklogItem>> backlogItems, IState<BacklogItem?> openedEpic, IState<BacklogItem?> openedStory, IState<Sprint> currentSprint, IState<bool> isAddItemModalOpen, IState<BacklogItem?> addTaskToStory, IState<IssueType> newIssueType)
    {
        // Get Stories that belong to this Epic
        var stories = backlogItems.Value.Where(item => item.ParentId == epic.Id && item.Type == IssueType.Story).ToArray();

        void DeleteItem(int id)
        {
            backlogItems.Set(backlogItems.Value.Where(item => item.Id != id).ToImmutableArray());
        }

        void AddItemToSprint(int itemId)
        {
            if (currentSprint.Value != null)
            {
                var updatedItems = backlogItems.Value
                    .Select(item => item.Id == itemId ?
                        item with { Status = ItemStatus.Todo, SprintId = currentSprint.Value.Id } : item)
                    .ToImmutableArray();

                var updatedSprint = currentSprint.Value with
                {
                    ItemIds = currentSprint.Value.ItemIds.Add(itemId)
                };

                backlogItems.Set(updatedItems);
                currentSprint.Set(updatedSprint);
            }
        }

        void RemoveItemFromSprint(int itemId)
        {
            if (currentSprint.Value != null)
            {
                var updatedItems = backlogItems.Value
                    .Select(item => item.Id == itemId ?
                        item with { Status = ItemStatus.Backlog, SprintId = null } : item)
                    .ToImmutableArray();

                var updatedSprint = currentSprint.Value with
                {
                    ItemIds = currentSprint.Value.ItemIds.Remove(itemId)
                };

                backlogItems.Set(updatedItems);
                currentSprint.Set(updatedSprint);
            }
        }

        return Layout.Vertical(
            // Breadcrumb navigation
            Layout.Horizontal(
                new Button("← Back to Epics", () => openedEpic.Set((BacklogItem?)null)).Secondary().Small(),
                Text.H3($"Epic: {epic.Title}")
            ),

            // Epic details
            new Card(
                Layout.Vertical(
                    !string.IsNullOrEmpty(epic.Description) ? Text.P(epic.Description) : null,
                    Layout.Horizontal(
                        new Badge($"{epic.StoryPoints} pts").Primary(),
                        new Badge($"{stories.Length} stories").Secondary()
                    ).Gap(4)
                )
            ),

            // Add Story button
            new Button("+ Add Story", () => { newIssueType.Set(IssueType.Story); isAddItemModalOpen.Set(true); }).Primary(),

            // Stories list
            Text.H3($"Stories ({stories.Length})"),
            stories.Length == 0 ?
                new Card(Text.P("No stories yet. Click '+ Add Story' to create a story.")) :
                Layout.Vertical(
                    stories
                        .OrderBy(x => x.Id)
                        .Select(story =>
                        {
                            bool isInSprint = story.SprintId != null;
                            var tasks = backlogItems.Value.Where(item => item.ParentId == story.Id && (item.Type == IssueType.Task || item.Type == IssueType.Bug)).ToArray();

                            return new Card(
                                Layout.Vertical(
                                    // Story header
                                    Layout.Horizontal(
                                        GetIssueTypeBadge(story.Type),

                                        Text.Strong(!string.IsNullOrEmpty(story.Description) ?
                                            $"{story.Title} - {story.Description}" : story.Title)
                                            .Width(Size.Grow()),

                                        new Badge($"{tasks.Length} tasks").Secondary(),
                                        new Badge($"{story.StoryPoints} pts").Primary(),

                                        isInSprint ?
                                            new Badge("In Sprint").Secondary() :
                                            new Badge("Backlog").Outline(),

                                        new Button("+ Add Task/Bug", () => { addTaskToStory.Set(story); newIssueType.Set(IssueType.Task); }).Primary().Small(),

                                        !isInSprint && currentSprint.Value != null ?
                                            new Button("Add to Sprint", () => AddItemToSprint(story.Id)).Primary().Small() :
                                        isInSprint ?
                                            new Button("Remove from Sprint", () => RemoveItemFromSprint(story.Id)).Secondary().Small() :
                                        null,

                                        new Button("Delete", () => DeleteItem(story.Id)).Destructive().Small()
                                    ),

                                    // Nested Tasks/Bugs - Always show section
                                    Layout.Vertical(
                                        Layout.Vertical(
                                            tasks.OrderBy(t => t.Id).Select(task =>
                                            {
                                                bool taskInSprint = task.SprintId != null;
                                                return new Card(
                                                    Layout.Horizontal(
                                                        GetIssueTypeBadge(task.Type),
                                                        Text.P(!string.IsNullOrEmpty(task.Description) ?
                                                            $"{task.Title} - {task.Description}" : task.Title)
                                                            .Width(Size.Grow()),
                                                        new Badge($"{task.StoryPoints} pts").Primary(),
                                                        taskInSprint ?
                                                            new Badge("In Sprint").Secondary() :
                                                            new Badge("Backlog").Outline(),
                                                        !taskInSprint && currentSprint.Value != null ?
                                                            new Button("Add to Sprint", () => AddItemToSprint(task.Id)).Primary().Small() :
                                                        taskInSprint ?
                                                            new Button("Remove from Sprint", () => RemoveItemFromSprint(task.Id)).Secondary().Small() :
                                                        null,
                                                        new Button("Delete", () => DeleteItem(task.Id)).Destructive().Small()
                                                    )
                                                );
                                            }).ToArray()
                                        ).Gap(4)
                                    ).Gap(4)
                                )
                            );
                        }).ToArray()
                ).Gap(4)
        );
    }

    // STORY DETAIL VIEW: Shows Tasks and Bugs within a Story
    private object BuildStoryDetailView(BacklogItem story, BacklogItem epic, IState<ImmutableArray<BacklogItem>> backlogItems, IState<BacklogItem?> openedEpic, IState<BacklogItem?> openedStory, IState<Sprint> currentSprint, IState<bool> isAddItemModalOpen)
    {
        // Get Tasks and Bugs that belong to this Story
        var tasks = backlogItems.Value.Where(item => item.ParentId == story.Id && (item.Type == IssueType.Task || item.Type == IssueType.Bug)).ToArray();

        void DeleteItem(int id)
        {
            backlogItems.Set(backlogItems.Value.Where(item => item.Id != id).ToImmutableArray());
        }

        void AddItemToSprint(int itemId)
        {
            if (currentSprint.Value != null)
            {
                var updatedItems = backlogItems.Value
                    .Select(item => item.Id == itemId ?
                        item with { Status = ItemStatus.Todo, SprintId = currentSprint.Value.Id } : item)
                    .ToImmutableArray();

                var updatedSprint = currentSprint.Value with
                {
                    ItemIds = currentSprint.Value.ItemIds.Add(itemId)
                };

                backlogItems.Set(updatedItems);
                currentSprint.Set(updatedSprint);
            }
        }

        void RemoveItemFromSprint(int itemId)
        {
            if (currentSprint.Value != null)
            {
                var updatedItems = backlogItems.Value
                    .Select(item => item.Id == itemId ?
                        item with { Status = ItemStatus.Backlog, SprintId = null } : item)
                    .ToImmutableArray();

                var updatedSprint = currentSprint.Value with
                {
                    ItemIds = currentSprint.Value.ItemIds.Remove(itemId)
                };

                backlogItems.Set(updatedItems);
                currentSprint.Set(updatedSprint);
            }
        }

        return Layout.Vertical(
            // Breadcrumb navigation
            Layout.Horizontal(
                new Button("← Back to Epic", () => openedStory.Set((BacklogItem?)null)).Secondary().Small(),
                Text.H3($"Story: {story.Title}")
            ),

            // Story details
            new Card(
                Layout.Vertical(
                    Text.Small($"Epic: {epic.Title}"),
                    !string.IsNullOrEmpty(story.Description) ? Text.P(story.Description) : null,
                    Layout.Horizontal(
                        new Badge($"{story.StoryPoints} pts").Primary(),
                        new Badge($"{tasks.Length} tasks/bugs").Secondary()
                    ).Gap(4)
                )
            ),

            // Add Task/Bug button
            new Button("+ Add Task/Bug", () => isAddItemModalOpen.Set(true)).Primary(),

            // Tasks and Bugs list
            Text.H3($"Tasks & Bugs ({tasks.Length})"),
            tasks.Length == 0 ?
                new Card(Text.P("No tasks or bugs yet. Click '+ Add Task/Bug' to create a task or bug.")) :
                Layout.Vertical(
                    tasks
                        .OrderBy(x => x.Id)
                        .Select(task =>
                        {
                            bool isInSprint = task.SprintId != null;

                            return new Card(
                                Layout.Horizontal(
                                    GetIssueTypeBadge(task.Type),

                                    Text.Strong(!string.IsNullOrEmpty(task.Description) ?
                                        $"{task.Title} - {task.Description}" : task.Title)
                                        .Width(Size.Grow()),

                                    new Badge($"{task.StoryPoints} pts").Primary(),

                                    isInSprint ?
                                        new Badge("In Sprint").Secondary() :
                                        new Badge("Backlog").Outline(),

                                    !isInSprint && currentSprint.Value != null ?
                                        new Button("Add to Sprint", () => AddItemToSprint(task.Id)).Primary().Small() :
                                    isInSprint ?
                                        new Button("Remove from Sprint", () => RemoveItemFromSprint(task.Id)).Secondary().Small() :
                                    null,

                                    new Button("Delete", () => DeleteItem(task.Id)).Destructive().Small()
                                )
                            );
                        }).ToArray()
                ).Gap(4)
        );
    }


    private object BuildSprintBoardView(IState<ImmutableArray<BacklogItem>> backlogItems, IState<Sprint> currentSprint, IState<ImmutableArray<Sprint>> archivedSprints)
    {

        // Helper method to update item status
        void UpdateItemStatus(int id, ItemStatus newStatus)
        {
            var updatedItems = backlogItems.Value
                .Select(item => item.Id == id ? item with { Status = newStatus } : item)
                .ToImmutableArray();

            backlogItems.Set(updatedItems);
        }

        void ArchiveSprint()
        {
            if (currentSprint.Value != null)
            {
                // Add current sprint to archived sprints
                archivedSprints.Set(archivedSprints.Value.Add(currentSprint.Value));

                // Clear current sprint
                currentSprint.Set((Sprint)null!);
            }
        }

        // Get sprint items by status
        var sprintItems = backlogItems.Value.Where(item => currentSprint.Value != null &&
            currentSprint.Value.ItemIds.Contains(item.Id)).ToImmutableArray();

        var todoItems = sprintItems.Where(item => item.Status == ItemStatus.Todo).ToArray();
        var inProgressItems = sprintItems.Where(item => item.Status == ItemStatus.InProgress).ToArray();
        var doneItems = sprintItems.Where(item => item.Status == ItemStatus.Done).ToArray();

        // Helper method to build sprint board item card
        object BuildSprintBoardItemCard(BacklogItem item)
        {
            return new Card(
                Layout.Vertical(
                    // Header with issue type and title
                    Layout.Horizontal(
                        GetIssueTypeBadge(item.Type),
                        Text.Strong(item.Title).Width(Size.Grow())
                    ),

                    // Description if available
                    !string.IsNullOrEmpty(item.Description) ?
                        Text.P(item.Description) : null,

                    // Footer with story points and action buttons
                    Layout.Horizontal(
                        new Badge($"{item.StoryPoints} pts").Primary(),

                        // Status progression buttons - forward and backward
                        item.Status == ItemStatus.Todo ?
                            new Button("Start", () => UpdateItemStatus(item.Id, ItemStatus.InProgress)).Primary().Small() :
                        item.Status == ItemStatus.InProgress ?
                            Layout.Horizontal(
                                new Button("← Reverse", () => UpdateItemStatus(item.Id, ItemStatus.Todo)).Destructive().Small(),
                                new Button("Complete", () => UpdateItemStatus(item.Id, ItemStatus.Done)).Primary().Small()
                            ).Gap(4) :
                        item.Status == ItemStatus.Done ?
                            new Button("← Reverse", () => UpdateItemStatus(item.Id, ItemStatus.InProgress)).Destructive().Small() :
                        null
                    ).Gap(8)
                ).Gap(8)
            );
        }

        return Layout.Vertical(
            Text.H2("Sprint Board"),

            // Show current sprint info or message if no sprint
            currentSprint.Value == null ?
                new Card(
                    Text.P("No active sprint. Create a sprint in the Backlog tab to get started.")
                ) :
                new Card(
                    Layout.Vertical(
                        Layout.Horizontal(
                            Layout.Vertical(
                                Text.H3($"Active Sprint: {currentSprint.Value.Name}"),
                                !string.IsNullOrEmpty(currentSprint.Value.Goal) ?
                                    Text.P($"Goal: {currentSprint.Value.Goal}") : null,
                                Text.Small($"Total items: {sprintItems.Length} | " +
                                          $"To Do: {todoItems.Length} | " +
                                          $"In Progress: {inProgressItems.Length} | " +
                                          $"Done: {doneItems.Length}")
                            ).Width(Size.Grow()),
                            new Button("Archive Sprint", ArchiveSprint).Secondary()
                        )
                    )
                ),

            // Three-column Kanban board
            currentSprint.Value != null && sprintItems.Length > 0 ?
                Layout.Horizontal(
                    // To Do Column
                    new Card(
                        Layout.Vertical(
                            Text.H3($"To Do ({todoItems.Length})"),
                            todoItems.Length > 0 ?
                                Layout.Vertical(
                                    todoItems.Select(BuildSprintBoardItemCard).ToArray()
                                ).Gap(8) :
                                Text.P("No items in To Do")
                        ).Gap(12)
                    ).Width(Size.Grow()),

                    // In Progress Column
                    new Card(
                        Layout.Vertical(
                            Text.H3($"In Progress ({inProgressItems.Length})"),
                            inProgressItems.Length > 0 ?
                                Layout.Vertical(
                                    inProgressItems.Select(BuildSprintBoardItemCard).ToArray()
                                ).Gap(8) :
                                Text.P("No items in progress")
                        ).Gap(12)
                    ).Width(Size.Grow()),

                    // Done Column
                    new Card(
                        Layout.Vertical(
                            Text.H3($"Done ({doneItems.Length})"),
                            doneItems.Length > 0 ?
                                Layout.Vertical(
                                    doneItems.Select(BuildSprintBoardItemCard).ToArray()
                                ).Gap(8) :
                                Text.P("No completed items")
                        ).Gap(12)
                    ).Width(Size.Grow())
                ).Gap(16) :

                currentSprint.Value != null ?
                    new Card(
                        Text.P("No items in sprint yet. Add items from the Backlog tab.")
                    ) : null
        ).Gap(16);
    }

    private object BuildSprintArchiveView(IState<ImmutableArray<Sprint>> archivedSprints, IState<ImmutableArray<BacklogItem>> backlogItems, IState<Sprint> currentSprint)
    {
        void RestoreSprint(Sprint sprint)
        {
            // If there's a current sprint, archive it first
            if (currentSprint.Value != null)
            {
                archivedSprints.Set(archivedSprints.Value.Add(currentSprint.Value));
            }

            // Remove the sprint from archives and make it current
            archivedSprints.Set(archivedSprints.Value.Remove(sprint));
            currentSprint.Set(sprint);
        }

        return Layout.Vertical(
            Text.H2("Sprint Archive"),

            archivedSprints.Value.Length == 0 ?
                new Card(
                    Text.P("No archived sprints yet. Archive a sprint from the Backlog tab to see it here.")
                ) :
                Layout.Vertical(
                    archivedSprints.Value
                        .OrderByDescending(s => s.Id)
                        .Select(sprint =>
                        {
                            var sprintItems = backlogItems.Value
                                .Where(item => sprint.ItemIds.Contains(item.Id))
                                .ToArray();

                            var completedItems = sprintItems.Where(item => item.Status == ItemStatus.Done).Count();
                            var totalPoints = sprintItems.Sum(item => item.StoryPoints);
                            var completedPoints = sprintItems.Where(item => item.Status == ItemStatus.Done).Sum(item => item.StoryPoints);

                            return new Card(
                                Layout.Vertical(
                                    Layout.Horizontal(
                                        Layout.Vertical(
                                            Text.H3($"{sprint.Name}"),
                                            !string.IsNullOrEmpty(sprint.Goal) ?
                                                Text.P($"Goal: {sprint.Goal}") : null,
                                            Text.Small($"Duration: {sprint.StartDate:MMM dd, yyyy} - {sprint.EndDate:MMM dd, yyyy}")
                                        ).Width(Size.Grow()),
                                        new Button("Make Current Sprint", () => RestoreSprint(sprint)).Primary()
                                    ),

                                    Layout.Horizontal(
                                        new Badge($"{completedItems}/{sprintItems.Length} items completed").Primary(),
                                        new Badge($"{completedPoints}/{totalPoints} points completed").Secondary()
                                    ).Gap(4),

                                    // Display sprint items
                                    sprintItems.Length > 0 ?
                                        Layout.Vertical(
                                            Text.H4("Sprint Items:"),
                                            Layout.Vertical(
                                                sprintItems
                                                    .OrderBy(item => item.Id)
                                                    .Select(item => new Card(
                                                        Layout.Horizontal(
                                                            GetIssueTypeBadge(item.Type),
                                                            Text.Strong(!string.IsNullOrEmpty(item.Description) ?
                                                                $"{item.Title} - {item.Description}" : item.Title)
                                                                .Width(Size.Grow()),
                                                            new Badge(item.Status.ToString()).Secondary(),
                                                            new Badge($"{item.StoryPoints} pts").Primary()
                                                        )
                                                    ))
                                                    .ToArray()
                                            ).Gap(2)
                                        ).Gap(4) :
                                        Text.P("No items in this sprint.")
                                ).Gap(4)
                            );
                        })
                        .ToArray()
                ).Gap(12)
        );
    }
}