using System.Collections.Immutable;

[App(icon: Icons.ListCheck)]
public class SprintBoard : ViewBase
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
        var backlogItems = UseState(ImmutableArray<BacklogItem>.Empty);
        var currentSprint = UseState(() => (Sprint)null!);

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
