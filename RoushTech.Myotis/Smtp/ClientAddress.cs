namespace RoushTech.Myotis.Smtp;

using System.Net;
using Microsoft.Extensions.Options;
using SmtpServer;
using SmtpServer.Net;
using SmtpServer.Protocol;

public class ClientAddress(IOptionsMonitor<SmtpOptions> options)
{
    // Any client can send a PROXY header before EHLO, so it only counts when the connection comes from a trusted proxy.
    public IPAddress Resolve(ISessionContext context)
    {
        var remote = (context.Properties.TryGetValue(EndpointListener.RemoteEndPointKey, out var value) ? value as IPEndPoint : null)?.Address ?? IPAddress.None;
        if (remote.IsIPv4MappedToIPv6)
        {
            remote = remote.MapToIPv4();
        }

        if (context.Properties.TryGetValue(ProxyCommand.ProxySourceEndpointKey, out var proxied) && proxied is IPEndPoint source && IsTrusted(remote))
        {
            return source.Address.IsIPv4MappedToIPv6 ? source.Address.MapToIPv4() : source.Address;
        }

        return remote;
    }

    private bool IsTrusted(IPAddress address) =>
        options.CurrentValue.TrustedProxies
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(entry => IPNetwork.TryParse(entry.Contains('/') ? entry : $"{entry}/{(entry.Contains(':') ? 128 : 32)}", out var network) && network.Contains(address));
}
