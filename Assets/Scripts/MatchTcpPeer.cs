using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace BrainrotPlanet
{
    // Loopback-only dedicated-server side. Internet clients go through the authenticated WSS gateway.
    // Exactly one reader and writer. Blocking socket IO never runs on Unity's thread.
    public sealed class MatchTcpPeer : IMatchPeer
    {
        public const int MaxPacket = 196608;
        private readonly TcpClient client;
        private readonly NetworkStream stream;
        private readonly ConcurrentQueue<byte[]> received = new ConcurrentQueue<byte[]>();
        private readonly ConcurrentQueue<byte[]> outgoing = new ConcurrentQueue<byte[]>();
        private readonly AutoResetEvent signal = new AutoResetEvent(false);
        private readonly object lifecycle = new object();
        private int incomingCount, outgoingCount;
        private volatile bool closed;
        public bool Closed => closed;
        public bool Connected => !closed;
        public string Error { get; private set; }

        public MatchTcpPeer(TcpClient socket)
        {
            client = socket; client.NoDelay = true;
            stream = client.GetStream(); stream.ReadTimeout = 15000; stream.WriteTimeout = 5000;
            new Thread(ReadLoop) { IsBackground = true, Name = "Brainrot LAN receive" }.Start();
            new Thread(WriteLoop) { IsBackground = true, Name = "Brainrot LAN send" }.Start();
        }
        public bool TryReceive(out byte[] bytes)
        {
            if (!received.TryDequeue(out bytes)) return false;
            Interlocked.Decrement(ref incomingCount); return true;
        }
        public void Send(byte[] bytes)
        {
            lock(lifecycle)
            {
            if (closed) return;
            if (bytes == null || bytes.Length == 0 || bytes.Length > MaxPacket)
                throw new ArgumentException("Invalid LAN packet size");
            if (Interlocked.Increment(ref outgoingCount) > 32) { Fail("Send queue overflow"); return; }
            outgoing.Enqueue(bytes); signal.Set();
            }
        }
        private void ReadExact(byte[] bytes)
        {
            int offset = 0;
            while (offset < bytes.Length)
            {
                int count = stream.Read(bytes, offset, bytes.Length - offset);
                if (count == 0) throw new EndOfStreamException();
                offset += count;
            }
        }
        public static int PacketLength(byte[] header)
        {
            int size = header[0] | header[1] << 8 | header[2] << 16 | header[3] << 24;
            if (size < 1 || size > MaxPacket) throw new InvalidDataException("LAN frame exceeds limit");
            return size;
        }
        private void ReadLoop()
        {
            try
            {
                var header = new byte[4];
                while (!closed)
                {
                    ReadExact(header);
                    var bytes = new byte[PacketLength(header)]; ReadExact(bytes);
                    if (Interlocked.Increment(ref incomingCount) > 64)
                        throw new InvalidDataException("Receive queue overflow");
                    received.Enqueue(bytes);
                }
            }
            catch (Exception e) { Fail(e.GetType().Name); }
        }
        private void WriteLoop()
        {
            try
            {
                while (!closed)
                {
                    if (!outgoing.TryDequeue(out byte[] bytes)) { signal.WaitOne(100); continue; }
                    Interlocked.Decrement(ref outgoingCount);
                    int n = bytes.Length;
                    var header = new[] { (byte)n, (byte)(n >> 8), (byte)(n >> 16), (byte)(n >> 24) };
                    stream.Write(header, 0, 4); stream.Write(bytes, 0, n);
                }
            }
            catch (Exception e) { Fail(e.GetType().Name); }
            finally { Dispose(); lock(lifecycle)signal.Dispose(); }
        }
        private void Fail(string error) { if (!closed) Error = error; Dispose(); }
        public void Dispose()
        {
            lock(lifecycle)
            {
            if(closed)return;
            closed = true;
            try { client.Close(); } catch (SocketException) { }
            signal.Set();
            }
        }
    }
}
