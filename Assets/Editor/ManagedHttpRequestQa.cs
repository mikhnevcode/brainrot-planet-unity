// Adapted from TechFounder ed937bf69d0fab3a062e538b8dd49259530ac19c.
// Shared HTTP infrastructure only; no TechFounder endpoints, identities or save data.
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BrainrotPlanet.Networking;
using UnityEngine;

namespace BrainrotPlanet.Editor
{
    public static class ManagedHttpRequestQa
    {
        private static int passed;

        public static void Run()
        {
            passed = 0;
            RunTest(TestSuccessResponse);
            RunTest(TestProtocolErrorPreservesBody);
            RunTest(TestEmptyReplyIsSilentConnectionError);
            RunTest(TestTimeoutIsSilentConnectionError);
            RunTest(TestAbortIsSilentConnectionError);
            RunTest(TestDeclaredOversizeResponseIsRejected);
            RunTest(TestUndeclaredOversizeResponseIsRejected);
            RunTest(TestRedirectIsNotFollowed);
            Debug.Log("MANAGED_HTTP_QA_PASS tests=" + passed);
        }

        private static void RunTest(Action test)
        {
            test();
            passed++;
        }

        private static void TestSuccessResponse()
        {
            using var server = new SingleResponseServer(200, "{\"revision\":54}");
            using var request = ManagedHttpRequest.Get(server.Url);
            request.TimeoutSeconds = 2;
            request.Send();
            WaitFor(request);

            AssertEqual(ManagedHttpRequest.RequestResult.Success, request.Result, "success result");
            AssertEqual(200L, request.ResponseCode, "success status");
            AssertEqual("{\"revision\":54}", request.Text, "success body");
        }

        private static void TestProtocolErrorPreservesBody()
        {
            using var server = new SingleResponseServer(503, "{\"error\":\"offline\"}");
            using var request = ManagedHttpRequest.Get(server.Url);
            request.TimeoutSeconds = 2;
            request.Send();
            WaitFor(request);

            AssertEqual(
                ManagedHttpRequest.RequestResult.ProtocolError,
                request.Result,
                "protocol result");
            AssertEqual(503L, request.ResponseCode, "protocol status");
            AssertEqual("{\"error\":\"offline\"}", request.Text, "protocol body");
        }

        private static void TestEmptyReplyIsSilentConnectionError()
        {
            int unityErrors = 0;
            Application.LogCallback callback = (_, _, type) =>
            {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                    unityErrors++;
            };

            Application.logMessageReceived += callback;
            try
            {
                using var server = new SingleResponseServer(0, string.Empty, true);
                using var request = ManagedHttpRequest.Get(server.Url);
                request.TimeoutSeconds = 2;
                request.Send();
                WaitFor(request);

                AssertEqual(
                    ManagedHttpRequest.RequestResult.ConnectionError,
                    request.Result,
                    "empty reply result");
                AssertEqual(0L, request.ResponseCode, "empty reply status");
                AssertEqual(0, unityErrors, "empty reply Unity errors");
            }
            finally
            {
                Application.logMessageReceived -= callback;
            }
        }

        private static void TestTimeoutIsSilentConnectionError()
        {
            int unityErrors = 0;
            Application.LogCallback callback = (_, _, type) =>
            {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                    unityErrors++;
            };

            Application.logMessageReceived += callback;
            try
            {
                using var server = new SingleResponseServer(
                    200,
                    "{\"late\":true}",
                    false,
                    1500);
                using var request = ManagedHttpRequest.Get(server.Url);
                request.TimeoutSeconds = 1;
                request.Send();
                WaitFor(request);

                AssertEqual(
                    ManagedHttpRequest.RequestResult.ConnectionError,
                    request.Result,
                    "timeout result");
                AssertEqual(0L, request.ResponseCode, "timeout status");
                AssertEqual("Request timed out", request.Error, "timeout error");
                AssertEqual(0, unityErrors, "timeout Unity errors");
            }
            finally
            {
                Application.logMessageReceived -= callback;
            }
        }

        private static void TestAbortIsSilentConnectionError()
        {
            using var server = new SingleResponseServer(
                200,
                "{\"late\":true}",
                false,
                1500);
            using var request = ManagedHttpRequest.Get(server.Url);
            request.TimeoutSeconds = 5;
            request.Send();
            Thread.Sleep(50);
            request.Abort();
            WaitFor(request);

            AssertEqual(
                ManagedHttpRequest.RequestResult.ConnectionError,
                request.Result,
                "abort result");
            AssertEqual(0L, request.ResponseCode, "abort status");
            AssertEqual("Request aborted", request.Error, "abort error");
        }

