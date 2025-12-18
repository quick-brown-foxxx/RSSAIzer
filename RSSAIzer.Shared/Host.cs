using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Text.Json.Serialization;

namespace RSSAIzer.Shared;

public sealed record Host
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum HostnameType
    {
        IPv4,
        IPv4WithPort,
        IPv6,
        IPv6WithPort,
        Hostname,
        HostnameWithPort,
        Invalid,
    }

    public string HostPart { get; }
    public int? Port { get; }

    /// <exception cref="ArgumentException"></exception>
    public Host(string value)
    {
        var type = DetermineHostType(value);
        (HostPart, Port) = ParseHost(value, type);
    }

    public override string ToString()
    {
        return Port.HasValue
            ? IsIPv6(HostPart)
                ? $"[{HostPart}]:{Port}"
                : $"{HostPart}:{Port}"
            : HostPart;
    }

    public static implicit operator string(Host id) => id.ToString();

    public static HostnameType DetermineHostType(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return HostnameType.Invalid;
        }

        if (
            value.Count(c => c == '[') > 1
            || value.Count(c => c == ']') > 1
            || value.Count(c => c == '[') != value.Count(c => c == ']')
            || value.IndexOf(']') < value.IndexOf('[')
            || value.Contains("..")
            || value.EndsWith(':')
            || value.Any(c =>
                (!char.IsLetterOrDigit(c) && !".:[]-".Contains(c)) || !char.IsAscii(c)
            )
        )
        {
            return HostnameType.Invalid;
        }

        // Check for IPv6 with port: [IPv6]:port
        if (value.StartsWith('['))
        {
            var closingBracketIndex = value.IndexOf(']');
            if (closingBracketIndex == -1)
            {
                return HostnameType.Invalid;
            }

            if (closingBracketIndex < value.Length - 1 && value[closingBracketIndex + 1] == ':')
            {
                var portPart = value[(closingBracketIndex + 2)..];
                if (!IsValidPort(portPart))
                {
                    return HostnameType.Invalid;
                }

                var ipv6Part = value.Substring(1, closingBracketIndex - 1);

                return IsIPv6(ipv6Part) ? HostnameType.IPv6WithPort : HostnameType.Invalid;
            }

            if (closingBracketIndex == value.Length - 1)
            {
                var ipv6Part = value.Substring(1, closingBracketIndex - 1);
                return IsIPv6(ipv6Part) ? HostnameType.IPv6 : HostnameType.Invalid;
            }

            return HostnameType.Invalid;
        }

        // Check for port presence or Ipv6
        var lastColonIndex = value.LastIndexOf(':');
        if (lastColonIndex > 0)
        {
            var hostPart = value[..lastColonIndex];
            var portPart = value[(lastColonIndex + 1)..];

            if (!IsValidPort(portPart))
            {
                return HostnameType.Invalid;
            }

            if (IsIPv4(hostPart))
            {
                return HostnameType.IPv4WithPort;
            }

            if (IsValidHostname(hostPart))
            {
                return HostnameType.HostnameWithPort;
            }

            return IsIPv6(value) ? HostnameType.IPv6 : HostnameType.Invalid;
        }

        if (IsIPv4(value))
        {
            return HostnameType.IPv4;
        }

        if (IsIPv6(value))
        {
            return HostnameType.IPv6;
        }

        if (IsValidHostname(value))
        {
            return HostnameType.Hostname;
        }

        return HostnameType.Invalid;
    }

    public static bool IsValidPort(string portPart)
    {
        if (string.IsNullOrEmpty(portPart))
        {
            return false;
        }

        if (!int.TryParse(portPart, out var port))
        {
            return false;
        }

        return port is >= 0 and <= 65535;
    }

    public static bool IsIPv4(string hostPart)
    {
        return IPAddress.TryParse(hostPart, out var ip)
            && ip.AddressFamily == AddressFamily.InterNetwork;
    }

    public static bool IsIPv6(string hostPart)
    {
        return IPAddress.TryParse(hostPart, out var ip)
            && ip.AddressFamily == AddressFamily.InterNetworkV6;
    }

    public static bool IsValidHostname(string hostPart)
    {
        return Uri.CheckHostName(hostPart) == UriHostNameType.Dns;
    }

    public static bool TryParseHost(string hostName, out Host? host)
    {
        host = null;

        try
        {
            host = new(hostName);
        }
        catch (ArgumentException)
        {
            return false;
        }

        return true;
    }

