using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace DewDrop.Framework
{
    /// <summary>
    /// Nexus Mods single sign-on (protocol v2), the same flow Stardrop uses: DewDrop opens a websocket to Nexus,
    /// sends the player to the website to approve it, and Nexus sends the API key back over the socket. One
    /// instance runs one sign-in; it never touches game state, so it can run entirely in the background.
    /// </summary>
    internal class NexusSso : IDisposable
    {
        private const int MaxReconnects = 3;
        private const int MaxMessageBytes = 64 * 1024;

        private static readonly Uri _endpoint = new("wss://sso.nexusmods.com");

        /// <summary>Nexus closes quiet sockets; like Stardrop, send something every 30 seconds.</summary>
        private static readonly TimeSpan _pingInterval = TimeSpan.FromSeconds(30);

        /// <summary>How long the player gets to approve DewDrop on the website.</summary>
        private static readonly TimeSpan _timeout = TimeSpan.FromMinutes(10);

        private static readonly TimeSpan _reconnectDelay = TimeSpan.FromSeconds(2);

        private readonly CancellationTokenSource _cancel = new();
        private readonly string _id = Guid.NewGuid().ToString();
        private string? _connectionToken;
        private bool _browserOpened;
        private int _disposed;

        /// <summary>
        /// Runs the whole sign-in on the calling task. Returns the API key, or null if it was cancelled; throws
        /// if Nexus can't be reached, turns the login down or nobody approves it in time.
        /// </summary>
        public async Task<string?> GetApiKeyAsync(string slug, CancellationToken token)
        {
            CancellationTokenSource linked;
            try
            {
                linked = CancellationTokenSource.CreateLinkedTokenSource(token, _cancel.Token);
            }
            catch (ObjectDisposedException)
            {
                return null;
            }

            using (linked)
            {
                linked.CancelAfter(_timeout);
                try
                {
                    return await RunAsync(slug, linked.Token);
                }
                catch (Exception) when (token.IsCancellationRequested || _cancel.IsCancellationRequested)
                {
                    // Cancelling aborts the socket, which surfaces as either a cancellation or a socket error
                    return null;
                }
                catch (Exception ex) when (linked.IsCancellationRequested)
                {
                    throw new TimeoutException("Nobody approved DewDrop on the website in time.", ex);
                }
            }
        }

        /// <summary>Stops the sign-in; GetApiKeyAsync then returns null. Returns at once, from any thread.</summary>
        public void Cancel()
        {
            // Cancelling runs the socket's abort callbacks inline, which shouldn't happen on the game thread
            Task.Run(() =>
            {
                try
                {
                    _cancel.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // Already finished and cleaned up
                }
            });
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
            {
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    _cancel.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // Nothing left to stop
                }
                _cancel.Dispose();
            });
        }

        private async Task<string> RunAsync(string slug, CancellationToken token)
        {
            for (var attempt = 0; ; attempt++)
            {
                string? key;
                try
                {
                    key = await ConnectAsync(slug, token);
                }
                catch (WebSocketException ex) when (_connectionToken is not null && attempt < MaxReconnects && !token.IsCancellationRequested)
                {
                    // Dropped mid-session; the connection token lets the next socket pick it up again
                    ModEntry.Log.Trace($"Nexus SSO connection dropped ({ex.Message}); reconnecting.");
                    key = null;
                }

                if (key is not null)
                {
                    return key;
                }

                if (_connectionToken is null || attempt >= MaxReconnects)
                {
                    throw new IOException("Nexus Mods closed the login connection. Try again.");
                }

                await Task.Delay(_reconnectDelay, token);
            }
        }

        /// <summary>One socket's worth of the flow: returns the key, or null if Nexus closed the socket first.</summary>
        private async Task<string?> ConnectAsync(string slug, CancellationToken token)
        {
            using var socket = new ClientWebSocket();
            await socket.ConnectAsync(_endpoint, token);
            await SendAsync(socket, JsonSerializer.Serialize(new { id = _id, token = _connectionToken, protocol = 2 }), token);

            if (!_browserOpened)
            {
                // The website finds this sign-in by its id, so it opens once the id is sent; a reconnect resumes
                // the same id and needs no new tab
                _browserOpened = true;
                var url = $"https://www.nexusmods.com/sso?id={_id}&application={Uri.EscapeDataString(slug)}";
                MainThread.Post(() =>
                {
                    if (!token.IsCancellationRequested)
                    {
                        DownloadManager.OpenUrl(url);
                    }
                });
            }

            using var keepAlive = CancellationTokenSource.CreateLinkedTokenSource(token);
            var pings = KeepAliveAsync(socket, keepAlive.Token);
            string? key = null;
            try
            {
                while (key is null && socket.State == WebSocketState.Open)
                {
                    var message = await ReceiveAsync(socket, token);
                    if (message is null)
                    {
                        break;
                    }

                    key = ReadMessage(message);
                }
            }
            finally
            {
                // A socket only allows one send at a time, so no ping may be in flight while it closes
                keepAlive.Cancel();
                await pings;
            }

            if (key is not null)
            {
                await CloseQuietlyAsync(socket);
            }
            return key;
        }

        /// <summary>Remembers the connection token, and returns the API key once a message carries one.</summary>
        private string? ReadMessage(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(text);
            }
            catch (JsonException)
            {
                ModEntry.Log.Trace($"Ignored a Nexus SSO message that isn't JSON ({text.Length} characters).");
                return null;
            }

            using (document)
            {
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                {
                    return null;
                }

                if (root.TryGetProperty("success", out var success) && success.ValueKind == JsonValueKind.False)
                {
                    var error = root.TryGetProperty("error", out var e) && e.ValueKind == JsonValueKind.String ? e.GetString() : null;
                    throw new InvalidOperationException(string.IsNullOrWhiteSpace(error)
                        ? "Nexus Mods turned the login down."
                        : $"Nexus Mods turned the login down: {error}");
                }

                if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
                {
                    return null;
                }

                // The first message only carries the connection token; the key follows once the player approves
                if (data.TryGetProperty("connection_token", out var connection) && connection.ValueKind == JsonValueKind.String)
                {
                    _connectionToken = connection.GetString();
                }

                return data.TryGetProperty("api_key", out var key) && key.ValueKind == JsonValueKind.String && key.GetString() is { Length: > 0 } apiKey
                    ? apiKey
                    : null;
            }
        }

        /// <summary>One whole message, or null once Nexus closes the socket.</summary>
        private static async Task<string?> ReceiveAsync(ClientWebSocket socket, CancellationToken token)
        {
            var buffer = new byte[4096];
            using var message = new MemoryStream();
            while (true)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    return null;
                }

                message.Write(buffer, 0, result.Count);
                if (message.Length > MaxMessageBytes)
                {
                    throw new InvalidDataException("Nexus Mods sent an unexpectedly large login message.");
                }

                if (result.EndOfMessage)
                {
                    return Encoding.UTF8.GetString(message.GetBuffer(), 0, (int)message.Length);
                }
            }
        }

        private static Task SendAsync(ClientWebSocket socket, string text, CancellationToken token)
        {
            return socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(text)), WebSocketMessageType.Text, true, token);
        }

        /// <summary>Stardrop's keep-alive: an empty text message every 30 seconds until stopped.</summary>
        private static async Task KeepAliveAsync(ClientWebSocket socket, CancellationToken token)
        {
            try
            {
                while (socket.State == WebSocketState.Open)
                {
                    await Task.Delay(_pingInterval, token);

                    // Not cancellable mid-send: cancelling a send aborts the whole socket
                    await socket.SendAsync(ArraySegment<byte>.Empty, WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
            catch (Exception)
            {
                // Stopped, or the socket is gone; the receive loop reports real problems
            }
        }

        private static async Task CloseQuietlyAsync(ClientWebSocket socket)
        {
            try
            {
                // Only send the close; waiting for Nexus to answer it would hold up the key
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Got the key", timeout.Token);
            }
            catch (Exception)
            {
                // The key is already here; a messy close doesn't matter
            }
        }
    }
}
