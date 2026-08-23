namespace ControllerBridge.Transport.WiFi;

using QRCoder;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

/// <summary>
/// QR Code generator for easy mobile client connection.
/// </summary>
public class QrCodeGenerator
{
    /// <summary>
    /// Generate connection QR code data (URL-safe JSON).
    /// </summary>
    public static string GenerateConnectionData(string hostName, int port)
    {
        var data = new
        {
            host = hostName,
            port = port,
            version = "1.0",
            protocol = "udp"
        };

        return JsonSerializer.Serialize(data);
    }

    /// <summary>
    /// Generate QR code image as PNG bytes.
    /// </summary>
    public static byte[] GenerateQrCodePng(string data)
    {
        using (var qrGenerator = new QRCodeGenerator())
        {
            var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.M);
            using (var qrCode = new PngByteQRCode(qrCodeData))
            {
                return qrCode.GetGraphic(10); // 10 pixels per module
            }
        }
    }

    /// <summary>
    /// Generate QR code as SVG string.
    /// </summary>
    public static string GenerateQrCodeSvg(string data)
    {
        using (var qrGenerator = new QRCodeGenerator())
        {
            var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.M);
            using (var qrCode = new SvgQRCode(qrCodeData))
            {
                return qrCode.GetGraphic(10); // 10 pixels per module
            }
        }
    }

    /// <summary>
    /// Get local machine hostname or IP.
    /// </summary>
    public static string GetLocalHostname()
    {
        try
        {
            var hostName = Dns.GetHostName();
            return hostName;
        }
        catch
        {
            return "localhost";
        }
    }

    /// <summary>
    /// Get local IP address on network.
    /// </summary>
    public static string GetLocalIpAddress()
    {
        try
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                if (socket.LocalEndPoint is IPEndPoint endPoint)
                {
                    return endPoint.Address.ToString();
                }
            }
        }
        catch { }

        return "127.0.0.1";
    }
}
