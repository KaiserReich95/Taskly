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
        int? SprintId = null
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

        return Layout.Vertical(
            Text.H1("Taskly - Backlog & Sprint Planning"),
            new TabsLayout(
                onSelect: e => { selectedTab.Set(e.Value); return ValueTask.CompletedTask; },
                onClose: null,
                onRefresh: null,
                onReorder: null,
                selectedIndex: selectedTab.Value,
                new Tab("Backlog", BuildBacklogView(backlogItems, currentSprint)),
                new Tab("Sprint Board", BuildSprintBoardView(backlogItems, currentSprint))
            )
        );
    }

    private object BuildBacklogView(IState<ImmutableArray<BacklogItem>> backlogItems, IState<Sprint> currentSprint)
    {
        var nextId = UseState(1);

        // State for adding new items
        var newTitle = UseState("");
        var newDescription = UseState("");
        var newStoryPoints = UseState(1);
        var newIssueType = UseState(IssueType.Task);

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
                var newItem = new BacklogItem(
                    Id: nextId.Value,
                    Title: newTitle.Value,
                    Description: newDescription.Value,
                    StoryPoints: newStoryPoints.Value,
                    Priority: backlogItems.Value.Length + 1,
                    Status: ItemStatus.Backlog,
                    Type: newIssueType.Value
                );

                backlogItems.Set(backlogItems.Value.Add(newItem));
                nextId.Set(nextId.Value + 1);

                // Clear form
                newTitle.Set("");
                newDescription.Set("");
                newStoryPoints.Set(1);
                newIssueType.Set(IssueType.Task);
            }
        }

        void DeleteItem(int id)
        {
            backlogItems.Set(backlogItems.Value.Where(item => item.Id != id).ToImmutableArray());
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

        void AddItemToSprint(int itemId)
        {
            if (currentSprint.Value != null)
            {
                // Update item status and assign to sprint
                var updatedItems = backlogItems.Value
                    .Select(item => item.Id == itemId ?
                        item with { Status = ItemStatus.Todo, SprintId = currentSprint.Value.Id } : item)
                    .ToImmutableArray();

                // Update sprint with new item
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

        return Layout.Vertical(
            Text.H2("Product Backlog"),

            // Add new item form
            new Card(
                Layout.Vertical(
                    Text.H3("Add New Backlog Item"),
                    newTitle.ToTextInput().Placeholder("Enter title..."),
                    newDescription.ToTextInput().Placeholder("Enter description..."),
                    newStoryPoints.ToNumberInput().Min(1).Max(21),
                    new SelectInput<IssueType>(
                        value: newIssueType.Value,
                        onChange: e => { newIssueType.Set(e.Value); return ValueTask.CompletedTask; },
                        options: new[] {
                            new Option<IssueType>("Task", IssueType.Task),
                            new Option<IssueType>("Bug", IssueType.Bug),
                            new Option<IssueType>("Story", IssueType.Story),
                            new Option<IssueType>("Epic", IssueType.Epic)
                        }
                    ),
                    new Button("Add Item", AddItem).Primary()
                )
            ),

            // Sprint management section
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
                        Text.H3($"Current Sprint: {currentSprint.Value.Name}"),
                        !string.IsNullOrEmpty(currentSprint.Value.Goal) ?
                            Text.P($"Goal: {currentSprint.Value.Goal}") : null,
                        Text.Small($"Items in sprint: {currentSprint.Value.ItemIds.Length}"),

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

            // Display all backlog items
            Text.H3($"All Backlog Items ({backlogItems.Value.Length})"),
            Layout.Vertical(
                backlogItems.Value
                    .OrderBy(x => x.Id)
                    .Select(item =>
                {
                    bool isInSprint = item.SprintId != null;

                    return new Card(
                        Layout.Horizontal(
                            // Issue type - compact
                            GetIssueTypeBadge(item.Type),

                            // Title and description - expanded to fill available space
                            Text.Strong(!string.IsNullOrEmpty(item.Description) ?
                                $"{item.Title} - {item.Description}" : item.Title)
                                .Width(Size.Grow()),

                            // Right side controls - compact
                            new Badge($"{item.StoryPoints} pts").Primary(),

                            // Sprint status indicator
                            isInSprint ?
                                new Badge("In Sprint").Secondary() :
                                new Badge("Backlog").Outline(),

                            // Sprint management button
                            !isInSprint && currentSprint.Value != null ?
                                new Button("Add to Sprint", () => AddItemToSprint(item.Id)).Primary().Small() :
                            isInSprint ?
                                new Button("Remove from Sprint", () => RemoveItemFromSprint(item.Id)).Secondary().Small() :
                            null,

                            // Delete button
                            new Button("Delete", () => DeleteItem(item.Id)).Destructive().Small()
                        )
                    );
                }).ToArray()
            )
        );
    }


    private object BuildSprintBoardView(IState<ImmutableArray<BacklogItem>> backlogItems, IState<Sprint> currentSprint)
    {

        // Helper method to update item status
        void UpdateItemStatus(int id, ItemStatus newStatus)
        {
            var updatedItems = backlogItems.Value
                .Select(item => item.Id == id ? item with { Status = newStatus } : item)
                .ToImmutableArray();

            backlogItems.Set(updatedItems);
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

                        // Status progression buttons
                        item.Status == ItemStatus.Todo ?
                            new Button("Start", () => UpdateItemStatus(item.Id, ItemStatus.InProgress)).Primary().Small() :
                        item.Status == ItemStatus.InProgress ?
                            new Button("Complete", () => UpdateItemStatus(item.Id, ItemStatus.Done)).Primary().Small() :
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
                        Text.H3($"Active Sprint: {currentSprint.Value.Name}"),
                        !string.IsNullOrEmpty(currentSprint.Value.Goal) ?
                            Text.P($"Goal: {currentSprint.Value.Goal}") : null,
                        Text.Small($"Total items: {sprintItems.Length} | " +
                                  $"To Do: {todoItems.Length} | " +
                                  $"In Progress: {inProgressItems.Length} | " +
                                  $"Done: {doneItems.Length}")
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
}