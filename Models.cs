using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace ER_StationAgent
{
    public sealed class BusEnvelope
    {
        // Target table name
        [JsonPropertyName("table")] public string Table { get; set; } = "";

        // Event type (INSERT, UPDATE, DELETE, etc.)
        [JsonPropertyName("event")] public string Event { get; set; } = "";

        // New row data after change
        [JsonPropertyName("new")] public DeliveryEvent? New { get; set; }

        // Old row data before change
        [JsonPropertyName("old")] public DeliveryEvent? Old { get; set; }

        // Event timestamp (unix or server time)
        [JsonPropertyName("ts")] public long Ts { get; set; }
    }
    public sealed class DeliveryEvent
    {
        // Unique event identifier
        [JsonPropertyName("id")] public string? Id { get; set; }

        // Event category type
        [JsonPropertyName("kind")] public string? Kind { get; set; }

        // Target station for event
        [JsonPropertyName("station")] public string? Station { get; set; }

        // Processing status
        [JsonPropertyName("status")] public string? Status { get; set; }

        // Reference to payload object
        [JsonPropertyName("payload_ref")] public string? PayloadRef { get; set; }

        // Raw JSON payload data
        [JsonPropertyName("payload")] public JsonElement Payload { get; set; }

        // Retry attempt counter
        [JsonPropertyName("attempts")] public int Attempts { get; set; }

        // Creation timestamp
        [JsonPropertyName("created_at")] public string? CreatedAt { get; set; }

        // Last update timestamp
        [JsonPropertyName("updated_at")] public string? UpdatedAt { get; set; }
    }

    public sealed class AssetPayload
    {
        [JsonPropertyName("kind")]
        public string Kind { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("variables")]
        public List<VariableDefinition> Variables { get; set; } = [];

        [JsonPropertyName("instance_name")]
        public string InstanceName { get; set; } = string.Empty;

        [JsonPropertyName("template_name")]
        public string TemplateName { get; set; } = string.Empty;
    }
    public sealed class DeploymentTemplatePayload
    {
        [JsonPropertyName("kind")]
        public string Kind { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("variables")]
        public List<VariableDefinition> Variables { get; set; } = [];

        [JsonPropertyName("instance_name")]
        public string InstanceName { get; set; } = string.Empty;

        [JsonPropertyName("template_name")]
        public string TemplateName { get; set; } = string.Empty;
    }
    public sealed class DeploymentPlaylistPayload
    {
        [JsonPropertyName("kind")]
        public string Kind { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("playlist_id")]
        public string PlaylistId { get; set; } = string.Empty;

        [JsonPropertyName("playlist_name")]
        public string PlaylistName { get; set; } = string.Empty;

        [JsonPropertyName("items")]
        public List<PlaylistItem> Items { get; set; } = [];
    }
    public sealed class PlaylistItem
    {
        [JsonPropertyName("kind")]
        public string Kind { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("position")]
        public int Position { get; set; }

        [JsonPropertyName("duration_s")]
        public int DurationSeconds { get; set; }

        [JsonPropertyName("template_name")]
        public string TemplateName { get; set; } = string.Empty;

        [JsonPropertyName("variables")]
        public List<VariableDefinition> Variables { get; set; } = [];
    }
    public sealed class VariableDefinition
    {
        [JsonPropertyName("kind")]
        public string Kind { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public JsonElement Value { get; set; }
    }
    public sealed class MediaAssetValue
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;

        [JsonPropertyName("filename")]
        public string Filename { get; set; } = string.Empty;
    }

    public sealed class MessagePayload
    {
        // Payload type
        [JsonPropertyName("kind")] public string? Kind { get; set; }

        // Sender name
        [JsonPropertyName("name")] public string? Name { get; set; }

        // Message content
        [JsonPropertyName("message")] public string? Message { get; set; }

        // Target station
        [JsonPropertyName("station")] public string? Station { get; set; }

        // Language code (en/ar/etc.)
        [JsonPropertyName("language")] public string? Language { get; set; }

        // Event timestamp
        [JsonPropertyName("timestamp")] public DateTime Timestamp { get; set; }
    }
    public sealed class MessageItem
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("station")]
        public string? Station { get; set; }

        [JsonPropertyName("language")]
        public string? Language { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        public MessageItem(
            string name,
            string message,
            string station,
            string language,
            DateTime timestamp)
        {
            Name = name;
            Message = message;
            Station = station;
            Language = language;
            Timestamp = timestamp;
        }

        // Required for JsonSerializer.Deserialize()
        public MessageItem()
        {
        }
    }

    public sealed class VentuzPackage
    {
        [JsonPropertyName("template_name")]
        public string TemplateName { get; set; } = string.Empty;

        [JsonPropertyName("variables")]
        public List<VariableDefinition> Variables { get; set; } = [];

        [JsonPropertyName("duration_s")]
        public int? DurationSeconds { get; set; }

        public static VentuzPackage From(PlaylistItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            return new VentuzPackage
            {
                TemplateName = item.TemplateName,
                Variables = item.Variables,
                DurationSeconds = item.DurationSeconds
            };
        }

        public static VentuzPackage From(DeploymentTemplatePayload payload)
        {
            ArgumentNullException.ThrowIfNull(payload);

            return new VentuzPackage
            {
                TemplateName = payload.TemplateName,
                Variables = payload.Variables,
                DurationSeconds = null
            };
        }
    }

    public class AppSettings
    {
        // API configuration
        public ApiSettings Api { get; set; } = new();

        // Storage configuration
        public StorageSettings Storage { get; set; } = new();

        // Ventuz/OSC configuration
        public VentuzSettings Ventuz { get; set; } = new();

        // Station Config
        public string Station { get; set; } = "";

        // Allowed Station List
        public string[] AllowedStations { get; set; } = Array.Empty<string>();
    }
    public class ApiSettings
    {
        // Base API URL
        public string BaseUrl { get; set; } = "";

        // ACK authentication token
        public string AckToken { get; set; } = "";
    }
    public class StorageSettings
    {
        // Root storage directory
        public string StoragePath { get; set; } = "";
    }
    public class VentuzSettings
    {
        // Multicast IP address
        public string MulticastIp { get; set; } = "";

        // UDP port number
        public int PortNo { get; set; }
    }
}
