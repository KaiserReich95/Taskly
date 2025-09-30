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

        return Layout.Vertical(
            Text.H1("Taskly - Backlog & Sprint Planning"),
            new TabsLayout(
                onSelect: e => { selectedTab.Set(e.Value); return ValueTask.CompletedTask; },
                onClose: null,
                onRefresh: null,
                onReorder: null,
                selectedIndex: selectedTab.Value,
                new Tab("Backlog", BuildBacklogView()),
                new Tab("Sprint Planning", BuildSprintPlanningView()),
                new Tab("Sprint Board", BuildSprintBoardView())
            )
        );
    }

    private object BuildBacklogView()
    {
        // State management for backlog items
        var backlogItems = UseState(ImmutableArray<BacklogItem>.Empty);
        var nextId = UseState(1);

        // State for adding new items
        var newTitle = UseState("");
        var newDescription = UseState("");
        var newStoryPoints = UseState(1);
        var newIssueType = UseState(IssueType.Task);

        // Sprint management
        var currentSprint = UseState((Sprint?)null);
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


        void UpdateItemStatus(int id, ItemStatus newStatus)
        {
            var updatedItems = backlogItems.Value
                .Select(item => item.Id == id ? item with { Status = newStatus } : item)
                .ToImmutableArray();

            backlogItems.Set(updatedItems);
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

        void MoveItemToSprint(int itemId)
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

            // Current Sprint section
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
                                backlogItems.Value
                                    .Where(item => currentSprint.Value.ItemIds.Contains(item.Id))
                                    .OrderBy(x => x.Id)
                                    .Select(item => BuildSprintItemCard(item))
                                    .ToArray()
                            ) :
                            Text.P("No items in sprint yet. Click 'Start' on backlog items to add them.")
                    )
                ),

            // Display existing items (not in sprint)
            Text.H3($"Backlog Items ({backlogItems.Value.Where(x => x.SprintId == null).Count()})"),
            Layout.Vertical(
                backlogItems.Value
                    .Where(item => item.SprintId == null)
                    .OrderBy(x => x.Id)
                    .Select(item =>
                {
                    return new Card(
                        Layout.Horizontal(
                            // Issue type - compact
                            GetIssueTypeBadge(item.Type),

                            // Title and description - expanded to fill available space
                            Text.Strong(!string.IsNullOrEmpty(item.Description) ?
                                $"{item.Title} - {item.Description}" : item.Title)
                                .Width(Size.Grow()),

                            // Right side controls - compact
                            new Badge(item.Status.ToString()).Outline(),
                            new Badge($"{item.StoryPoints} pts").Primary(),

                            // Status change button (only show Start if there's an active sprint)
                            item.Status == ItemStatus.Backlog && currentSprint.Value != null ?
                                new Button("Start", () => MoveItemToSprint(item.Id)).Primary().Small() :
                            item.Status == ItemStatus.Todo ?
                                new Button("Progress", () => UpdateItemStatus(item.Id, ItemStatus.InProgress)).Secondary().Small() :
                            item.Status == ItemStatus.InProgress ?
                                new Button("Review", () => UpdateItemStatus(item.Id, ItemStatus.Review)).Secondary().Small() :
                            item.Status == ItemStatus.Review ?
                                new Button("Done", () => UpdateItemStatus(item.Id, ItemStatus.Done)).Primary().Small() : null,

                            // Delete button
                            new Button("Delete", () => DeleteItem(item.Id)).Destructive().Small()
                        )
                    );
                }).Where(card => card != null).ToArray()
            )
        );

        // Helper method for building sprint item cards
        object BuildSprintItemCard(BacklogItem item)
        {
            return new Card(
                Layout.Horizontal(
                    // Issue type - compact
                    GetIssueTypeBadge(item.Type),

                    // Title and description - expanded to fill available space
                    Text.Strong(!string.IsNullOrEmpty(item.Description) ?
                        $"{item.Title} - {item.Description}" : item.Title)
                        .Width(Size.Grow()),

                    // Right side controls - compact
                    new Badge(item.Status.ToString()).Outline(),
                    new Badge($"{item.StoryPoints} pts").Primary(),

                    // Status change buttons for sprint items
                    item.Status == ItemStatus.Todo ?
                        new Button("In Progress", () => UpdateItemStatus(item.Id, ItemStatus.InProgress)).Secondary().Small() :
                    item.Status == ItemStatus.InProgress ?
                        new Button("Review", () => UpdateItemStatus(item.Id, ItemStatus.Review)).Secondary().Small() :
                    item.Status == ItemStatus.Review ?
                        new Button("Done", () => UpdateItemStatus(item.Id, ItemStatus.Done)).Primary().Small() : null,

                    // Delete button
                    new Button("Delete", () => DeleteItem(item.Id)).Destructive().Small()
                )
            );
        }
    }

    private object BuildSprintPlanningView()
    {
        return Layout.Vertical(
            Text.H2("Sprint Planning"),
            Text.P("Plan your upcoming sprint here")
        );
    }

    private object BuildSprintBoardView()
    {
        return Layout.Vertical(
            Text.H2("Sprint Board"),
            Text.P("Track progress of your current sprint here")
        );
    }
}