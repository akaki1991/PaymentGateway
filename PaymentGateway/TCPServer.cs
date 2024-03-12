using PaymentGateway.Shared.Helpers;
using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using CSharp8583;
using CSharp8583.Common;
using PaymentGateway.Services.Interfaces;

namespace PaymentGateway;

public class TCPServer
{
    private const string SuccessMessageLog = "Message: {clientMessage}, Date: {requestDate}, Execution time elapsed (milliseconds): {ElapsedMilliseconds}";
    private const string FailMessageLog = "Message: {clientMessage}, Date: {requestDate}, Execution time elapsed (milliseconds): {ElapsedMilliseconds}, Exception: {Message}";
    //private static readonly SemaphoreSlim _semaphore = new(10, 10);
    private readonly ILogger<TCPServer> _logger;

    private readonly ITransactionDataParser _transactionDataParser;

    public TCPServer(ILogger<TCPServer> logger, ITransactionDataParser transactionDataParser)
    {
        _logger = logger;
        _transactionDataParser = transactionDataParser;
    }

    public async Task StartServer(CancellationToken cancellationToken)
    {
        // Establish the local endpoint for the socket.
        IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
        int port = 8000;
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);

        // Create a TCP/IP socket.
        Socket listener = new(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        try
        {
            // Bind the socket to the local endpoint and listen for incoming connections.
            listener.Bind(localEndPoint);
            listener.Listen(100);

            Console.WriteLine($"Server started on {ipAddress}:{port}. Waiting for connections...");

            while (!cancellationToken.IsCancellationRequested)
            {
                // Start an asynchronous socket to listen for connections.
                Console.WriteLine("Waiting for a connection...");
                Socket handler = await listener.AcceptAsync(cancellationToken);

                _ = HandleClient(handler);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
        finally
        {
            listener.Close();
        }
    }

    public async Task HandleClient(Socket handler)
    {
        var requestDate = DateTimeOffset.UtcNow;
        Stopwatch sw = Stopwatch.StartNew();
        StringBuilder message = new(string.Empty);
        try
        {
            //await _semaphore.WaitAsync(); // Acquire the semaphore

            byte[] buffer = new byte[1024];

            while (true)
            {
                int bytesRead = await handler.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
                if (bytesRead <= 0)
                    break;

                message.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));

                if (handler.Available == 0)
                {
                    var parsedMessage = _transactionDataParser.ParseMessege(ByteArrayHelpers.HexToASCII(message.ToString()));

                    var response = HandleNetworkManagementRequest(parsedMessage);
                    await handler.SendAsync(new ArraySegment<byte>(response, 0, response.Length), SocketFlags.None);
                    break;
                }
            }

            sw.Stop();

            _logger.LogInformation(SuccessMessageLog, message, requestDate, sw.ElapsedMilliseconds);
        }
        catch (Exception e)
        {
            sw.Stop();
            _logger.LogError(FailMessageLog, message, requestDate, sw.ElapsedMilliseconds, e.Message);
            Console.WriteLine(e.ToString());
        }
        finally
        {
            //_semaphore.Release(); // Release the semaphore
            handler.Shutdown(SocketShutdown.Both);
            handler.Close();
        }
    }

    public static byte[] HandleNetworkManagementRequest(IIsoMessage message)
    {
        var iso8583 = new Iso8583(new FieldValidator());
        message.MTI.Value = "1814";
        message.SetFieldValue(39, "800");

        var asciiMessageBytes = iso8583.Build(message);

        return ByteArrayHelpers.AsciiByteArrayToHexByteArray(asciiMessageBytes);
    }
}
