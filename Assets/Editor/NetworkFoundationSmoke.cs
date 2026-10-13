using System;
using BrainrotPlanet.Networking;
using UnityEngine;

namespace BrainrotPlanet.Editor
{
    public static class NetworkFoundationSmoke
    {
        public static void Run()
        {
            // Reserved example domains: validation only, never contacted.
            ServerEndpointConfiguration.ValidateStoreBaseUrl("https://example.com/brainrot");
            string[] denied = { null, "", "api", "http://example.com", "https://localhost",
                "https://127.0.0.1", "https://[::1]", "https://192.168.1.10",
                "https://name:password@example.com", "https://example.com?token=x", "https://example.com#x" };
            foreach (string url in denied)
            {
                bool rejected = false;
                try { ServerEndpointConfiguration.ValidateStoreBaseUrl(url); }
                catch (InvalidOperationException) { rejected = true; }
                if (!rejected) throw new Exception("Unsafe store API URL accepted: " + url);
            }
            Debug.Log("NETWORK_ENDPOINT_SMOKE_PASS cases=12");
            ManagedHttpRequestQa.Run();
            Debug.Log("NETWORK_FOUNDATION_SMOKE_PASS endpointCases=12 httpCases=8 realTimeMatch=false");
        }
    }
}
