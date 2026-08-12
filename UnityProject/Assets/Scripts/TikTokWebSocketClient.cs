using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace TikTokLiveGame
{
    public sealed class TikTokWebSocketClient : MonoBehaviour
    {
        [SerializeField] private string serverUrl = "ws://127.0.0.1:3000";
        [SerializeField] private int reconnectDelayMs = 2000;
        private readonly ConcurrentQueue<string> inbox = new();
        private ClientWebSocket socket;
        private CancellationTokenSource lifetime;
        private Task supervisor;
        private long lastPongTicks;
        private static TikTokWebSocketClient instance;

        public event Action<TikTokEvent> EventReceived;
        public bool IsConnected => socket != null && socket.State == WebSocketState.Open;
        public string ServerUrl => serverUrl;

        private void OnEnable()
        {
            if (instance != null && instance != this)
            {
                Destroy(this);
                return;
            }
            instance = this;
            lifetime = new CancellationTokenSource();
            supervisor = RunConnectionSupervisorAsync(lifetime.Token);
        }

        private void Update()
        {
            while (inbox.TryDequeue(out string json))
            {
                try
                {
                    TikTokEvent liveEvent = JsonUtility.FromJson<TikTokEvent>(json);
                    if (liveEvent != null && !string.IsNullOrWhiteSpace(liveEvent.type))
                        EventReceived?.Invoke(liveEvent);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"TikTok event JSON was ignored: {exception.Message}");
                }
            }
        }

        private async Task RunConnectionSupervisorAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                ClientWebSocket activeSocket = new();
                socket = activeSocket;
                try
                {
                    await activeSocket.ConnectAsync(new Uri(serverUrl), token);
                    Interlocked.Exchange(ref lastPongTicks, DateTime.UtcNow.Ticks);
                    Debug.Log($"Connected to TikTok Node bridge at {serverUrl}");
                    string registration = JsonUtility.ToJson(new ClientMessage
                    {
                        type = "register",
                        role = "overlay"
                    });
                    await SendRawAsync(activeSocket, registration, token);

                    using CancellationTokenSource connectionLifetime = CancellationTokenSource.CreateLinkedTokenSource(token);
                    Task receiver = ReceiveLoopAsync(activeSocket, connectionLifetime.Token);
                    Task heartbeat = HeartbeatLoopAsync(activeSocket, connectionLifetime.Token);
                    await Task.WhenAny(receiver, heartbeat);
                    connectionLifetime.Cancel();
                    try { await receiver; } catch when (!token.IsCancellationRequested) { }
                    try { await heartbeat; } catch when (!token.IsCancellationRequested) { }
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    if (!token.IsCancellationRequested)
                        Debug.LogWarning($"TikTok bridge unavailable: {exception.Message}");
                }
                finally
                {
                    if (ReferenceEquals(socket, activeSocket)) socket = null;
                    try { activeSocket.Abort(); } catch { }
                    activeSocket.Dispose();
                }

                if (token.IsCancellationRequested) break;
                try { await Task.Delay(reconnectDelayMs, token); }
                catch (OperationCanceledException) { break; }
            }
        }

        private async Task ReceiveLoopAsync(ClientWebSocket activeSocket, CancellationToken token)
        {
            byte[] buffer = new byte[16 * 1024];
            while (!token.IsCancellationRequested && activeSocket.State == WebSocketState.Open)
            {
                using MemoryStream message = new();
                WebSocketReceiveResult result;
                do
                {
                    result = await activeSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                    if (result.MessageType == WebSocketMessageType.Close) return;
                    message.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);

                string json = Encoding.UTF8.GetString(message.ToArray());
                if (json.Contains("\"type\":\"pong\"", StringComparison.Ordinal))
                    Interlocked.Exchange(ref lastPongTicks, DateTime.UtcNow.Ticks);
                else
                    inbox.Enqueue(json);
            }
        }

        private async Task HeartbeatLoopAsync(ClientWebSocket activeSocket, CancellationToken token)
        {
            while (!token.IsCancellationRequested && activeSocket.State == WebSocketState.Open)
            {
                await Task.Delay(2000, token);
                await SendRawAsync(activeSocket, "{\"type\":\"ping\"}", token);
                long elapsedTicks = DateTime.UtcNow.Ticks - Interlocked.Read(ref lastPongTicks);
                if (elapsedTicks > TimeSpan.FromSeconds(7).Ticks)
                    throw new TimeoutException("Node heartbeat timed out");
            }
        }

        private static async Task SendRawAsync(ClientWebSocket activeSocket, string json, CancellationToken token)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            await activeSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, token);
        }

        private async void Send(ClientMessage message)
        {
            ClientWebSocket activeSocket = socket;
            if (activeSocket == null || activeSocket.State != WebSocketState.Open) return;
            try
            {
                await SendRawAsync(activeSocket, JsonUtility.ToJson(message), lifetime.Token);
            }
            catch (Exception exception)
            {
                if (lifetime != null && !lifetime.IsCancellationRequested)
                    Debug.LogWarning($"Could not send to TikTok bridge: {exception.Message}");
                try { activeSocket.Abort(); } catch { }
            }
        }

        public void ConnectTikTok(string username) => Send(new ClientMessage
        {
            type = "set_username",
            username = username?.Trim().TrimStart('@')
        });

        public void DisconnectTikTok() => Send(new ClientMessage { type = "disconnect_tiktok" });
        public void StartDemo(int count) => Send(new ClientMessage { type = "demo_start", count = count });
        public void StopDemo() => Send(new ClientMessage { type = "demo_stop" });
        public void ResetGame() => Send(new ClientMessage { type = "reset_game" });
        public void DemoGift(int diamonds, int userIndex = 1) => Send(new ClientMessage
        {
            type = "demo_event",
            action = "gift",
            value = diamonds,
            userIndex = userIndex
        });
        private async void OnDisable()
        {
            lifetime?.Cancel();
            try { socket?.Abort(); } catch { }
            if (supervisor != null)
            {
                try { await supervisor; } catch { }
            }
            lifetime?.Dispose();
            lifetime = null;
            socket = null;
            if (instance == this) instance = null;
        }
    }
}
