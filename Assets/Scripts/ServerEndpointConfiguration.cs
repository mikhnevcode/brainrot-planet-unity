using System;

namespace BrainrotPlanet.Networking
{
    // Adapted from TechFounder ed937bf69d0fab3a062e538b8dd49259530ac19c.
    // Intentionally no default URL: Brainrot must never use TechFounder's backend.
    // Use for lobby/account HTTPS APIs, not real-time match packets.
    public static class ServerEndpointConfiguration
    {
        public static void ValidateStoreBaseUrl(string baseUrl)
        {
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri uri) ||
                !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Store builds require an absolute HTTPS API endpoint.");
            if (uri.IsLoopback || Uri.CheckHostName(uri.Host) != UriHostNameType.Dns)
                throw new InvalidOperationException("Store builds require a non-loopback DNS API endpoint.");
            if (!string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) ||
                !string.IsNullOrEmpty(uri.Fragment))
                throw new InvalidOperationException("API endpoint must not contain credentials, query or fragment.");
        }
    }
}
