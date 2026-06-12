using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Vizcon.OSC;

namespace ER_StationAgent
{
    public sealed class PlaylistManager
    {
        private readonly DeploymentPlaylistPayload _playlist;
        private readonly UDPSender _sender;
        private readonly AppSettings _settings;

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true
        };

        private Task? _playbackTask;
        private CancellationTokenSource? _cts;

        public bool IsRunning => _playbackTask is { IsCompleted: false };

        public PlaylistManager(DeploymentPlaylistPayload playlist, UDPSender sender, AppSettings settings)
        {
            _playlist = playlist;
            _sender = sender;
            _settings = settings;
        }

        public void Start()
        {
            if (IsRunning)
            {
                return;
            }

            _cts = new CancellationTokenSource();
            _playbackTask = Task.Run(() => PlaybackLoopAsync(_cts.Token));
        }

        public async Task StopAsync()
        {
            if (!IsRunning || _cts is null || _playbackTask is null)
            {
                return;
            }

            _cts.Cancel();

            try
            {
                await _playbackTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }

            _cts.Dispose();
            _cts = null;
            _playbackTask = null;
        }

        // JSON serializer settings 
        JsonSerializerOptions tempOpts = new() { WriteIndented = true };
        private void SendTemplateToVentuz(VentuzPackage pkg)
        {
            // Send deployment OSC package 
            var oscMsgD = new OscMessage("/DEPLOYMENT_TEMPLATE", JsonSerializer.Serialize(pkg, tempOpts));
            _sender.Send(oscMsgD);
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
                    _settings.Storage.StoragePath,
                    mav.Path);

                if (File.Exists(assetName))
                {
                    //Logger.Instance.Log($"{assetName} - Exists in storage");
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

        private async Task PlaybackLoopAsync(CancellationToken cancellationToken)
        {
            if (_playlist.Items.Count == 0)
            {
                return;
            }

            var orderedItems = _playlist.Items
                .OrderBy(i => i.Position)
                .ToList();

            Logger.Instance.Log($"Starting Playlist {_playlist.PlaylistName}");

            while (!cancellationToken.IsCancellationRequested)
            {
                foreach (var item in orderedItems)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    bool allAssetsValid = true;

                    foreach (var variable in item.Variables.Where(v =>
                                    (v.Kind == "image" ||
                                     v.Kind == "audio" ||
                                     v.Kind == "video")))
                    {
                        var asset = variable.Value.Deserialize<MediaAssetValue>();

                        if (asset == null) continue;

                        var assetCheck = await AssetCheck(asset);

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
                            $"Skipping template '{item.TemplateName}' due to missing asset(s).");

                        continue;
                    }

                    // Send payload
                    var pkg = VentuzPackage.From(item);
                    SendTemplateToVentuz(pkg);

                    await Task.Delay(
                        TimeSpan.FromSeconds(item.DurationSeconds),
                        cancellationToken);
                }

                //Logger.Instance.Log($"Restarting Playlist {_playlist.PlaylistName}");
            }
        }
    }
}
