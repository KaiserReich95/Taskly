namespace Taskly.Apps;
using Taskly.Models;
using Taskly.Connections;

[App(icon: Icons.Kanban)]
public class SprintBoardApp : ViewBase
{
    public override object Build()
    {
        // Local state that will be updated by signals
        var lastBacklogItems = UseState(ImmutableArray<BacklogItem>.Empty);
        var backlogItems = UseState(ImmutableArray<BacklogItem>.Empty);
        var currentSprint = UseState<Sprint>(() => null!);
        var archivedSprints = UseState(ImmutableArray<Sprint>.Empty);

        // Use signal receivers to get state from Taskly app (Server broadcast allows cross-app access)
        var backlogItemsReceiver = Context.UseSignal<BacklogItemsSignal, ImmutableArray<BacklogItem>, bool>();
        var currentSprintReceiver = Context.UseSignal<CurrentSprintSignal, Sprint, bool>();
        var archivedSprintsReceiver = Context.UseSignal<ArchivedSprintsSignal, ImmutableArray<Sprint>, bool>();

        // Receive state updates from Taskly app
        UseEffect(() => backlogItemsReceiver.Receive(backlogItems =>
        {
            Console.WriteLine($"Received {backlogItems.Length} backlog items");

            lastBacklogItems.Set(backlogItems);
            return true;
        }));

        UseEffect(() => currentSprintReceiver.Receive(value =>
        {
            Console.WriteLine($"SprintBoard received sprint: {value?.Name} with {value?.ItemIds.Length ?? 0} items");
            currentSprint.Set(value);
            return true;
        }));

        UseEffect(() => archivedSprintsReceiver.Receive(value =>
        {
            archivedSprints.Set(value);
            return true;
        }));

        // Create signal senders to send updates back to Taskly app
        var backlogItemsSender = Context.CreateSignal<BacklogItemsSignal, ImmutableArray<BacklogItem>, bool>();
        var currentSprintSender = Context.CreateSignal<CurrentSprintSignal, Sprint, bool>();
        var archivedSprintsSender = Context.CreateSignal<ArchivedSprintsSignal, ImmutableArray<Sprint>, bool>();

        // Helper method to update item status and send back via signal
        async void UpdateItemStatus(int id, ItemStatus newStatus)
        {
            var updatedItems = lastBacklogItems.Value
                .Select(item => item.Id == id ? item with { Status = newStatus } : item)
                .ToImmutableArray();

            lastBacklogItems.Set(updatedItems);
            await backlogItemsSender.Send(updatedItems);
        }

        async void ArchiveSprint()
        {
            if (currentSprint.Value != null)
            {
                // Add current sprint to archived sprints
                var updatedArchived = archivedSprints.Value.Add(currentSprint.Value);
                archivedSprints.Set(updatedArchived);
                await archivedSprintsSender.Send(updatedArchived);

                // Clear current sprint
                Sprint? nullSprint = null;
                currentSprint.Set(nullSprint!);
                await currentSprintSender.Send(nullSprint!);
            }
        }

        // Get all items in sprint (stories only, since only stories are added to sprints)
        var sprintStories = lastBacklogItems.Value.Where(item =>
            currentSprint.Value != null &&
            currentSprint.Value.ItemIds.Contains(item.Id) &&
            item.Type == IssueType.Story).ToImmutableArray();

        // Get all tasks/bugs that belong to stories in the sprint
        var allTasks = lastBacklogItems.Value.Where(item =>
            (item.Type == IssueType.Task || item.Type == IssueType.Bug) &&
            sprintStories.Any(story => story.Id == item.ParentId)).ToImmutableArray();

        // Group tasks by status
        var todoTasks = allTasks.Where(item => item.Status == ItemStatus.Todo).ToArray();
        var inProgressTasks = allTasks.Where(item => item.Status == ItemStatus.InProgress).ToArray();
        var doneTasks = allTasks.Where(item => item.Status == ItemStatus.Done).ToArray();

        // Helper to build task/bug card
        object BuildTaskCard(BacklogItem task)
        {
            return new Card(
                Layout.Vertical(
                    // Header with issue type and title
                    Layout.Horizontal(
                        GetIssueTypeBadge(task.Type),
                        Text.Strong(task.Title).Width(Size.Grow())
                    ),

                    // Description if available
                    !string.IsNullOrEmpty(task.Description) ?
                        Text.P(task.Description) : null,

                    // Footer with story points and action buttons
                    Layout.Horizontal(
                        new Badge($"{task.StoryPoints} pts").Primary(),

                        // Status progression buttons
                        task.Status == ItemStatus.Todo ?
                            new Button("Start", () => UpdateItemStatus(task.Id, ItemStatus.InProgress)).Primary().Small() :
                        task.Status == ItemStatus.InProgress ?
                            Layout.Horizontal(
                                new Button("← Reverse", () => UpdateItemStatus(task.Id, ItemStatus.Todo)).Destructive().Small(),
                                new Button("Complete", () => UpdateItemStatus(task.Id, ItemStatus.Done)).Primary().Small()
                            ).Gap(4) :
                        task.Status == ItemStatus.Done ?
                            new Button("← Reverse", () => UpdateItemStatus(task.Id, ItemStatus.InProgress)).Destructive().Small() :
                        null
                    ).Gap(8)
                ).Gap(8)
            );
        }

        // Helper to build Epic/Story hierarchy with kanban columns inside each story
        object BuildHierarchyPanel()
        {
            // Group stories by their parent epic
            var epicGroups = sprintStories
                .GroupBy(story => story.ParentId)
                .OrderBy(g => g.Key);

            return epicGroups.Any() ?
                Layout.Vertical(
                    epicGroups.Select(epicGroup =>
                    {
                        var epic = lastBacklogItems.Value.FirstOrDefault(e => e.Id == epicGroup.Key);
                        var stories = epicGroup.ToArray();

                        return new Card(
                            Layout.Vertical(
                                // Epic header
                                Layout.Horizontal(
                                    GetIssueTypeBadge(IssueType.Epic),
                                    Text.Strong(epic?.Title ?? "Unknown Epic").Width(Size.Grow())
                                ),

                                // Stories within epic
                                Layout.Vertical(
                                    stories.Select(story =>
                                    {
                                        // Get tasks for this story grouped by status
                                        var storyTasks = allTasks.Where(t => t.ParentId == story.Id).ToArray();
                                        var todoTasks = storyTasks.Where(t => t.Status == ItemStatus.Todo).ToArray();
                                        var inProgressTasks = storyTasks.Where(t => t.Status == ItemStatus.InProgress).ToArray();
                                        var doneTasks = storyTasks.Where(t => t.Status == ItemStatus.Done).ToArray();

                                        return new Card(
                                            Layout.Vertical(
                                                // Story header
                                                Layout.Horizontal(
                                                    GetIssueTypeBadge(IssueType.Story),
                                                    Text.Strong(story.Title).Width(Size.Grow())
                                                ),

                                                // Kanban columns for this story's tasks
                                                Layout.Horizontal(
                                                    // To Do Column
                                                    Layout.Vertical(
                                                        Text.H4($"To Do ({todoTasks.Length})"),
                                                        todoTasks.Length > 0 ?
                                                            Layout.Vertical(
                                                                todoTasks.Select(BuildTaskCard).ToArray()
                                                            ).Gap(4) :
                                                            Text.Small("No tasks")
                                                    ).Gap(4).Width(Size.Grow()),

                                                    // In Progress Column
                                                    Layout.Vertical(
                                                        Text.H4($"In Progress ({inProgressTasks.Length})"),
                                                        inProgressTasks.Length > 0 ?
                                                            Layout.Vertical(
                                                                inProgressTasks.Select(BuildTaskCard).ToArray()
                                                            ).Gap(4) :
                                                            Text.Small("No tasks")
                                                    ).Gap(4).Width(Size.Grow()),

                                                    // Done Column
                                                    Layout.Vertical(
                                                        Text.H4($"Done ({doneTasks.Length})"),
                                                        doneTasks.Length > 0 ?
                                                            Layout.Vertical(
                                                                doneTasks.Select(BuildTaskCard).ToArray()
                                                            ).Gap(4) :
                                                            Text.Small("No tasks")
                                                    ).Gap(4).Width(Size.Grow())
                                                ).Gap(8)
                                            ).Gap(8)
                                        );
                                    }).ToArray()
                                ).Gap(4)
                            ).Gap(8)
                        );
                    }).ToArray()
                ).Gap(8) :
                new Card(Text.P("No stories in sprint"));
        }

        Badge GetIssueTypeBadge(IssueType type)
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

        return Layout.Vertical(
            Text.H2("Sprint Board"),

            // Show current sprint info or message if no sprint
            currentSprint.Value == null ?
                new Card(
                    Text.P("No active sprint. Create a sprint in the Planning app to get started.")
                ) :
                new Card(
                    Layout.Vertical(
                        Layout.Horizontal(
                            Layout.Vertical(
                                Text.H3($"Active Sprint: {currentSprint.Value.Name}"),
                                !string.IsNullOrEmpty(currentSprint.Value.Goal) ?
                                    Text.P($"Goal: {currentSprint.Value.Goal}") : null,
                                Text.Small($"Tasks/Bugs: {allTasks.Length} | " +
                                          $"To Do: {todoTasks.Length} | " +
                                          $"In Progress: {inProgressTasks.Length} | " +
                                          $"Done: {doneTasks.Length}")
                            ).Width(Size.Grow()),
                            new Button("Archive Sprint", ArchiveSprint).Secondary()
                        )
                    )
                ),

            // Hierarchical Kanban board with columns inside each story
            currentSprint.Value != null && sprintStories.Length > 0 ?
                BuildHierarchyPanel() :
            currentSprint.Value != null ?
                new Card(
                    Text.P("No stories in sprint yet. Add stories from the Planning app.")
                ) : null
        ).Gap(16);
    }
}
