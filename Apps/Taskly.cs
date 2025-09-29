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
        return Layout.Vertical(
            Text.H1("Taskly - Backlog & Sprint Planning"),
            Text.Literal("Coming soon: Sprint planning board functionality")
        );
    }
}