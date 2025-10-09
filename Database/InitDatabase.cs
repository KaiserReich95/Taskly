using Microsoft.Data.Sqlite;
using Dapper;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace Taskly.Database;

/// <summary>
/// Database initialization and connection setup for SQLite
/// </summary>
public class InitDatabase
{
    private static string _connectionString = "Data Source=Database/taskly_database.db";

    /// <summary>
    /// Get a new database connection
    /// </summary>
    public static SqliteConnection GetConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();

        // Enable foreign keys for this connection
        connection.Execute("PRAGMA foreign_keys = ON;");

        return connection;
    }

    /// <summary>
    /// Test the database connection
    /// </summary>
    public static bool TestConnection()
    {
        try
        {
            using var connection = GetConnection();
            connection.QueryFirstOrDefault<int>("SELECT COUNT(*) FROM backlog_items");
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
    public static List<BacklogItemModel> GetAllBacklogItems()
    {
        using var connection = GetConnection();
        var sql = @"SELECT
                        id AS Id,
                        title AS Title,
                        description AS Description,
                        story_points AS StoryPoints,
                        priority AS Priority,
                        status AS Status,
                        type AS Type,
                        sprint_id AS SprintId,
                        parent_id AS ParentId,
                        created_at AS CreatedAt,
                        updated_at AS UpdatedAt
                    FROM backlog_items";
        var items = connection.Query<BacklogItemModel>(sql).ToList();

        Console.WriteLine($"GetAllBacklogItems: Retrieved {items.Count} items from database");
        foreach (var item in items)
        {
            Console.WriteLine($"  DB: ID={item.Id}, Type={item.Type}, ParentId={item.ParentId}, Title={item.Title}");
        }

        return items;
    }

    /// <summary>
    /// Get all sprints
    /// </summary>
    public static List<SprintModel> GetAllSprints()
    {
        using var connection = GetConnection();
        var sql = @"SELECT
                        id AS Id,
                        name AS Name,
                        start_date AS StartDate,
                        end_date AS EndDate,
                        goal AS Goal,
                        item_ids AS ItemIdsJson,
                        is_archived AS IsArchived,
                        created_at AS CreatedAt,
                        updated_at AS UpdatedAt
                    FROM sprints";
        var sprints = connection.Query<SprintModel>(sql).ToList();

        // Deserialize JSON item_ids for each sprint
        foreach (var sprint in sprints)
        {
            sprint.ItemIds = string.IsNullOrEmpty(sprint.ItemIdsJson)
                ? new List<int>()
                : JsonConvert.DeserializeObject<List<int>>(sprint.ItemIdsJson) ?? new List<int>();
        }

        return sprints;
    }

    /// <summary>
    /// Get current (non-archived) sprint
    /// </summary>
    public static SprintModel? GetCurrentSprint()
    {
        using var connection = GetConnection();
        var sql = @"SELECT
                        id AS Id,
                        name AS Name,
                        start_date AS StartDate,
                        end_date AS EndDate,
                        goal AS Goal,
                        item_ids AS ItemIdsJson,
                        is_archived AS IsArchived,
                        created_at AS CreatedAt,
                        updated_at AS UpdatedAt
                    FROM sprints
                    WHERE is_archived = 0
                    LIMIT 1";
        var sprint = connection.QueryFirstOrDefault<SprintModel>(sql);

        if (sprint != null)
        {
            sprint.ItemIds = string.IsNullOrEmpty(sprint.ItemIdsJson)
                ? new List<int>()
                : JsonConvert.DeserializeObject<List<int>>(sprint.ItemIdsJson) ?? new List<int>();
        }

        return sprint;
    }

    /// <summary>
    /// Create a new backlog item
    /// </summary>
    public static BacklogItemModel? CreateBacklogItem(BacklogItemModel item)
    {
        using var connection = GetConnection();
        var sql = @"INSERT INTO backlog_items (title, description, story_points, priority, status, type, sprint_id, parent_id)
                    VALUES (@Title, @Description, @StoryPoints, @Priority, @Status, @Type, @SprintId, @ParentId);
                    SELECT
                        id AS Id,
                        title AS Title,
                        description AS Description,
                        story_points AS StoryPoints,
                        priority AS Priority,
                        status AS Status,
                        type AS Type,
                        sprint_id AS SprintId,
                        parent_id AS ParentId,
                        created_at AS CreatedAt,
                        updated_at AS UpdatedAt
                    FROM backlog_items WHERE id = last_insert_rowid();";
        return connection.QueryFirstOrDefault<BacklogItemModel>(sql, item);
    }

    /// <summary>
    /// Update a backlog item
    /// </summary>
    public static BacklogItemModel? UpdateBacklogItem(BacklogItemModel item)
    {
        using var connection = GetConnection();
        var sql = @"UPDATE backlog_items
                    SET title = @Title, description = @Description, story_points = @StoryPoints,
                        priority = @Priority, status = @Status, type = @Type,
                        sprint_id = @SprintId, parent_id = @ParentId
                    WHERE id = @Id;
                    SELECT
                        id AS Id,
                        title AS Title,
                        description AS Description,
                        story_points AS StoryPoints,
                        priority AS Priority,
                        status AS Status,
                        type AS Type,
                        sprint_id AS SprintId,
                        parent_id AS ParentId,
                        created_at AS CreatedAt,
                        updated_at AS UpdatedAt
                    FROM backlog_items WHERE id = @Id;";
        return connection.QueryFirstOrDefault<BacklogItemModel>(sql, item);
    }

    /// <summary>
    /// Delete a backlog item
    /// </summary>
    public static void DeleteBacklogItem(int id)
    {
        using var connection = GetConnection();
        connection.Execute("DELETE FROM backlog_items WHERE id = @Id", new { Id = id });
    }

    /// <summary>
    /// Create a new sprint
    /// </summary>
    public static SprintModel? CreateSprint(SprintModel sprint)
    {
        using var connection = GetConnection();

        // Serialize ItemIds to JSON
        sprint.ItemIdsJson = JsonConvert.SerializeObject(sprint.ItemIds ?? new List<int>());

        // Format DateTime as ISO 8601 string for SQLite
        var sql = @"INSERT INTO sprints (name, start_date, end_date, goal, item_ids, is_archived)
                    VALUES (@Name, @StartDate, @EndDate, @Goal, @ItemIdsJson, @IsArchived);
                    SELECT
                        id AS Id,
                        name AS Name,
                        start_date AS StartDate,
                        end_date AS EndDate,
                        goal AS Goal,
                        item_ids AS ItemIdsJson,
                        is_archived AS IsArchived,
                        created_at AS CreatedAt,
                        updated_at AS UpdatedAt
                    FROM sprints WHERE id = last_insert_rowid();";

        var parameters = new
        {
            sprint.Name,
            StartDate = sprint.StartDate.ToString("yyyy-MM-dd HH:mm:ss"),
            EndDate = sprint.EndDate.ToString("yyyy-MM-dd HH:mm:ss"),
            sprint.Goal,
            sprint.ItemIdsJson,
            sprint.IsArchived
        };

        var result = connection.QueryFirstOrDefault<SprintModel>(sql, parameters);

        if (result != null)
        {
            result.ItemIds = string.IsNullOrEmpty(result.ItemIdsJson)
                ? new List<int>()
                : JsonConvert.DeserializeObject<List<int>>(result.ItemIdsJson) ?? new List<int>();
        }

        return result;
    }

    /// <summary>
    /// Update a sprint
    /// </summary>
    public static SprintModel? UpdateSprint(SprintModel sprint)
    {
        using var connection = GetConnection();

        // Serialize ItemIds to JSON
        sprint.ItemIdsJson = JsonConvert.SerializeObject(sprint.ItemIds ?? new List<int>());

        var sql = @"UPDATE sprints
                    SET name = @Name, start_date = @StartDate, end_date = @EndDate,
                        goal = @Goal, item_ids = @ItemIdsJson, is_archived = @IsArchived
                    WHERE id = @Id;
                    SELECT
                        id AS Id,
                        name AS Name,
                        start_date AS StartDate,
                        end_date AS EndDate,
                        goal AS Goal,
                        item_ids AS ItemIdsJson,
                        is_archived AS IsArchived,
                        created_at AS CreatedAt,
                        updated_at AS UpdatedAt
                    FROM sprints WHERE id = @Id;";

        var parameters = new
        {
            sprint.Id,
            sprint.Name,
            StartDate = sprint.StartDate.ToString("yyyy-MM-dd HH:mm:ss"),
            EndDate = sprint.EndDate.ToString("yyyy-MM-dd HH:mm:ss"),
            sprint.Goal,
            sprint.ItemIdsJson,
            sprint.IsArchived
        };

        var result = connection.QueryFirstOrDefault<SprintModel>(sql, parameters);

        if (result != null)
        {
            result.ItemIds = string.IsNullOrEmpty(result.ItemIdsJson)
                ? new List<int>()
                : JsonConvert.DeserializeObject<List<int>>(result.ItemIdsJson) ?? new List<int>();
        }

        return result;
    }

}

// Database Models (matching the schema)

public class BacklogItemModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    [Column("story_points")]
    public int StoryPoints { get; set; }

    public int Priority { get; set; }
    public string Status { get; set; } = "Backlog";
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

public class SprintModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    [Column("start_date")]
    public DateTime StartDate { get; set; }

    [Column("end_date")]
    public DateTime EndDate { get; set; }

    public string? Goal { get; set; }

    // Not mapped to database - used for serialization
    [Newtonsoft.Json.JsonIgnore]
    [NotMapped]
    public List<int>? ItemIds { get; set; }

    // Database column (JSON string)
    [Column("item_ids")]
    public string ItemIdsJson { get; set; } = "[]";

    // SQLite stores boolean as 0/1
    [Column("is_archived")]
    public int IsArchived { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

public class DeveloperModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
