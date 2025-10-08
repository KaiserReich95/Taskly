using Taskly.Database;

namespace Taskly.Database;

/// <summary>
/// Utility methods to clean/reset database tables
/// </summary>
public class CleanDatabase
{
    /// <summary>
    /// Delete all data from all tables (WARNING: This cannot be undone!)
    /// </summary>
    public static async Task CleanAllTables()
    {
        try
        {
            Console.WriteLine("⚠️  WARNING: Cleaning all database tables...");

            var client = await InitDatabase.GetClient();

            // Delete all backlog items
            var items = await InitDatabase.GetAllBacklogItems();
            foreach (var item in items)
            {
                await client
                    .From<BacklogItemModel>()
                    .Where(x => x.Id == item.Id)
                    .Delete();
            }
            Console.WriteLine($"✓ Deleted {items.Count} backlog items");

            // Delete all sprints
            var sprints = await InitDatabase.GetAllSprints();
            foreach (var sprint in sprints)
            {
                await client
                    .From<SprintModel>()
                    .Where(x => x.Id == sprint.Id)
                    .Delete();
            }
            Console.WriteLine($"✓ Deleted {sprints.Count} sprints");

            // Delete all developers
            var developers = await client.From<DeveloperModel>().Get();
            foreach (var developer in developers.Models)
            {
                await client
                    .From<DeveloperModel>()
                    .Where(x => x.Id == developer.Id)
                    .Delete();
            }
            Console.WriteLine($"✓ Deleted {developers.Models.Count} developers");

            Console.WriteLine("✓ Database cleaned successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error cleaning database: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Delete only backlog items (keeps sprints)
    /// </summary>
    public static async Task CleanBacklogItems()
    {
        try
        {
            Console.WriteLine("Cleaning backlog items...");

            var items = await InitDatabase.GetAllBacklogItems();
            foreach (var item in items)
            {
                await InitDatabase.DeleteBacklogItem(item.Id);
            }

            Console.WriteLine($"✓ Deleted {items.Count} backlog items");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error cleaning backlog items: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Delete only sprints (keeps backlog items)
    /// </summary>
    public static async Task CleanSprints()
    {
        try
        {
            Console.WriteLine("Cleaning sprints...");

            var client = await InitDatabase.GetClient();
            var sprints = await InitDatabase.GetAllSprints();

            foreach (var sprint in sprints)
            {
                await client
                    .From<SprintModel>()
                    .Where(x => x.Id == sprint.Id)
                    .Delete();
            }

            Console.WriteLine($"✓ Deleted {sprints.Count} sprints");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error cleaning sprints: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Reset database to empty state and restart sequences
    /// </summary>
    public static async Task ResetDatabase()
    {
        try
        {
            Console.WriteLine("⚠️  WARNING: Resetting entire database...");

            // Clean all tables first
            await CleanAllTables();

            Console.WriteLine("✓ Database reset complete!");
            Console.WriteLine("Note: Sequence/auto-increment IDs are not reset. To reset them, run these SQL commands:");
            Console.WriteLine("  ALTER SEQUENCE backlog_items_id_seq RESTART WITH 1;");
            Console.WriteLine("  ALTER SEQUENCE sprints_id_seq RESTART WITH 1;");
            Console.WriteLine("  ALTER SEQUENCE developers_id_seq RESTART WITH 1;");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error resetting database: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Interactive prompt to confirm database cleaning
    /// </summary>
    public static async Task<bool> PromptAndClean()
    {
        Console.WriteLine("⚠️  WARNING: This will delete ALL data from the database!");
        Console.Write("Are you sure you want to continue? (type 'yes' to confirm): ");

        var confirmation = Console.ReadLine()?.Trim().ToLower();

        if (confirmation == "yes")
        {
            await CleanAllTables();
            return true;
        }
        else
        {
            Console.WriteLine("Operation cancelled.");
            return false;
        }
    }
}