#pragma warning disable CS8618, CS9264 //Non-nullable variable must contain a non-null value when exiting constructor
    [JsonConstructor]
    [Obsolete("For deserialization only", true)]
    public Host()
#pragma warning restore CS8618, CS9264
    { }

    private static (string Host, int? Port) ParseHost(string value, HostnameType type)
    {
        return type switch
        {
            HostnameType.IPv4 => ParseIPv4(value),
            HostnameType.IPv4WithPort => ParseIPv4WithPort(value),
            HostnameType.IPv6 => ParseIPv6(value),
            HostnameType.IPv6WithPort => ParseIPv6WithPort(value),
            HostnameType.Hostname => ParseHost(value),
            HostnameType.HostnameWithPort => ParseHostnameWithPort(value),
            HostnameType.Invalid => throw new ArgumentException(
                "Host must be a valid hostname or IP address without method or path, port allowed, only ASCII symbols"
            ),
            _ => throw new UnreachableException(),
        };
    }

    private static (string Host, int? Port) ParseIPv4(string value)
    {
        if (!IsIPv4(value))
        {
            throw new UnreachableException($"Host parser error {nameof(ParseIPv4)} with {value}");
        }

        return (value, null);
    }

    private static (string Host, int? Port) ParseIPv4WithPort(string value)
    {
        var lastColonIndex = value.LastIndexOf(':');
        var host = value[..lastColonIndex];
        var port = int.Parse(value[(lastColonIndex + 1)..]);

        if (!IsIPv4(host) || !IsValidPort(port.ToString()))
        {
            throw new UnreachableException(
                $"Host parser error {nameof(ParseIPv4WithPort)} with {value}"
            );
        }

        return (host, port);
    }

    private static (string Host, int? Port) ParseIPv6(string value)
    {
        if (!IsIPv6(value))
        {
            throw new UnreachableException($"Host parser error {nameof(ParseIPv6)} with {value}");
        }

        if (value.StartsWith('[') && value.EndsWith(']'))
        {
            value = value.Substring(1, value.Length - 2);
        }

        return (value, null);
    }

    private static (string Host, int? Port) ParseIPv6WithPort(string value)
    {
        var closingBracketIndex = value.IndexOf(']');
        var ipv6 = value.Substring(1, closingBracketIndex - 1);
        var portString = value[(closingBracketIndex + 2)..];
        var port = int.Parse(portString);

        if (!IsIPv6(ipv6) || !IsValidPort(port.ToString()))
        {
            throw new UnreachableException(
                $"Host parser error {nameof(ParseIPv6WithPort)} with {value}"
            );
        }

        return (ipv6, port);
    }

    private static (string Host, int? Port) ParseHost(string value)
    {
        if (!IsValidHostname(value))
        {
            throw new UnreachableException($"Host parser error {nameof(ParseHost)} with {value}");
        }

        return (value, null);
    }

    private static (string Host, int? Port) ParseHostnameWithPort(string value)
    {
        var lastColonIndex = value.LastIndexOf(':');
        var host = value[..lastColonIndex];
        var port = int.Parse(value[(lastColonIndex + 1)..]);

        if (!IsValidHostname(host) || !IsValidPort(port.ToString()))
        {
            throw new UnreachableException(
                $"Host parser error {nameof(ParseHostnameWithPort)} with {value}"
            );
        }

        return (host, port);
    }
}