        private static void TestDeclaredOversizeResponseIsRejected()
        {
            TestOversizeResponse(true, "declared oversize");
        }

        private static void TestUndeclaredOversizeResponseIsRejected()
        {
            TestOversizeResponse(false, "streamed oversize");
        }

        private static void TestOversizeResponse(bool includeContentLength, string label)
        {
            using var server = new SingleResponseServer(
                200,
                new string('x', 64),
                false,
                0,
                includeContentLength);
            using var request = ManagedHttpRequest.Get(server.Url);
            request.TimeoutSeconds = 2;
            request.MaxResponseBytes = 32;
            request.Send();
            WaitFor(request);

            AssertEqual(
                ManagedHttpRequest.RequestResult.DataProcessingError,
                request.Result,
                label + " result");
            AssertEqual(0L, request.ResponseCode, label + " status");
            if (request.Error.IndexOf("exceeds 32 bytes", StringComparison.Ordinal) < 0)
            {
                throw new InvalidOperationException(
                    label + ": unexpected error " + request.Error);
            }
        }

        private static void TestRedirectIsNotFollowed()
        {
            using var server = new SingleResponseServer(
                302,
                "redirect denied",
                false,
                0,
                true,
                "Location: http://127.0.0.1:9/downgrade\r\n");
            using var request = ManagedHttpRequest.Get(server.Url);
            request.TimeoutSeconds = 2;
            request.Send();
            WaitFor(request);

            AssertEqual(
                ManagedHttpRequest.RequestResult.ProtocolError,
                request.Result,
                "redirect result");
            AssertEqual(302L, request.ResponseCode, "redirect status");
            AssertEqual("redirect denied", request.Text, "redirect body");
        }

        private static void WaitFor(ManagedHttpRequest request)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            while (!request.IsDone && stopwatch.Elapsed < TimeSpan.FromSeconds(5))
                Thread.Sleep(10);
            if (!request.IsDone)
                throw new InvalidOperationException("managed request did not complete");
        }

        private static void AssertEqual<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(
                    label + ": expected " + expected + ", actual " + actual);
        }

        private sealed class SingleResponseServer : IDisposable
        {
            private readonly TcpListener listener;
            private readonly Task worker;
            private readonly int statusCode;
            private readonly string body;
            private readonly bool closeWithoutResponse;
            private readonly int responseDelayMilliseconds;
            private readonly bool includeContentLength;
            private readonly string extraHeaders;

            public string Url { get; }

            public SingleResponseServer(
                int statusCode,
                string body,
                bool closeWithoutResponse = false,
                int responseDelayMilliseconds = 0,
                bool includeContentLength = true,
                string extraHeaders = "")
            {
                this.statusCode = statusCode;
                this.body = body ?? string.Empty;
                this.closeWithoutResponse = closeWithoutResponse;
                this.responseDelayMilliseconds = Math.Max(0, responseDelayMilliseconds);
                this.includeContentLength = includeContentLength;
                this.extraHeaders = extraHeaders ?? string.Empty;
                listener = new TcpListener(IPAddress.Loopback, 0);
                listener.Start();
                int port = ((IPEndPoint)listener.LocalEndpoint).Port;
                Url = "http://127.0.0.1:" + port + "/probe";
                worker = Task.Run(ServeOnce);
            }

            public void Dispose()
            {
                listener.Stop();
                try
                {
                    worker.Wait(TimeSpan.FromSeconds(1));
                }
                catch
                {
                }
            }

            private async Task ServeOnce()
            {
                using TcpClient client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                using NetworkStream stream = client.GetStream();
                var requestBuffer = new byte[4096];
                await stream.ReadAsync(requestBuffer, 0, requestBuffer.Length).ConfigureAwait(false);
                if (closeWithoutResponse)
                    return;
                if (responseDelayMilliseconds > 0)
                    await Task.Delay(responseDelayMilliseconds).ConfigureAwait(false);

                byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
                string reason = statusCode == 200 ? "OK" : "Service Unavailable";
                string lengthHeader = includeContentLength
                    ? "Content-Length: " + bodyBytes.Length + "\r\n"
                    : string.Empty;
                byte[] headerBytes = Encoding.ASCII.GetBytes(
                    "HTTP/1.1 " + statusCode + " " + reason + "\r\n" +
                    "Content-Type: application/json\r\n" +
                    lengthHeader +
                    extraHeaders +
                    "Connection: close\r\n\r\n");
                await stream.WriteAsync(headerBytes, 0, headerBytes.Length).ConfigureAwait(false);
                await stream.WriteAsync(bodyBytes, 0, bodyBytes.Length).ConfigureAwait(false);
            }
        }
    }
}
