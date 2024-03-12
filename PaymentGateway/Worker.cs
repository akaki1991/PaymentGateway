using CSharp8583;
using CSharp8583.Common;
using PaymentGateway.Services.Interfaces;
using PaymentGateway.Shared.Helpers;
using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace PaymentGateway;

public class Worker : BackgroundService
{
    private const string SuccessMessageLog = "Message: {clientMessage}, Date: {requestDate}, Execution time elapsed (milliseconds): {ElapsedMilliseconds}";
    private const string FailMessageLog = "Message: {clientMessage}, Date: {requestDate}, Execution time elapsed (milliseconds): {ElapsedMilliseconds}, Exception: {Message}";

    private readonly ILogger<Worker> _logger;
    private readonly ITransactionDataParser _transactionDataParser;

    public Worker(ILogger<Worker> logger, ITransactionDataParser transactionDataParser)
    {
        _logger = logger;
        _transactionDataParser = transactionDataParser;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var ipAddress = IPAddress.Parse("127.0.0.1");
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
        var requestDate = DateTime.UtcNow;
        var stopWatch = new Stopwatch();
        stopWatch.Start();
        StringBuilder clientMessage = new(string.Empty);
        var bufferPool = ArrayPool<byte>.Shared;
        var buffer = bufferPool.Rent(8000);
        try
        {
            using (client)
            {
                using var stream = client.GetStream();
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

                if (clientMessage.Length == 0)
                    return;

                var message = _transactionDataParser.ParseMessege(ByteArrayHelpers.HexToASCII(clientMessage.ToString()));

                Console.WriteLine($"Received: {clientMessage}");
                var responseMessage = HandleNetworkManagementRequest(message);

                await stream.WriteAsync(responseMessage.AsMemory(0, responseMessage.Length), cancellationToken);

                stopWatch.Stop();
                _logger.LogInformation(SuccessMessageLog,
                                       clientMessage,
                                       requestDate,
                                       stopWatch.ElapsedMilliseconds);
            }
        }
        catch (Exception e)
        {
            stopWatch.Stop();
            _logger.LogInformation(FailMessageLog,
                                   clientMessage,
                                   requestDate,
                                   stopWatch.ElapsedMilliseconds,
                                   e.Message);
        }
        finally
        {
            client.Close();
        }
    }

    private void WorkerThread(TcpClient client)
    {
        var requestDate = DateTime.UtcNow;
        var stopWatch = new Stopwatch();
        stopWatch.Start();
        StringBuilder clientMessage = new(string.Empty);
        var bufferPool = ArrayPool<byte>.Shared;
        var buffer = bufferPool.Rent(8000);
        try
        {
            using (client)
            {
                using var stream = client.GetStream();
                int numberOfBytesRead;

                try
                {
                    while (client.Available > 0)
                    {
                        numberOfBytesRead = stream.Read(buffer, 0, buffer.Length);
                        clientMessage.Append(Encoding.ASCII.GetString(buffer, 0, numberOfBytesRead));
                    }
                }
                finally
                {
                    bufferPool.Return(buffer);
                }

                var message = _transactionDataParser.ParseMessege(ByteArrayHelpers.HexToASCII(clientMessage.ToString()));

                Console.WriteLine($"Received: {clientMessage}");
                var responseMessage = HandleNetworkManagementRequest(message);

                stream.Write(responseMessage, 0, responseMessage.Length);

                stopWatch.Stop();
                _logger.LogInformation(SuccessMessageLog,
                                       clientMessage,
                                       requestDate,
                                       stopWatch.ElapsedMilliseconds);
            }
        }
        catch (Exception e)
        {
            stopWatch.Stop();
            _logger.LogInformation(FailMessageLog,
                                   clientMessage,
                                   requestDate,
                                   stopWatch.ElapsedMilliseconds,
                                   e.Message);
        }
        finally
        {
            client.Close();
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
