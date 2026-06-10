using System.Text.Json.Serialization;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.Windows.Forms;
using Vizcon.OSC;
using static System.Net.Mime.MediaTypeNames;
using System.Text.Json.Nodes;
using System.Timers;
using System.Threading;
using static System.Collections.Specialized.BitVector32;

namespace ER_StationAgent
{
    public partial class ER_StationAgent_UI : Form
    {
        // =========================
        // APPLICATION STATE
        // =========================

        // Application configuration settings
        private AppSettings Settings = new();

        // Currently selected station
        string? STATION;

        // Allowed station list for dropdown selection
        private string[] allowedStations = Array.Empty<string>();

        // In-memory message queue
        private MessageStack msgStack = new();

        // In-memory message archive (English/Arabic storage)
        private MessageArchive msgArch = new();

        // Stack Sender Timer
        int SendCycle = 6000; // 6 seconds
        System.Timers.Timer sendTimer = new(6000);

        // =========================
        // NETWORK / COMMUNICATION
        // =========================

        // Shared HTTP client for API communication
        private HttpClient http;

        // Cancellation token source for start/stop control
        private CancellationTokenSource? cts;

        // OSC UDP sender for external system communication
        private UDPSender sender;

        // =========================
        // DATA / SERIALIZATION
        // =========================

        // JSON serializer settings (case-insensitive mapping)
        private readonly JsonSerializerOptions jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        // Database controller instance
        private DbControl db;

        // =========================
        // CONSTRUCTOR
        // =========================

        public ER_StationAgent_UI()
        {
            // Initialize WinForms components
            InitializeComponent();

            // System Variables Init
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Read App Config File
            Settings = config.Get<AppSettings>()!;

            // OSC Init (multicast sender)
            sender = new(Settings.Ventuz.MulticastIp, Settings.Ventuz.PortNo);
            sendTimer.Elapsed += OnTimedEvent;

            // UI Initialization
            LoadStations();
            GridInit();
            btnStop.Enabled = false;
            cmbStation.SelectedIndex = -1;

            // Rich Text Box Context Menu - Clearing
            rtbLog.ContextMenuStrip = rtbContextMenu;

            // API client initialization
            http = new HttpClient
            {
                BaseAddress = new Uri(Settings.Api.BaseUrl),
                Timeout = Timeout.InfiniteTimeSpan
            };

            // Database initialization
            db = new DbControl("station.db");

            // Connect Archive Event to Send Trigger
            msgArch.ArchRefresh += SendArchToVentuz;
        }

