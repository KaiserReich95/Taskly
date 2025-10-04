namespace Taskly.Apps;
using Taskly.Models;
using Taskly.Connections;

[App(icon: Icons.Archive)]
public class SprintArchiveApp : ViewBase
{
    public override object Build()
    {
        // Local state that will be updated by signals
        var backlogItems = UseState(ImmutableArray<BacklogItem>.Empty);
        var currentSprint = UseState<Sprint>(() => null!);
        var archivedSprints = UseState(ImmutableArray<Sprint>.Empty);

        // Use signal receivers to get state from Taskly app
        var backlogItemsReceiver = Context.UseSignal<BacklogItemsSignal, ImmutableArray<BacklogItem>, bool>();
        var currentSprintReceiver = Context.UseSignal<CurrentSprintSignal, Sprint, bool>();
        var archivedSprintsReceiver = Context.UseSignal<ArchivedSprintsSignal, ImmutableArray<Sprint>, bool>();

        // Receive state updates from Taskly app
        UseEffect(() => backlogItemsReceiver.Receive(value =>
        {
            backlogItems.Set(value);
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
        var currentSprintSender = Context.CreateSignal<CurrentSprintSignal, Sprint, bool>();
        var archivedSprintsSender = Context.CreateSignal<ArchivedSprintsSignal, ImmutableArray<Sprint>, bool>();

        async void RestoreSprint(Sprint sprint)
        {
            // If there's a current sprint, archive it first
            if (currentSprint.Value != null)
            {
                var updatedArchived = archivedSprints.Value.Add(currentSprint.Value);
                archivedSprints.Set(updatedArchived);
                await archivedSprintsSender.Send(updatedArchived);
            }

            // Remove the sprint from archives and make it current
            var newArchivedSprints = archivedSprints.Value.Remove(sprint);
            archivedSprints.Set(newArchivedSprints);
            currentSprint.Set(sprint);

            await archivedSprintsSender.Send(newArchivedSprints);
            await currentSprintSender.Send(sprint);
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
            Text.H2("Sprint Archive"),

            archivedSprints.Value.Length == 0 ?
                new Card(
                    Text.P("No archived sprints yet. Archive a sprint from the Backlog or Sprint Board tab to see it here.")
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
        ).Gap(16);
    }
}
