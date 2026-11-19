using System.Net;
using System.Net.Http;
using System.Net.Sockets;

namespace AppliLeCrocodile.Services;

public static class ReviewHttpClientFactory
{
    public static HttpClient Create()
    {
        if (OperatingSystem.IsBrowser())
        {
            return new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(15) };
        }

        SocketsHttpHandler handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            PooledConnectionLifetime = TimeSpan.FromMinutes(10),
            ConnectCallback = ConnectIpv4Async
        };
        return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
    }

    private static async ValueTask<Stream> ConnectIpv4Async(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        IPAddress[] addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, AddressFamily.InterNetwork, cancellationToken)
            .ConfigureAwait(false);
        addresses = addresses.Where(address => address.AddressFamily == AddressFamily.InterNetwork).ToArray();
        if (addresses.Length == 0)
        {
            throw new SocketException((int)SocketError.HostNotFound);
        }

        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(addresses, context.DnsEndPoint.Port, cancellationToken).ConfigureAwait(false);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }
}
