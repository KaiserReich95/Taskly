namespace Taskly.Apps;
using Taskly.Connections;
using Taskly.Models;

[App(icon: Icons.Calendar)]
public class PlanningApp : ViewBase
{

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

        // Create signal senders for broadcasting to other apps
        var backlogItemsSignal = Context.CreateSignal<BacklogItemsSignal, ImmutableArray<BacklogItem>, bool>();
        var currentSprintSignal = Context.CreateSignal<CurrentSprintSignal, Sprint, bool>();
        var archivedSprintsSignal = Context.CreateSignal<ArchivedSprintsSignal, ImmutableArray<Sprint>, bool>();

        // Create signal receivers to get updates from other apps (like SprintBoardApp)
        var backlogItemsReceiver = Context.UseSignal<BacklogItemsSignal, ImmutableArray<BacklogItem>, bool>();
        var currentSprintReceiver = Context.UseSignal<CurrentSprintSignal, Sprint, bool>();
        var archivedSprintsReceiver = Context.UseSignal<ArchivedSprintsSignal, ImmutableArray<Sprint>, bool>();

        return Layout.Vertical(
            Text.H1("Taskly - Backlog & Sprint Planning"),
            new TabsLayout(
                onSelect: e => { selectedTab.Set(e.Value); return ValueTask.CompletedTask; },
                onClose: null,
                onRefresh: null,
                onReorder: null,
                selectedIndex: selectedTab.Value,
                new Tab("Backlog", BuildBacklogView(backlogItems, currentSprint, archivedSprints, backlogItemsSignal, currentSprintSignal, archivedSprintsSignal))
            )
        );
    }

    private object BuildBacklogView(
        IState<ImmutableArray<BacklogItem>> backlogItems,
        IState<Sprint> currentSprint,
        IState<ImmutableArray<Sprint>> archivedSprints,
        ISignalSender<ImmutableArray<BacklogItem>, bool> backlogItemsSignal,
        ISignalSender<Sprint, bool> currentSprintSignal,
        ISignalSender<ImmutableArray<Sprint>, bool> archivedSprintsSignal)
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

        async ValueTask CreateSprint(Event<Button> _)
        {
            Console.WriteLine($"CreateSprint called! Sprint name: '{newSprintName.Value}', Goal: '{newSprintGoal.Value}'");

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

                // Broadcast the new sprint to Sprint Board app
                await currentSprintSignal.Send(sprint);

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

                new Button("+ Add Epic", () => { newIssueType.Set(IssueType.Epic); isAddItemModalOpen.Set(true); }).Primary(),

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
                new Card(Text.P("No epics yet. Click '+ Add Epic' to create your first epic.")) :
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

}
