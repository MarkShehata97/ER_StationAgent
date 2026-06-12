using ER_StationAgent;
using Microsoft.Data.Sqlite;
using System.Text.Json;
using static System.Collections.Specialized.BitVector32;

public class DbControl
{
    // Connection string for SQLite database
    private readonly string _connectionString;

    // =========================
    // CONSTRUCTOR
    // =========================
    public DbControl(string dbPath)
    {
        // Build connection string and initialize DB on startup
        _connectionString = $"Data Source={dbPath}";
        Initialize();
    }

    // =========================
    // INIT DATABASE
    // =========================
    private void Initialize()
    {
        // Create and open SQLite connection
        using var con = new SqliteConnection(_connectionString);
        con.Open();

        // Create command for table initialization
        var cmd = con.CreateCommand();

        // Ensure events table exists (idempotent)
        cmd.CommandText =
        @"
        CREATE TABLE IF NOT EXISTS events (
            id TEXT PRIMARY KEY,
            station TEXT,
            kind TEXT,
            payload_ref TEXT,
            payload TEXT,
            timestamp TEXT
        );
        ";

        // Execute table creation
        cmd.ExecuteNonQuery();
    }

    // =========================
    // CREATE / INSERT
    // =========================
    public void Post(string id, string station, string kind, string payload_ref, object payload, DateTime timestamp)
    {
        // Open database connection
        using var con = new SqliteConnection(_connectionString);
        con.Open();

        // Prepare insert/replace command
        var cmd = con.CreateCommand();

        cmd.CommandText =
        @"
        INSERT OR REPLACE INTO events (id, station, kind, payload_ref, payload, timestamp)
        VALUES ($id, $station, $kind, $payload_ref, $payload, $timestamp);
        ";

        // JSON serialization options for storing payload safely as string
        var options = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = false
        };

        // Bind parameters
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$station", station);
        cmd.Parameters.AddWithValue("$kind", kind);
        cmd.Parameters.AddWithValue("$payload_ref", payload_ref);

        // Serialize object payload into JSON string
        cmd.Parameters.AddWithValue("$payload", JsonSerializer.Serialize(payload, options));

        cmd.Parameters.AddWithValue("$timestamp", timestamp.ToString("o"));

        // Execute insert/update
        cmd.ExecuteNonQuery();
    }

    // =========================
    // READ
    // =========================
    public MessageItem? GetMessage(string payload_ref)
    {
        using var con = new SqliteConnection(_connectionString);
        con.Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        @"
        SELECT payload
        FROM events
        WHERE payload_ref = $payload_ref;
        ";

        cmd.Parameters.AddWithValue("payload_ref", payload_ref);

        var result = cmd.ExecuteScalar()?.ToString();

        if (string.IsNullOrWhiteSpace(result))
            return null;

        var payload = JsonSerializer.Deserialize<MessagePayload>(result);

        if (payload == null)
            return null;

        return new MessageItem(
            payload.Name ?? "",
            payload.Message ?? "",
            payload.Station ?? "",
            payload.Timestamp
        );
    }

    // =========================
    // UPDATE
    // =========================
    public void Put(string id, string newPayloadJson)
    {
        // Open database connection
        using var con = new SqliteConnection(_connectionString);
        con.Open();

        // Prepare update query
        var cmd = con.CreateCommand();

        cmd.CommandText =
        @"
        UPDATE events
        SET payload = $payload
        WHERE id = $id;
        ";

        // Bind parameters
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$payload", newPayloadJson);

        // Execute update
        cmd.ExecuteNonQuery();
    }

    // =========================
    // DELETE
    // =========================
    public void Delete(string payload_ref)
    {
        // Open database connection
        using var con = new SqliteConnection(_connectionString);
        con.Open();

        // Prepare delete query
        var cmd = con.CreateCommand();

        cmd.CommandText =
        @"
        DELETE FROM events
        WHERE payload_ref = $payload_ref;
        ";

        // Bind ID parameter
        cmd.Parameters.AddWithValue("$payload_ref", payload_ref);

        // Execute deletion
        cmd.ExecuteNonQuery();
    }

    // ================================
    // LATEST DEPLOYMENT - UPON RESTART
    // ================================
    public DeliveryEvent? GetLatestDeployment(string station)
    {
        using var con = new SqliteConnection(_connectionString);
        con.Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        @"
        SELECT id, station, kind, payload_ref, payload, timestamp
        FROM events
        WHERE station = $station
          AND kind IN ('deployment-template', 'deployment-playlist')
        ORDER BY timestamp DESC
        LIMIT 1;
        "
        ;

        cmd.Parameters.AddWithValue("$station", station);

        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
            return null;

        return new DeliveryEvent
        {
            Id = reader.GetString(0),
            Station = reader.GetString(1),
            Kind = reader.GetString(2),
            PayloadRef = reader.GetString(3),
            Payload = JsonSerializer.Deserialize<JsonElement>(
                reader.GetString(4)),
            CreatedAt = reader.GetString(5)
        };
    }
}

