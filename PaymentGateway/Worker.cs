using CSharp8583;
using CSharp8583.Common;
using PaymentGateway.Services.Interfaces;
using PaymentGateway.Shared.Helpers;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace PaymentGateway;

public class Worker(ILogger<Worker> logger, ITransactionDataParser transactionDataParser) : BackgroundService
{
    private readonly ILogger<Worker> _logger = logger;
    private readonly ITransactionDataParser _transactionDataParser = transactionDataParser;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var ipAddress = IPAddress.Loopback;
        var port = 8000;
        var listener = new TcpListener(ipAddress, port);

        listener.Start();
        Console.WriteLine($"Server started on {ipAddress}:{port}. Waiting for connections...");

        while (!stoppingToken.IsCancellationRequested)
        {
            var client = await listener.AcceptTcpClientAsync(stoppingToken);
            Console.WriteLine("Connected to a client. Handling connection in a separate task...");

            _ = Task.Run(() => HandleClientAsync(client, stoppingToken), stoppingToken);
        }
    }

    async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        var bufferPool = ArrayPool<byte>.Shared;
        var buffer = bufferPool.Rent(8192);
        try
        {
            using (client)
            {
                var stream = client.GetStream();
                int numberOfBytesRead;

                while ((numberOfBytesRead = await stream.ReadAsync(buffer, cancellationToken)) != 0)
                {
                    var clientMessage = Encoding.ASCII.GetString(buffer, 0, numberOfBytesRead);

                    var message = _transactionDataParser.ParseMessege(ByteArrayHelpers.HexToASCII(clientMessage));

                    Console.WriteLine($"Received: {clientMessage}");
                    var responseMessage = HandleNetworkManagementRequest(message);
                    await stream.WriteAsync(responseMessage.AsMemory(0, responseMessage.Length), cancellationToken);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Exception in handling client: {e.Message}");
        }
        finally
        {
            bufferPool.Return(buffer);
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
