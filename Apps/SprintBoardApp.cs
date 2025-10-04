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

        // Get sprint items by status
        var sprintItems = lastBacklogItems.Value.Where(item => currentSprint.Value != null &&
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
}
