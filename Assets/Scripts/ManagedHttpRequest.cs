// Adapted from TechFounder ed937bf69d0fab3a062e538b8dd49259530ac19c.
// Shared HTTP infrastructure only; no TechFounder endpoints, identities or save data.
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BrainrotPlanet.Networking
{
    public sealed class ManagedHttpRequest : IDisposable
    {
        public enum RequestResult
        {
            InProgress,
            Success,
            ConnectionError,
            ProtocolError,
            DataProcessingError
        }

        private sealed class ResponsePayload
        {
            public long StatusCode;
            public byte[] Data;
            public RequestResult Result;
            public string Error;
        }

        private static readonly HttpClient Client = CreateClient();

        private readonly string url;
        private readonly string method;
        private readonly Dictionary<string, string> headers =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private CancellationTokenSource cancellation;
        private Task<ResponsePayload> activeTask;
        private bool finalized;
        private volatile bool abortedByCaller;
        private RequestResult result = RequestResult.InProgress;
        private long responseCode;
        private byte[] data = Array.Empty<byte>();
        private string error = string.Empty;
        private volatile float downloadProgress;

        public int TimeoutSeconds { get; set; } = 5;
        public int MaxResponseBytes { get; set; } = 16 * 1024 * 1024;
        public byte[] Body { get; set; }
        public string ContentType { get; set; } = "application/octet-stream";

        public bool IsDone
        {
            get
            {
                FinalizeIfCompleted();
                return finalized;
            }
        }

        public RequestResult Result
        {
            get
            {
                FinalizeIfCompleted();
                return result;
            }
        }

        public long ResponseCode
        {
            get
            {
                FinalizeIfCompleted();
                return responseCode;
            }
        }

        public byte[] Data
        {
            get
            {
                FinalizeIfCompleted();
                return data;
            }
        }

        public string Text => Encoding.UTF8.GetString(Data ?? Array.Empty<byte>());

        public string Error
        {
            get
            {
                FinalizeIfCompleted();
                return error;
            }
        }

        public float DownloadProgress => Math.Max(0f, Math.Min(1f, downloadProgress));

        public ManagedHttpRequest(string url, string method)
        {
            this.url = url ?? string.Empty;
            this.method = string.IsNullOrWhiteSpace(method) ? "GET" : method;
        }

        public static ManagedHttpRequest Get(string url)
        {
            return new ManagedHttpRequest(url, "GET");
        }

        public void SetRequestHeader(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name))
                return;
            headers[name] = value ?? string.Empty;
        }

        public void Send()
        {
            if (activeTask != null)
                throw new InvalidOperationException("Request already started");

            abortedByCaller = false;
            cancellation = new CancellationTokenSource();
            cancellation.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, TimeoutSeconds)));
            activeTask = SendAsync(cancellation.Token);
        }

        public void Abort()
        {
            if (finalized)
                return;
            abortedByCaller = true;
            cancellation?.Cancel();
        }

        public void Dispose()
        {
            cancellation?.Cancel();
            cancellation?.Dispose();
            cancellation = null;
        }

        private async Task<ResponsePayload> SendAsync(CancellationToken token)
        {
            try
            {
                using var request = new HttpRequestMessage(new HttpMethod(method), url);
                if (Body != null)
                {
                    request.Content = new ByteArrayContent(Body);
                    request.Content.Headers.TryAddWithoutValidation(
                        "Content-Type",
                        string.IsNullOrWhiteSpace(ContentType)
                            ? "application/octet-stream"
                            : ContentType);
                }

                foreach (KeyValuePair<string, string> header in headers)
                {
                    if (string.Equals(header.Key, "Content-Type", StringComparison.OrdinalIgnoreCase) &&
                        request.Content != null)
                    {
                        request.Content.Headers.Remove(header.Key);
                        request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                    else
                    {
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }

                using HttpResponseMessage response = await Client.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    token).ConfigureAwait(false);

                byte[] responseData = await ReadResponseAsync(response, token).ConfigureAwait(false);
                long statusCode = (long)response.StatusCode;
                bool success = statusCode >= 200 && statusCode <= 299;
                return new ResponsePayload
                {
                    StatusCode = statusCode,
                    Data = responseData,
                    Result = success ? RequestResult.Success : RequestResult.ProtocolError,
                    Error = success ? string.Empty : "HTTP " + statusCode
                };
            }
            catch (OperationCanceledException)
            {
                return Failure(abortedByCaller ? "Request aborted" : "Request timed out");
            }
            catch (HttpRequestException ex)
            {
                return Failure(ex.Message);
            }
            catch (Exception ex)
            {
                return new ResponsePayload
                {
                    StatusCode = 0,
                    Data = Array.Empty<byte>(),
                    Result = RequestResult.DataProcessingError,
                    Error = ex.Message
                };
            }
        }

        private async Task<byte[]> ReadResponseAsync(
            HttpResponseMessage response,
            CancellationToken token)
        {
            if (response.Content == null)
            {
                downloadProgress = 1f;
                return Array.Empty<byte>();
            }

            long? expectedLength = response.Content.Headers.ContentLength;
            int maximumBytes = Math.Max(1, MaxResponseBytes);
            if (expectedLength.HasValue && expectedLength.Value > maximumBytes)
            {
                throw new InvalidDataException(
                    "Response exceeds " + maximumBytes + " bytes");
            }
            using Stream stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using var buffer = new MemoryStream();
            var chunk = new byte[81920];
            int read;
            while ((read = await stream.ReadAsync(chunk, 0, chunk.Length, token).ConfigureAwait(false)) > 0)
            {
                if (buffer.Length + read > maximumBytes)
                {
                    throw new InvalidDataException(
                        "Response exceeds " + maximumBytes + " bytes");
                }
                buffer.Write(chunk, 0, read);
                if (expectedLength.HasValue && expectedLength.Value > 0)
                    downloadProgress = (float)buffer.Length / expectedLength.Value;
            }

            downloadProgress = 1f;
            return buffer.ToArray();
        }

        private static ResponsePayload Failure(string message)
        {
            return new ResponsePayload
            {
                StatusCode = 0,
                Data = Array.Empty<byte>(),
                Result = RequestResult.ConnectionError,
                Error = message ?? string.Empty
            };
        }

        private void FinalizeIfCompleted()
        {
            if (finalized || activeTask == null || !activeTask.IsCompleted)
                return;

            ResponsePayload payload;
            try
            {
                payload = activeTask.GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                payload = Failure(ex.Message);
            }

            responseCode = payload.StatusCode;
            data = payload.Data ?? Array.Empty<byte>();
            result = payload.Result;
            error = payload.Error ?? string.Empty;
            downloadProgress = 1f;
            finalized = true;
        }

        private static HttpClient CreateClient()
        {
            var handler = new HttpClientHandler
            {
                // API and content endpoints are exact release inputs. Following
                // redirects could silently cross authority or downgrade HTTPS.
                AllowAutoRedirect = false
            };
            return new HttpClient(handler, true)
            {
                Timeout = Timeout.InfiniteTimeSpan
            };
        }
    }
}
