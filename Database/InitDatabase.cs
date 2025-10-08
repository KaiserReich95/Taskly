using Supabase;
using Supabase.Postgrest;
using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;

namespace Taskly.Database;

/// <summary>
/// Database initialization and connection setup for Supabase
/// </summary>
public class InitDatabase
{
    private static Supabase.Client? _client;

    /// <summary>
    /// Initialize and get the Supabase client instance
    /// </summary>
    public static async Task<Supabase.Client> GetClient()
    {
        if (_client != null)
            return _client;

        // Get configuration from environment variables
        var url = Environment.GetEnvironmentVariable("SUPABASE_URL")
            ?? "https://ewhnkdzqvqhgmemkuxlz.supabase.co"; // Fallback to hardcoded URL

        var key = Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY")
            ?? "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImV3aG5rZHpxdnFoZ21lbWt1eGx6Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NTk5MDI1MDYsImV4cCI6MjA3NTQ3ODUwNn0.O6h99Ay9y4yYMkLUdvlhjGEPVXI-BGKKn7oLIeF65b8"; // Fallback to hardcoded key

        var options = new SupabaseOptions
        {
            AutoConnectRealtime = true
        };

        _client = new Supabase.Client(url, key, options);
        await _client.InitializeAsync();

        Console.WriteLine("✓ Connected to Supabase database");
        return _client;
    }

    /// <summary>
    /// Test the database connection
    /// </summary>
    public static async Task<bool> TestConnection()
    {
        try
        {
            var client = await GetClient();

            // Try a simple query to verify connection
            var response = await client
                .From<BacklogItemModel>()
                .Select("id")
                .Limit(1)
                .Get();

            Console.WriteLine("✓ Database connection test successful");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Database connection test failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Get all backlog items
    /// </summary>
    public static async Task<List<BacklogItemModel>> GetAllBacklogItems()
    {
        var client = await GetClient();
        var response = await client.From<BacklogItemModel>().Get();
        return response.Models;
    }

    /// <summary>
    /// Get all sprints
    /// </summary>
    public static async Task<List<SprintModel>> GetAllSprints()
    {
        var client = await GetClient();
        var response = await client.From<SprintModel>().Get();
        return response.Models;
    }

    /// <summary>
    /// Get current (non-archived) sprint
    /// </summary>
    public static async Task<SprintModel?> GetCurrentSprint()
    {
        var client = await GetClient();
        var response = await client
            .From<SprintModel>()
            .Where(x => x.IsArchived == false)
            .Limit(1)
            .Get();

        return response.Models.FirstOrDefault();
    }

    /// <summary>
    /// Create a new backlog item
    /// </summary>
    public static async Task<BacklogItemModel?> CreateBacklogItem(BacklogItemModel item)
    {
        var client = await GetClient();
        var response = await client.From<BacklogItemModel>().Insert(item);
        return response.Models.FirstOrDefault();
    }

    /// <summary>
    /// Update a backlog item
    /// </summary>
    public static async Task<BacklogItemModel?> UpdateBacklogItem(BacklogItemModel item)
    {
        var client = await GetClient();
        var response = await client
            .From<BacklogItemModel>()
            .Update(item);
        return response.Models.FirstOrDefault();
    }

    /// <summary>
    /// Delete a backlog item
    /// </summary>
    public static async Task DeleteBacklogItem(int id)
    {
        var client = await GetClient();
        await client
            .From<BacklogItemModel>()
            .Where(x => x.Id == id)
            .Delete();
    }

    /// <summary>
    /// Create a new sprint
    /// </summary>
    public static async Task<SprintModel?> CreateSprint(SprintModel sprint)
    {
        var client = await GetClient();
        var response = await client.From<SprintModel>().Insert(sprint);
        return response.Models.FirstOrDefault();
    }

    /// <summary>
    /// Update a sprint
    /// </summary>
    public static async Task<SprintModel?> UpdateSprint(SprintModel sprint)
    {
        var client = await GetClient();
        var response = await client
            .From<SprintModel>()
            .Update(sprint);
        return response.Models.FirstOrDefault();
    }

}

// Database Models (matching the schema)

[Table("backlog_items")]
public class BacklogItemModel : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("story_points")]
    public int StoryPoints { get; set; }

    [Column("priority")]
    public int Priority { get; set; }

    [Column("status")]
    public string Status { get; set; } = "Backlog";

    [Column("type")]
    public string Type { get; set; } = "Task";

    [Column("sprint_id")]
    public int? SprintId { get; set; }

    [Column("parent_id")]
    public int? ParentId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

[Table("sprints")]
public class SprintModel : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("start_date")]
    public DateTime StartDate { get; set; }

    [Column("end_date")]
    public DateTime EndDate { get; set; }

    [Column("goal")]
    public string? Goal { get; set; }

    [Column("item_ids")]
    public List<int>? ItemIds { get; set; }

    [Column("is_archived")]
    public bool IsArchived { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

[Table("developers")]
public class DeveloperModel : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("capacity")]
    public int Capacity { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
