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
using System;

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

        // Playlist manager instance
        private PlaylistManager? playlistManager;

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
            db = new DbControl("er_database.db");

            // Connect Archive Event to Send Trigger
            msgArch.ArchRefresh += SendArchToVentuz;
        }

        // =========================
        // UI EVENTS
        // =========================
        private void ER_StationAgent_UI_Shown(object sender, EventArgs e)
        {
            Logger.Instance.OnLog += Log;

            int index = cmbStation.FindStringExact(Settings.Station);
            if (index >= 0)
            {
                cmbStation.SelectedIndex = index;
            }

            btnStart_Click(null, EventArgs.Empty);

            _ = RestoreLastDeploymentAsync();
        }
        private void ER_StationAgent_UI_FormClosing(object sender, FormClosingEventArgs e)
        {
            Logger.Instance.Log("Closing App...");
            Logger.Instance.OnLog -= Log;
            btnStop_Click(null, EventArgs.Empty);
        }
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
                Logger.Instance.Log("Error: Please Select A Station");
                return;
            }

            // Prevent multiple starts
            if (cts != null) return;

            // Ensure station is selected
            if (string.IsNullOrWhiteSpace(STATION))
                return;

            // Create cancellation token for listener loop
            cts = new CancellationTokenSource();

            Logger.Instance.Log($"Starting listener: {STATION}");

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

            if (playlistManager != null && playlistManager.IsRunning)
            {
                Logger.Instance.Log("Stopping existing playlist...");
                _ = Task.Run(async () => { await playlistManager.StopAsync(); });
            }

            // Log stop action (may take time due to reconnect delay)
            Logger.Instance.Log("Stopping... might take upto 30s...");
        }
        private void dataGridViewEn_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            // Record successful cell edit.
             Logger.Instance.Log($"Cell [{e.RowIndex},{e.ColumnIndex}] edit has been applied.");

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
            // Populate station dropdown with allowed stations
            cmbStation.Items.AddRange(Settings.AllowedStations);

            // Select first station by default
            cmbStation.SelectedIndex = -1;
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
        private async Task RestoreLastDeploymentAsync()
        {
            try
            {
                // Optional: give startup a moment to settle
                await Task.Delay(1000);

                var deployment = await Task.Run(() =>
                    db.GetLatestDeployment(Settings.Station));

                if (deployment == null)
                    return;

                Logger.Instance.Log(
                    $"Restoring last deployment: {deployment.Kind}");

                switch (deployment.Kind)
                {
                    case "deployment-template":
                        await HandleTemplate(deployment);
                        break;

                    case "deployment-playlist":
                        await HandlePlaylist(deployment);
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Log(
                    $"Failed to restore deployment: {ex.Message}");
            }
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
                    Logger.Instance.Log($"STREAM ERROR: {ex.Message}");
                }

                if (ct.IsCancellationRequested)
                    break;

                // Wait before reconnecting
                Logger.Instance.Log($"Reconnecting in {backoff.TotalSeconds:0}s...");
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

            Logger.Instance.Log("Listener stopped.");
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

            Logger.Instance.Log("Connected to stream.");

            // Read stream content
            using var stream = await res.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream);

            string? eventName = null;
            var buffer = new StringBuilder();

            // Process incoming SSE messages
            while (!ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct);
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
                Logger.Instance.Log($"Bad frame: {ex.Message}");
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
                    await HandleAsset(row);
                    break;

                case "deployment-template":
                    // Handle template deployment payload
                    await HandleTemplate(row);
                    break;

                case "deployment-playlist":
                    // Handle playlist deployment payload
                    await HandlePlaylist(row);
                    break;

                case "message":
                    HandleMsg(row);
                    break;

                case "message-remove":
                    HandleMsgRemove(row);
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
        // PAYLOAD HANDLERS
        // =========================

        private async Task HandleAsset(DeliveryEvent frame)
        {
            var apl = frame.Payload.Deserialize<AssetPayload>();

            if (apl == null) return;

            await AssetDownload(apl!);
        }
        private async Task HandleTemplate(DeliveryEvent frame)
        {
            var dtpl = frame.Payload.Deserialize<DeploymentTemplatePayload>();
            if (dtpl == null) return;

            var allAssetsValid = true;

            foreach (var variable in dtpl.Variables.Where(v =>
                (v.Kind == "image" ||
                 v.Kind == "audio" ||
                 v.Kind == "video")))
            {
                var asset = variable.Value.Deserialize<MediaAssetValue>();

                if (asset == null) continue;

                var assetCheck = await AssetCheck(asset);
                Logger.Instance.Log($"Asset: {asset.Filename} - Found: {assetCheck}");

                if (!assetCheck)
                {
                    allAssetsValid = false;
                    break;
                }

                //Logger.Instance.Log($"Asset: {asset.Filename} - Found: {assetCheck}");
            }

            if (!allAssetsValid)
            {
                Logger.Instance.Log(
                    $"Skipping template '{dtpl.TemplateName}' due to missing asset(s).");

                return;
            }

            if (playlistManager != null && playlistManager.IsRunning)
            {
                Logger.Instance.Log("Stopping existing playlist...");
                await playlistManager.StopAsync();
            }

            _ = Task.Run(() =>
            {
                var pkg = VentuzPackage.From(dtpl);
                SendTemplateToVentuz(pkg);
            });
        }
        private async Task HandlePlaylist(DeliveryEvent frame)
        {
            var dppl = frame.Payload.Deserialize<DeploymentPlaylistPayload>();
            if (dppl == null) return;

            var playlistId = dppl.PlaylistId;
            var playlistName = dppl.PlaylistName;

            foreach (var item in dppl.Items)
            {
                foreach (var variable in item.Variables.Where(v =>
                    (v.Kind == "image" ||
                     v.Kind == "audio" ||
                     v.Kind == "video")))
                {
                    var asset = variable.Value.Deserialize<MediaAssetValue>();

                    if (asset == null) continue;

                    var assetCheck = await AssetCheck(asset);
                    Logger.Instance.Log($"Asset: {asset.Filename} - Found: {assetCheck}");
                }
            }

            if (playlistManager != null && playlistManager.IsRunning)
            {
                Logger.Instance.Log("Stopping existing playlist...");
                await playlistManager.StopAsync();
            }

            playlistManager = new PlaylistManager(dppl, sender, Settings);
            playlistManager.Start();
        }
        private void HandleMsg(DeliveryEvent frame)
        {
            // Handle message payload
            var mpl = frame.Payload.Deserialize<MessagePayload>();

            // Null Check the payload
            if (mpl == null)
            {
                Logger.Instance.Log("Message Payload is Null");
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
        }
        private void HandleMsgRemove(DeliveryEvent frame)
        {
            // Handle message payload

            /*
            //var ampl = frame.Payload.Deserialize<MessagePayload>();
            // Filter messages for this station
            //var isMatch3 = string.Equals(ampl!.Station, STATION, StringComparison.OrdinalIgnoreCase);
            //if (!isMatch3) return;
             */

            var payloadRef = frame.PayloadRef;

            MessageItem? getResult = db.GetMessage(payloadRef!);

            if (getResult == null)
            {
                Logger.Instance.Log($"No entry found for {payloadRef}");
                return;
            }

            Logger.Instance.Log($"Deleting {getResult.Name}'s Message:{getResult.Message}...");
            msgStack.Remove(getResult.Name!, getResult.Message!);
            msgArch.Remove(getResult.Name!, getResult.Message!);
            db.Delete(payloadRef!);
        }

        // =========================
        // HELPERS
        // =========================

        private async Task AssetDownload(AssetPayload apl)
        {
            // Ignore null payloads
            if (apl == null) return;

            foreach (var variable in apl.Variables)
            {
                if (variable.Kind == "image" ||
                    variable.Kind == "audio" ||
                    variable.Kind == "video")
                {
                    var asset = variable.Value.Deserialize<MediaAssetValue>();

                    if (asset == null)
                        continue;

                    // Build asset URL and local file path
                    var assetUrl = asset.Url;
                    var assetName = Settings.Storage.StoragePath + "\\" + asset.Path;

                    await AssetDownload(assetUrl!, assetName);
                }
            }
        }
        private async Task AssetDownload(string url, string filename)
        {
            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(filename))
            {
                Logger.Instance.Log("Asset download failed: URL or filename was null/empty.");
                return;
            }

            try
            {
                using var http = new HttpClient();

                using var response = await http.GetAsync(url);
                response.EnsureSuccessStatusCode();

                await using var stream = await response.Content.ReadAsStreamAsync();
                await using var fileStream = new FileStream(filename, FileMode.Create);

                await stream.CopyToAsync(fileStream);

                Logger.Instance.Log($"Downloaded asset: {filename}");
            }
            catch (HttpRequestException ex)
            {
                Logger.Instance.Log($"HTTP error downloading '{url}': {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger.Instance.Log($"Access denied writing '{filename}': {ex.Message}");
            }
            catch (IOException ex)
            {
                Logger.Instance.Log($"File I/O error for '{filename}': {ex.Message}");
            }
            catch (Exception ex)
            {
                Logger.Instance.Log($"Unexpected error downloading '{url}' -> '{filename}': {ex}");
            }
        }

        private async Task<bool> AssetCheck(MediaAssetValue mav)
        {
            try
            {
                if (mav == null)
                {
                    Logger.Instance.Log("Asset check failed: MediaAssetValue is null.");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(mav.Url))
                {
                    Logger.Instance.Log($"Asset check failed: URL missing for '{mav.Path}'.");
                    return false;
                }

                var assetName = Path.Combine(
                    Settings.Storage.StoragePath,
                    mav.Path);

                if (File.Exists(assetName))
                {
                    Logger.Instance.Log($"{assetName} - Exists in storage");
                    return true;
                }

                Logger.Instance.Log($"{assetName} - Does not exist in storage");
                Logger.Instance.Log("Downloading asset...");

                await AssetDownload(mav.Url, assetName);

                // Verify download actually succeeded
                if (!File.Exists(assetName))
                {
                    Logger.Instance.Log($"Download completed but file not found: {assetName}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Instance.Log($"Asset check failed for '{mav?.Path}': {ex.Message}");
                return false;
            }
        }

        // JSON serializer settings 
        JsonSerializerOptions tempOpts = new() { WriteIndented = true };
        private void SendTemplateToVentuz(VentuzPackage pkg)
        {
            // Send deployment OSC package 
            var oscMsgD = new OscMessage("/DEPLOYMENT_TEMPLATE", JsonSerializer.Serialize(pkg, tempOpts));
            sender.Send(oscMsgD);
        }

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
            Logger.Instance.Log("--------------------");
            Logger.Instance.Log($"ID: {de.Id}");
            Logger.Instance.Log($"Station: {de.Station}");
            Logger.Instance.Log($"Status: {de.Status}");
            Logger.Instance.Log($"Kind: {de.Kind}");
            Logger.Instance.Log($"Payload: {de.Payload}");
            Logger.Instance.Log("--------------------");
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

            Logger.Instance.Log("Entry added to Database");
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
                    Logger.Instance.Log($"ACK FAILED {eventId}: {body}");
                else
                    Logger.Instance.Log($"ACK OK {eventId}");
            }
            catch (Exception ex)
            {
                // Log any unexpected errors during ACK
                Logger.Instance.Log($"ACK ERROR {eventId}: {ex.Message}");
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
                    $"{msg}\n");
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
                Logger.Instance.Log($"Timer Status: {sendTimer.Enabled}");
            }
        }
    }
}