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
                    Status: ItemStatus.Backlog
                );

                backlogItems.Set(backlogItems.Value.Add(newItem));
                nextId.Set(nextId.Value + 1);

                // Clear form
                newTitle.Set("");
                newDescription.Set("");
                newStoryPoints.Set(1);
            }
        }

        void DeleteItem(int id)
        {
            backlogItems.Set(backlogItems.Value.Where(item => item.Id != id).ToImmutableArray());
        }

        void MovePriorityUp(int id)
        {
            var items = backlogItems.Value.OrderBy(x => x.Priority).ToArray();
            var currentIndex = Array.FindIndex(items, x => x.Id == id);

            if (currentIndex > 0)
            {
                // Swap priorities
                var currentItem = items[currentIndex];
                var previousItem = items[currentIndex - 1];

                var updatedItems = backlogItems.Value
                    .Select(item => item.Id == currentItem.Id ? item with { Priority = previousItem.Priority } :
                                   item.Id == previousItem.Id ? item with { Priority = currentItem.Priority } : item)
                    .ToImmutableArray();

                backlogItems.Set(updatedItems);
            }
        }

        void MovePriorityDown(int id)
        {
            var items = backlogItems.Value.OrderBy(x => x.Priority).ToArray();
            var currentIndex = Array.FindIndex(items, x => x.Id == id);

            if (currentIndex < items.Length - 1)
            {
                // Swap priorities
                var currentItem = items[currentIndex];
                var nextItem = items[currentIndex + 1];

                var updatedItems = backlogItems.Value
                    .Select(item => item.Id == currentItem.Id ? item with { Priority = nextItem.Priority } :
                                   item.Id == nextItem.Id ? item with { Priority = currentItem.Priority } : item)
                    .ToImmutableArray();

                backlogItems.Set(updatedItems);
            }
        }

        void UpdateItemStatus(int id, ItemStatus newStatus)
        {
            var updatedItems = backlogItems.Value
                .Select(item => item.Id == id ? item with { Status = newStatus } : item)
                .ToImmutableArray();

            backlogItems.Set(updatedItems);
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
                    new Button("Add Item", AddItem).Primary()
                )
            ).Title("New Item"),

            // Display existing items
            Text.H3($"Backlog Items ({backlogItems.Value.Length})"),
            Layout.Vertical(
                backlogItems.Value.OrderBy(x => x.Priority).Select(item =>
                {
                    var isFirst = item.Priority == backlogItems.Value.Min(x => x.Priority);
                    var isLast = item.Priority == backlogItems.Value.Max(x => x.Priority);

                    return new Card(
                        Layout.Vertical(
                            // Header with title and priority info
                            Layout.Horizontal(
                                Text.Strong(item.Title),
                                new Badge($"#{item.Priority}").Secondary()
                            ),

                            // Description
                            !string.IsNullOrEmpty(item.Description) ? Text.P(item.Description) : null,

                            // Status and story points
                            Layout.Horizontal(
                                new Badge(item.Status.ToString()).Outline(),
                                new Badge($"{item.StoryPoints} pts").Primary()
                            ),

                            // Action buttons
                            Layout.Horizontal(
                                // Priority buttons
                                !isFirst ? new Button("↑", () => MovePriorityUp(item.Id)).Small() : null,
                                !isLast ? new Button("↓", () => MovePriorityDown(item.Id)).Small() : null,

                                // Status change buttons
                                item.Status == ItemStatus.Backlog ?
                                    new Button("Start", () => UpdateItemStatus(item.Id, ItemStatus.Todo)).Primary().Small() : null,
                                item.Status == ItemStatus.Todo ?
                                    new Button("In Progress", () => UpdateItemStatus(item.Id, ItemStatus.InProgress)).Secondary().Small() : null,
                                item.Status == ItemStatus.InProgress ?
                                    new Button("Review", () => UpdateItemStatus(item.Id, ItemStatus.Review)).Secondary().Small() : null,
                                item.Status == ItemStatus.Review ?
                                    new Button("Done", () => UpdateItemStatus(item.Id, ItemStatus.Done)).Primary().Small() : null,

                                // Delete button
                                new Button("Delete", () => DeleteItem(item.Id)).Destructive().Small()
                            )
                        )
                    );
                }).Where(card => card != null).ToArray()
            )
        );
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