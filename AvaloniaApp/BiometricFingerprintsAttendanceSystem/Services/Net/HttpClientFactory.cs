using System.Net;
using System.Net.Sockets;
using System.Linq;

namespace BiometricFingerprintsAttendanceSystem.Services.Net;

internal static class HttpClientFactory
{
    public static HttpClient CreateWithBaseAddress(
        Uri baseAddress,
        TimeSpan? timeout = null,
        string? apiKeyHeader = null,
        string? apiKey = null)
    {
        var handler = CreateHandlerPreferIPv4();
        var client = new HttpClient(handler)
        {
            BaseAddress = baseAddress
        };

        if (timeout.HasValue)
        {
            client.Timeout = timeout.Value;
        }

        if (!string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(apiKeyHeader))
        {
            client.DefaultRequestHeaders.Remove(apiKeyHeader);
            client.DefaultRequestHeaders.Add(apiKeyHeader, apiKey);
        }

        return client;
    }

    private static SocketsHttpHandler CreateHandlerPreferIPv4()
    {
        return new SocketsHttpHandler
        {
            ConnectCallback = async (context, cancellationToken) =>
            {
                var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, cancellationToken);
                var ordered = addresses
                    .OrderBy(address => address.AddressFamily == AddressFamily.InterNetwork ? 0 : 1)
                    .ToArray();

                Exception? lastError = null;
                foreach (var address in ordered)
                {
                    var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    try
                    {
                        await socket.ConnectAsync(address, context.DnsEndPoint.Port, cancellationToken);
                        return new NetworkStream(socket, ownsSocket: true);
                    }
                    catch (Exception ex)
                    {
                        lastError = ex;
                        socket.Dispose();
                    }
                }

                throw lastError ?? new SocketException((int)SocketError.HostNotFound);
            }
        };
    }
}
