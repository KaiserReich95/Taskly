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
        return Layout.Vertical(
            Text.H2("Product Backlog"),
            Text.P("Manage your product backlog items here")
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