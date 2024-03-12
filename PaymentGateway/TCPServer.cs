using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace PaymentGateway;

public class TCPServer
{
    private TcpListener _listener;

    public TCPServer()
    {
        StartServer();
    }

    private void StartServer()
    {
        var ipAddress = IPAddress.Parse("127.0.0.1");
        var port = 8000;
        _listener = new TcpListener(ipAddress, port);
        _listener.Start();

        var bufferPool = ArrayPool<byte>.Shared;
        var buffer = bufferPool.Rent(8000);
        StringBuilder clientMessage = new(string.Empty);

        using TcpClient client = _listener.AcceptTcpClient();

        var tcpStream = client.GetStream();

        int numberOfBytesRead;

        while ((numberOfBytesRead = tcpStream.Read(buffer, 0, buffer.Length)) != 0)
        {
            clientMessage.Append(Encoding.ASCII.GetString(buffer, 0, numberOfBytesRead));
        }

    }
}
