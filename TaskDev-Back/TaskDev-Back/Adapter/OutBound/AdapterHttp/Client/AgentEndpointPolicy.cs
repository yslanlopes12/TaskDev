using System.Net;
using System.Net.Sockets;

namespace TaskDev.Api.Adapter.OutBound.AdapterHttp.Client;

/// <summary>Checks the destination again when the socket is opened and pins that connection to a validated IP.</summary>
public sealed class AgentEndpointPolicy(IConfiguration configuration)
{
    private readonly bool _allowLocalOllama = configuration.GetValue<bool>("Agent:AllowLocalOllama");

    public Uri ValidateEndpoint(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Fragment))
            throw new ArgumentException("Endpoint inválido: use uma URL HTTP(S) sem credenciais ou fragmento.");

        var local = IsLocalOllama(uri);
        if (uri.Scheme != Uri.UriSchemeHttps && !local)
            throw new ArgumentException("Endpoints externos de IA devem usar HTTPS.");

        if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) && !local)
            throw new ArgumentException("O acesso local de IA exige Agent__AllowLocalOllama=true e a porta 11434.");

        return uri;
    }

    private bool IsLocalOllama(Uri uri) => _allowLocalOllama &&
        uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) && uri.Port == 11434;

    public void ValidateAddress(Uri uri, IPAddress address)
    {
        if (IsLocalOllama(uri) && IPAddress.IsLoopback(address)) return;
        if (!IsPublicAddress(address))
            throw new ArgumentException("Endereços privados, reservados e de metadados não são permitidos para IA.");
    }

    public static bool IsPublicAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        var bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            return !(bytes[0] is 0 or 10 or 127 || bytes[0] >= 224 ||
                (bytes[0] == 100 && bytes[1] is >= 64 and <= 127) ||
                (bytes[0] == 169 && bytes[1] == 254) ||
                (bytes[0] == 172 && bytes[1] is >= 16 and <= 31) ||
                (bytes[0] == 192 && bytes[1] == 168) ||
                (bytes[0] == 192 && bytes[1] == 0 && bytes[2] is 0 or 2) ||
                (bytes[0] == 198 && bytes[1] is 18 or 19) ||
                (bytes[0] == 198 && bytes[1] == 51 && bytes[2] == 100) ||
                (bytes[0] == 203 && bytes[1] == 0 && bytes[2] == 113));
        }

        // Only globally routable IPv6 unicast; exclude documentation addresses.
        return address.AddressFamily == AddressFamily.InterNetworkV6 && (bytes[0] & 0xe0) == 0x20 &&
            !(bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] == 0x0d && bytes[3] == 0xb8);
    }

    public async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        var uri = ValidateEndpoint(context.InitialRequestMessage.RequestUri?.AbsoluteUri ?? "");
        var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, cancellationToken);
        if (addresses.Length == 0) throw new HttpRequestException("O endpoint de IA não possui endereço válido.");
        // Reject mixed public/private DNS replies before making any connection.
        foreach (var address in addresses) ValidateAddress(uri, address);

        foreach (var address in addresses)
        {
            var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                await socket.ConnectAsync(new IPEndPoint(address, context.DnsEndPoint.Port), cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (SocketException) { socket.Dispose(); }
            catch { socket.Dispose(); throw; }
        }
        throw new HttpRequestException("Não foi possível conectar ao endpoint de IA.");
    }
}