        // =========================
        // UI EVENTS
        // =========================

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Clear UI Log
            rtbLog.Clear();
        }
        private void cmbStation_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbStation.SelectedIndex == -1) return;

            // Update current station from dropdown selection
            STATION = cmbStation.SelectedItem?.ToString();

            // Ignore empty or invalid selection
            if (string.IsNullOrWhiteSpace(STATION))
                return;

            // Load messages for selected station
            LoadMessages();
        }
        private void btnStart_Click(object? sender, EventArgs e)
        {
            if (cmbStation.SelectedIndex == -1)
            {
                Log("Error: Please Select A Station");
                return;
            }

            // Prevent multiple starts
            if (cts != null) return;

            // Ensure station is selected
            if (string.IsNullOrWhiteSpace(STATION))
                return;

            // Create cancellation token for listener loop
            cts = new CancellationTokenSource();

            Log($"Starting listener: {STATION}");

            // Update UI state for running listener
            btnStart.Enabled = false;
            btnStop.Enabled = true;
            cmbStation.Enabled = false;

            // Start background listener loop
            _ = Task.Run(() => RunLoop(STATION, cts.Token));
        }
        private void btnStop_Click(object? sender, EventArgs e)
        {
            // Request cancellation of the running listener loop
            cts?.Cancel();

            // Cancel any ongoing HTTP requests
            http.CancelPendingRequests();

            // Log stop action (may take time due to reconnect delay)
            Log("Stopping... might take upto 30s...");
        }
        private void dataGridViewEn_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            // Record successful cell edit.
            Log($"Cell [{e.RowIndex},{e.ColumnIndex}] edit has been applied.");

            // Persist changes to storage.
            // msgStack.Save(STATION!);

            // Push updated data to Ventuz.
            SendMsgToVentuz(STATION!);
        }
        private void btnSetSendCycle_Click(object sender, EventArgs e)
        {
            SendCycle = int.Parse(tbSendCycle.Text);
            sendTimer.Interval = SendCycle;
        }

        // =========================
        // INITIALIZATION
        // =========================

        private void LoadStations()
        {
            var stationFile = Path.Combine(AppContext.BaseDirectory, "Stations.txt");

            if (File.Exists(stationFile))
            {
                allowedStations = File.ReadAllLines(stationFile)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .ToArray();
            }
            else
            {
                Log("Stations.txt not found.");
            }

            // Populate station dropdown with allowed stations
            cmbStation.Items.AddRange(allowedStations);

            // Select first station by default
            cmbStation.SelectedIndex = 0;
        }
        private void GridInit()
        {
            // Display Ventuz multicast configuration
            lblMulticastIp.Text = Settings.Ventuz.MulticastIp;
            lblPort.Text = Settings.Ventuz.PortNo.ToString();

            // Display storage path
            lblAssetPath.Text = Settings.Storage.StoragePath;

            // Bind message stacks to data grids
            dataGridViewEn.DataSource = msgStack.MessageQueue;

            // Configure English grid layout + Limit interaction
            dataGridViewEn.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridViewEn.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dataGridViewEn.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dataGridViewEn.RowHeadersVisible = false;
            dataGridViewEn.Columns[2].ReadOnly = true;
            dataGridViewEn.Columns[3].ReadOnly = true;
        }
        private void LoadMessages()
        {
            // Clear current message stack before loading new data
            msgStack.Clear();

            // Load messages from station-specific files (EN + AR)
            msgStack.LoadFromFiles(
                $"Messages\\{STATION}_Messages.txt");
        }

        // =========================
        // CORE LOOP
        // =========================

        private async Task RunLoop(string station, CancellationToken ct)
        {
            // Initial reconnect delay
            var backoff = TimeSpan.FromSeconds(1);
            var maxBackoff = TimeSpan.FromSeconds(30);

            // Update UI to connected state
            SafeUI(() =>
            {
                ConnIndi.BackColor = Color.FromArgb(34, 197, 94);
                connLabel.Text = "Connected";
            });

            // Main listener loop
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    // Start listening to stream
                    await ListenAsync(station, ct);

                    // Reset backoff after successful connection
                    backoff = TimeSpan.FromSeconds(1);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Log($"STREAM ERROR: {ex.Message}");
                }

                if (ct.IsCancellationRequested)
                    break;

                // Wait before reconnecting
                Log($"Reconnecting in {backoff.TotalSeconds:0}s...");
                await Task.Delay(backoff, ct);

                // Exponential backoff
                backoff = TimeSpan.FromTicks(
                    Math.Min(backoff.Ticks * 2, maxBackoff.Ticks));
            }

            // Update UI to disconnected state
            SafeUI(() =>
            {
                btnStart.Enabled = true;
                btnStop.Enabled = false;
                cmbStation.Enabled = true;
                ConnIndi.BackColor = Color.FromArgb(239, 68, 68);
                connLabel.Text = "Disconnected";
                cts = null;
            });

            Log("Listener stopped.");
        }
        private async Task ListenAsync(string station, CancellationToken ct)
        {
            // Build SSE endpoint URL
            var url = $"{Settings.Api.BaseUrl}/api/realtime/station-{station}";

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue("text/event-stream"));

            // Open streaming connection
            using var res = await http.SendAsync(
                req,
                HttpCompletionOption.ResponseHeadersRead,
                ct);

            res.EnsureSuccessStatusCode();

            Log("Connected to stream.");

            // Read stream content
            using var stream = await res.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream);

            string? eventName = null;
            var buffer = new StringBuilder();

            // Process incoming SSE messages
            while (!ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();
                if (line == null) break;

                // Empty line indicates end of event
                if (line.Length == 0)
                {
                    if (buffer.Length > 0)
                    {
                        await HandleFrame(eventName ?? "message", buffer.ToString());
                        buffer.Clear();
                        eventName = null;
                    }
                    continue;
                }

                // Read event type
                if (line.StartsWith("event:"))
                    eventName = line[6..].Trim();

                // Read event data
                else if (line.StartsWith("data:"))
                    buffer.AppendLine(line[5..].TrimStart());
            }
        }

        // =========================
        // FRAME HANDLING
        // =========================

        private async Task HandleFrame(string eventName, string data)
        {
            // Process only change events
            if (eventName != "change")
                return;

            BusEnvelope? env;

            try
            {
                // Deserialize incoming frame
                env = JsonSerializer.Deserialize<BusEnvelope>(data, jsonOpts);
            }
            catch (Exception ex)
            {
                Log($"Bad frame: {ex.Message}");
                return;
            }

            // Ignore unrelated tables
            if (env?.Table != "delivery_events")
                return;

            // Process inserts only
            if (env.Event != "INSERT")
                return;

            var row = env.New;
            if (row == null)
                return;

            // Filter packages for this station
            var isMatch = string.Equals(row.Station, STATION, StringComparison.OrdinalIgnoreCase);
            if (!isMatch) return;

            switch (row.Kind)
            {
                case "asset":
                    // Handle asset payload
                    var apl = row.Payload.Deserialize<AssetPayload>();
                    await AssetDownload(apl!);
                    break;

                case "deployment":
                    // Handle deployment payload
                    var dpl = row.Payload.Deserialize<DeploymentPayload>();
                    AssetCheck(dpl!);

                    _ = Task.Run(() =>
                    {
                        // JSON serializer settings
                        JsonSerializerOptions opts = new()
                        {
                            WriteIndented = true
                        };

                        // Send deployment OSC package
                        var oscMsgD = new OscMessage("/DEPLOYMENT", JsonSerializer.Serialize(row.Payload, opts));
                        sender.Send(oscMsgD);
                    });

                    break;

                case "message":
                    // Handle message payload
                    var mpl = row.Payload.Deserialize<MessagePayload>();

                    // Null Check the payload
                    if (mpl == null)
                    {
                        Log("Message Payload is Null");
                        return;
                    }

                    // Add message to Archive - regardless of station of origin
                    AddToArchive(mpl);

                    // Filter messages by station of origin for breaking news
                    var isMatch2 = string.Equals(mpl!.Station, STATION, StringComparison.OrdinalIgnoreCase);
                    if (!isMatch2) return;

                    _ = Task.Run(() =>
                    {
                        // Add message to Stack
                        AddToStack(mpl);

                        if (sendTimer.Enabled == false)
                        {
                            StartTimer();
                        }
                    });

                    break;

                case "message-remove":

                    Log("Removing message...");
                    // Handle message payload
                    var ampl = row.Payload.Deserialize<MessagePayload>();

                    // Filter messages for this station
                    //var isMatch3 = string.Equals(ampl!.Station, STATION, StringComparison.OrdinalIgnoreCase);
                    //if (!isMatch3) return;

                    var payloadRef = row.PayloadRef;

                    MessageItem? getResult = db.GetMessage(payloadRef!);

                    if (getResult == null)
                    {
                        Log($"No entry found for {payloadRef}");
                        return;
                    }

                    Log($"Deleting {getResult.Name}'s Message:{getResult.Message}...");
                    msgStack.Remove(getResult.Name!, getResult.Message!);
                    msgArch.Remove(getResult.Name!, getResult.Message!);
                    db.Delete(payloadRef!);

                    break;
            }

            // Log row details
            PrintRowInfo(row);

            // Store event in database
            _ = Task.Run(() => AddToDB(row));

            // Acknowledge processed event
            if (!string.IsNullOrWhiteSpace(row.Id))
            {
                _ = Task.Run(() => Ack(row.Id));
            }
        }

        // =========================
        // HELPERS
        // =========================

        private void SendMsgToVentuz(string station)
        {
            // Send message OSC update
            var oscMsgM =
                new OscMessage($"/MESSAGES", msgStack.ExportJson(station));
            sender.Send(oscMsgM);
        }
        private void SendArchToVentuz(string language)
        {
            // Send message OSC update
            var oscMsgM =
                new OscMessage($"/ARCHIVE_{language.ToUpper()}", msgArch.ExportJson(language));
            sender.Send(oscMsgM);
        }
        private void PrintRowInfo(DeliveryEvent de)
        {
            Log("--------------------");
            Log($"ID: {de.Id}");
            Log($"Station: {de.Station}");
            Log($"Status: {de.Status}");
            Log($"Kind: {de.Kind}");
            Log($"Payload: {de.Payload}");
            Log("--------------------");
        }
        private async Task AssetDownload(AssetPayload apl)
        {
            // Ignore null payloads
            if (apl == null) return;

            // Build asset URL and local file path
            var assetUrl = apl.ImageUrl;
            var assetName = Settings.Storage.StoragePath + "\\" + apl.ImageFilename;

            // Download asset file
            using var http = new HttpClient();
            using var response = await http.GetAsync(assetUrl);
            response.EnsureSuccessStatusCode();

            // Save asset to local storage
            await using var stream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = new FileStream(assetName, FileMode.Create);
            await stream.CopyToAsync(fileStream);
        }
        private async Task AssetDownload(string imageUrl, string imageFileName)
        {
            // Validate input parameters
            if (imageUrl == null || imageFileName == null) return;

            // Assign source URL and destination file path
            var assetUrl = imageUrl;
            var assetName = imageFileName;

            // Download asset file
            using var http = new HttpClient();
            using var response = await http.GetAsync(assetUrl);
            response.EnsureSuccessStatusCode();

            // Save file to disk
            await using var stream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = new FileStream(assetName, FileMode.Create);
            await stream.CopyToAsync(fileStream);
        }
        private bool AssetCheck(DeploymentPayload dpl)
        {
            // Validate deployment payload
            if (dpl == null) return false;

            // No asset required
            if (dpl.ImageUrl == null) return true;

            // Build asset URL and local file path
            var assetUrl = dpl.ImageUrl;
            var assetName = Settings.Storage.StoragePath + "\\" + dpl.ImageFilename;

            // Check if asset already exists
            if (File.Exists(assetName))
            {
                Log(assetName + " - Exists in storage");
                return true;
            }
            else
            {
                Log(assetName + " - Does not exist in storage");
                Log("Downloading Asset...");

                // Download missing asset in background
                _ = Task.Run(() => AssetDownload(assetUrl!, assetName));
                return true;
            }
        }
        private void AddToDB(DeliveryEvent delEvent)
        {
            // Validate required event fields
            if (delEvent == null ||
                delEvent.Id == null ||
                delEvent.Station == null ||
                delEvent.Kind == null ||
                delEvent.PayloadRef == null ||
                delEvent.CreatedAt == null) return;

            // Parse event timestamp
            var ts = DateTime.Parse(
                delEvent.CreatedAt,
                null,
                System.Globalization.DateTimeStyles.AdjustToUniversal
            );

            // Store event in database
            db.Post(delEvent.Id, delEvent.Station, delEvent.Kind, delEvent.PayloadRef, delEvent.Payload, ts);

            Log("Entry added to Database");
        }
        private void AddToStack(MessagePayload mpl)
        {
            // Execute on UI thread
            this.Invoke(() =>
            {
                // Add message to in-memory stack
                msgStack.Push(
                    mpl!.Name!,
                    mpl.Message!,
                    mpl.Station!,
                    mpl.Language!,
                    mpl.Timestamp
                );

                // Persist stack to storage
                msgStack.Save(STATION!);
            });
        }
        private void AddToArchive(MessagePayload mpl)
        {
            // Execute on UI thread
            this.Invoke(() =>
            {
                // Add message to in-memory stack
                msgArch.Push(
                    mpl!.Name!,
                    mpl.Message!,
                    mpl.Station!,
                    mpl.Language!,
                    mpl.Timestamp
                );

                // Persist stack to storage
                msgArch.Save(mpl.Language!);
            });
        }
        private async Task Ack(string eventId)
        {
            try
            {
                // Create HTTP request for ACK endpoint
                var req = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"{Settings.Api.BaseUrl}api/public/ack-delivery");

                // Add station authentication token
                req.Headers.Add("X-Station-Ack-Token", Settings.Api.AckToken);

                // Set request body with event ID
                req.Content = new StringContent(
                    JsonSerializer.Serialize(new { event_id = eventId }),
                    Encoding.UTF8,
                    "application/json");

                // Send ACK request
                var res = await http.SendAsync(req);
                var body = await res.Content.ReadAsStringAsync();

                // Log result based on response status
                if (!res.IsSuccessStatusCode)
                    Log($"ACK FAILED {eventId}: {body}");
                else
                    Log($"ACK OK {eventId}");
            }
            catch (Exception ex)
            {
                // Log any unexpected errors during ACK
                Log($"ACK ERROR {eventId}: {ex.Message}");
            }
        }
        private void SafeUI(Action action)
        {
            // Ensure UI updates happen on UI thread
            if (InvokeRequired)
                BeginInvoke(action);
            else
                action();
        }
        private void Log(string msg)
        {
            // Safely append log message to UI
            SafeUI(() =>
            {
                rtbLog.AppendText(
                    $"[{DateTime.Now:HH:mm:ss}] {msg}\n");
            });
        }
        public void StartTimer()
        {
            sendTimer.AutoReset = true;
            sendTimer.Enabled = true;

            // immediate first run
            OnTimedEvent(null, null);
        }
        private void OnTimedEvent(object? objSender, ElapsedEventArgs? e)
        {
            if (msgStack.MessageQueue.Count > 0)
            {
                SafeUI(() =>
                {
                    var s = msgStack.ExportJson(STATION!);
                    //Log(s);

                    SendMsgToVentuz(STATION!);

                    msgStack.Trim();

                    msgStack.Save(STATION!);
                });
            }
            else
            {
                var oscEnd = new OscMessage($"/MESSAGES_OVER", true);
                sender.Send(oscEnd);

                sendTimer.Stop();
                sendTimer.Enabled = false;
                Log($"Timer Status: {sendTimer.Enabled}");
            }
        }
    }

    // =========================
    // MODELS
    // =========================

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
        // Payload type
        [JsonPropertyName("kind")] public string? Kind { get; set; }

        // Image download URL
        [JsonPropertyName("image_url")] public string? ImageUrl { get; set; }

        // Event timestamp
        [JsonPropertyName("timestamp")] public DateTime Timestamp { get; set; }

        // Runtime instance name
        [JsonPropertyName("instance_name")] public string? InstanceName { get; set; }

        // Template identifier
        [JsonPropertyName("template_name")] public string? TemplateName { get; set; }

        // Local filename for storage
        [JsonPropertyName("image_filename")] public string? ImageFilename { get; set; }
    }
    public sealed class DeploymentPayload
    {
        // Payload type
        [JsonPropertyName("kind")] public string? Kind { get; set; }

        // Arabic text content
        [JsonPropertyName("text_ar")] public string? TextAr { get; set; }

        // English text content
        [JsonPropertyName("text_en")] public string? TextEn { get; set; }

        // Optional image URL
        [JsonPropertyName("image_url")] public string? ImageUrl { get; set; }

        // Event timestamp
        [JsonPropertyName("timestamp")] public DateTime Timestamp { get; set; }

        // Runtime instance name
        [JsonPropertyName("instance_name")] public string? InstanceName { get; set; }

        // Template identifier
        [JsonPropertyName("template_name")] public string? TemplateName { get; set; }

        // Local filename for storage
        [JsonPropertyName("image_filename")] public string? ImageFilename { get; set; }
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

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        public MessageItem(
            string name,
            string message,
            string station,
            DateTime timestamp)
        {
            Name = name;
            Message = message;
            Station = station;
            Timestamp = timestamp;
        }

        // Required for JsonSerializer.Deserialize()
        public MessageItem()
        {
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