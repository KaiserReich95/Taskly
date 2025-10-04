namespace Taskly.Connections;

using Taskly.Models;

[Signal(BroadcastType.Server)]
public class BacklogItemsSignal : AbstractSignal<ImmutableArray<BacklogItem>, bool> { }

[Signal(BroadcastType.Server)]
public class CurrentSprintSignal : AbstractSignal<Sprint, bool> { }

[Signal(BroadcastType.Server)]
public class ArchivedSprintsSignal : AbstractSignal<ImmutableArray<Sprint>, bool> { }
