using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace BrainrotPlanet
{
    public interface IMatchPeer : IDisposable
    {
        bool Connected { get; }
        bool Closed { get; }
        bool TryReceive(out byte[] packet);
        void Send(byte[] packet);
    }

    // One send and one receive at a time. No Unity API access on worker tasks.
    public sealed class MatchWebSocketPeer : IMatchPeer
    {
        private readonly ClientWebSocket socket = new ClientWebSocket();
        private readonly CancellationTokenSource stop = new CancellationTokenSource();
        private readonly ConcurrentQueue<byte[]> incoming = new ConcurrentQueue<byte[]>();
        private readonly ConcurrentQueue<byte[]> outgoing = new ConcurrentQueue<byte[]>();
        private readonly SemaphoreSlim ready = new SemaphoreSlim(0);
        private volatile bool connected, closed;
        private int incomingCount, outgoingCount;
        public bool Connected => connected && !closed;
        public bool Closed => closed;
        public string Error { get; private set; }

        public MatchWebSocketPeer(string endpoint, string testKey)
        {
            var uri = new Uri(endpoint);
            if (uri.Scheme != "wss" || !string.IsNullOrEmpty(uri.UserInfo))
                throw new ArgumentException("A secure WSS endpoint is required");
            if(!string.IsNullOrEmpty(testKey))
                socket.Options.SetRequestHeader("X-Brainrot-Test-Key", testKey);
            socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(5);
            _ = Run(uri);
        }
        public bool TryReceive(out byte[] packet)
        {
            if (!incoming.TryDequeue(out packet)) return false;
            Interlocked.Decrement(ref incomingCount); return true;
        }
        public void Send(byte[] packet)
        {
            if (closed) return;
            if (packet == null || packet.Length == 0 || packet.Length > MatchTcpPeer.MaxPacket)
                throw new ArgumentException("Invalid match packet");
            if (Interlocked.Increment(ref outgoingCount) > 32) { Dispose(); return; }
            outgoing.Enqueue(packet); ready.Release();
        }
        private async Task Run(Uri uri)
        {
            Task sender = null, receiver = null;
            try
            {
                using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(stop.Token))
                {
                    timeout.CancelAfter(TimeSpan.FromSeconds(10));
                    await socket.ConnectAsync(uri, timeout.Token).ConfigureAwait(false);
                }
                connected = true;
                sender = SendLoop(); receiver = ReceiveLoop();
                await Task.WhenAny(sender, receiver).ConfigureAwait(false);
            }
            catch (Exception e) { Error = e.GetType().Name; }
            finally
            {
                Dispose();
                if (sender != null && receiver != null)
                {
                    try { await Task.WhenAll(sender, receiver).ConfigureAwait(false); }
                    catch (Exception e) { Error = e.GetType().Name; }
                }
                socket.Dispose();
            }
        }
        private async Task SendLoop()
        {
            while (!closed)
            {
                await ready.WaitAsync(stop.Token).ConfigureAwait(false);
                if (!outgoing.TryDequeue(out byte[] packet)) continue;
                Interlocked.Decrement(ref outgoingCount);
                await socket.SendAsync(new ArraySegment<byte>(packet), WebSocketMessageType.Binary,
                    true, stop.Token).ConfigureAwait(false);
            }
        }
        private async Task ReceiveLoop()
        {
            var chunk = new byte[8192];
            while (!closed)
            {
                using var data = new MemoryStream();
                WebSocketReceiveResult result;
                do
                {
                    result = await socket.ReceiveAsync(new ArraySegment<byte>(chunk), stop.Token).ConfigureAwait(false);
                    if (result.MessageType != WebSocketMessageType.Binary)
                        throw new InvalidDataException("Binary match frame expected");
                    if (data.Length + result.Count > MatchTcpPeer.MaxPacket)
                        throw new InvalidDataException("Match frame exceeds limit");
                    data.Write(chunk, 0, result.Count);
                } while (!result.EndOfMessage);
                if (data.Length == 0 || Interlocked.Increment(ref incomingCount) > 64)
                    throw new InvalidDataException("Match receive queue exceeded");
                incoming.Enqueue(data.ToArray());
            }
        }
        public void Dispose()
        {
            if (closed) return;
            closed = true; connected = false;
            stop.Cancel(); socket.Abort();
        }
    }
}
