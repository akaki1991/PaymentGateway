using CSharp8583;
using CSharp8583.Common;
using PaymentGateway.Services.Interfaces;
using PaymentGateway.Shared.Helpers;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace PaymentGateway;

public class Worker : BackgroundService
{
    private const string SuccessMessageLog = "Message: {clientMessage}, Date: {requestDate}, Execution time elapsed (milliseconds): {ElapsedMilliseconds}";
    private const string FailMessageLog = "Message: {clientMessage}, Date: {requestDate}, Execution time elapsed (milliseconds): {ElapsedMilliseconds}, Exception: {Message}";

    private readonly ILogger<Worker> _logger;
    private readonly ITransactionDataParser _transactionDataParser;
    private readonly SemaphoreSlim _connectionSemaphore = new(initialCount: 150); // Adjust the count as needed

    public Worker(ILogger<Worker> logger, ITransactionDataParser transactionDataParser)
    {
        _logger = logger;
        _transactionDataParser = transactionDataParser;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var ipAddress = IPAddress.Parse("127.0.0.1");
        var port = 8000;
        var localEndPoint = new IPEndPoint(ipAddress, port);

        using var listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        listener.Bind(localEndPoint);
        listener.Listen(backlog: 100); // The maximum length of the pending connections queue.
        Console.WriteLine($"Server started on {ipAddress}:{port}. Waiting for connections...");

        while (!stoppingToken.IsCancellationRequested)
        {
            var client = await listener.AcceptAsync(stoppingToken);
            Console.WriteLine("Connected to a client. Handling connection in a separate task...");

            _ = HandleClientAsync(client, stoppingToken).ContinueWith(t => _connectionSemaphore.Release());
        }
    }

    private async Task HandleClientAsync(Socket client, CancellationToken cancellationToken)
    {
        if (!client.Connected)
        {
            Console.WriteLine("Client is not connected.");
            return;
        }

        var requestDate = DateTime.UtcNow;
        var stopWatch = new Stopwatch();
        stopWatch.Start();
        StringBuilder clientMessage = new(string.Empty);
        var bufferPool = ArrayPool<byte>.Shared;
        var buffer = bufferPool.Rent(8000);

        try
        {
            int numberOfBytesRead;
            try
            {
                while (client.Available > 0)
                {
                    numberOfBytesRead = await client.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None, cancellationToken);
                    clientMessage.Append(Encoding.ASCII.GetString(buffer, 0, numberOfBytesRead));
                }
            }
            finally
            {
                bufferPool.Return(buffer);
            }

            var message = _transactionDataParser.ParseMessage(ByteArrayHelpers.HexToASCII(clientMessage.ToString()));
            Console.WriteLine($"Received: {clientMessage}");

            var sendMessage = HandleNetworkManagementRequest(message);

            await client.SendAsync(new ArraySegment<byte>(sendMessage), SocketFlags.None, cancellationToken);
            stopWatch.Stop();
            _logger.LogInformation(SuccessMessageLog, clientMessage, requestDate, stopWatch.ElapsedMilliseconds);
        }
        catch (Exception e)
        {
            stopWatch.Stop();
            _logger.LogInformation(FailMessageLog, clientMessage, requestDate, stopWatch.ElapsedMilliseconds, e.Message);
        }
        finally
        {
            client.Shutdown(SocketShutdown.Both);
            client.Close();
        }
    }

    public static byte[] HandleNetworkManagementRequest(IIsoMessage message)
    {
        var iso8583 = new Iso8583(new FieldValidator());
        //message.MTI.Value = "1814";
        //message.SetFieldValue(39, "800");

        var asciiMessageBytes = iso8583.Build(message);

        return ByteArrayHelpers.AsciiByteArrayToHexByteArray(asciiMessageBytes);
    }
}
