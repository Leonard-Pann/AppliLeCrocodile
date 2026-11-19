using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using AppliLeCrocodile.Services;

namespace AppliLeCrocodile.Tests;

internal static class Ipv4TransportTests
{
    public static async Task<int> RunAsync()
    {
        using TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        Task<AddressFamily> server = RespondAsync(listener);
        using HttpClient client = ReviewHttpClientFactory.Create();
        string response = await client.GetStringAsync($"http://127.0.0.1:{port}/");
        AddressFamily family = await server;
        if (response != "ok" || family != AddressFamily.InterNetwork)
        {
            throw new InvalidOperationException("The review client did not connect through IPv4.");
        }

        try
        {
            await client.GetAsync("http://[::1]:1/");
            throw new InvalidOperationException("The review client accepted an IPv6-only address.");
        }
        catch (HttpRequestException)
        {
            return 2;
        }
    }

    private static async Task<AddressFamily> RespondAsync(TcpListener listener)
    {
        using TcpClient connection = await listener.AcceptTcpClientAsync();
        using NetworkStream stream = connection.GetStream();
        using StreamReader reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
        while (await reader.ReadLineAsync() is string line && line.Length > 0)
        {
        }
        byte[] response = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: close\r\n\r\nok");
        await stream.WriteAsync(response);
        return ((IPEndPoint)connection.Client.RemoteEndPoint!).AddressFamily;
    }
}
