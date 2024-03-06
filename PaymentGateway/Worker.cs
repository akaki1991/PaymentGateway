using CSharp8583;
using CSharp8583.Common;
using Microsoft.Extensions.Logging;
using PaymentGateway.Services.Implementations;
using PaymentGateway.Services.Interfaces;
using PaymentGateway.Shared.Helpers;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace PaymentGateway;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ITransactionDataParser _transactionDataParser;
    private readonly string _filePath;

    public Worker(ILogger<Worker> logger, ITransactionDataParser transactionDataParser)
    {
        _logger = logger;
        _transactionDataParser = transactionDataParser;
        _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log", "log.txt");
    }

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

            _ = HandleClientAsync(client, stoppingToken);
        }
    }

    async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        StringBuilder clientMessage = new(string.Empty);
        var bufferPool = ArrayPool<byte>.Shared;
        var buffer = bufferPool.Rent(8000);
        try
        {
            using (client)
            {
                var stream = client.GetStream();
                int numberOfBytesRead;

                try
                {
                    while (client.Available > 0)
                    {
                        numberOfBytesRead = await stream.ReadAsync(buffer, cancellationToken);
                        clientMessage.Append(Encoding.ASCII.GetString(buffer, 0, numberOfBytesRead));
                    }
                }
                finally
                {
                    bufferPool.Return(buffer);
                }

                var message = _transactionDataParser.ParseMessege(ByteArrayHelpers.HexToASCII(clientMessage.ToString()));

                Console.WriteLine($"Received: {clientMessage}");
                //var responseMessage = HandleNetworkManagementRequest(message);

                await stream.WriteAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                File.AppendAllText(_filePath, clientMessage + Environment.NewLine);
            }
        }
        catch (Exception e)
        {
            File.AppendAllText(_filePath, $"{clientMessage} Exception: {e.Message}" + Environment.NewLine);
            Console.WriteLine($"Exception in handling client: {e.Message}");
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
