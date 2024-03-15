using PaymentGateway.Shared.Helpers;
using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using CSharp8583;
using CSharp8583.Common;
using PaymentGateway.Services.Interfaces;
using System;
using Microsoft.Extensions.Options;
using PaymentGateway.Configuration;

namespace PaymentGateway;

public class TCPServer
{
    private const string SuccessMessageLog = "Message: {clientMessage}, Date: {requestDate}, Execution time elapsed (milliseconds): {ElapsedMilliseconds}";
    private const string FailMessageLog = "Message: {clientMessage}, Date: {requestDate}, Execution time elapsed (milliseconds): {ElapsedMilliseconds}, Exception: {Message}";

    private readonly ILogger<TCPServer> _logger;
    private readonly ITransactionDataParser _transactionDataParser;
    private readonly IMessageStrategyFactory _messageStrategyFactory;
    private readonly PaymentGatewayConfig _gatewayConfig;

    public TCPServer(ILogger<TCPServer> logger,
        ITransactionDataParser transactionDataParser,
        IMessageStrategyFactory messageStrategyFactory,
        IOptions<PaymentGatewayConfig> options)
    {
        _logger = logger;
        _transactionDataParser = transactionDataParser;
        _messageStrategyFactory = messageStrategyFactory;
        _gatewayConfig = options.Value;
    }

    public async Task StartServer(CancellationToken cancellationToken)
    {
        // Establish the local endpoint for the socket.
        IPAddress ipAddress = IPAddress.Parse(_gatewayConfig.IpAddress);
        IPEndPoint localEndPoint = new(ipAddress, _gatewayConfig.Port);

        // Create a TCP/IP socket.
        Socket listener = new(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        try
        {
            // Bind the socket to the local endpoint and listen for incoming connections.
            listener.Bind(localEndPoint);
            listener.Listen(100);

            Console.WriteLine($"Server started on {ipAddress}:{_gatewayConfig.Port}. Waiting for connections...");

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
        var bufferPool = ArrayPool<byte>.Shared;
        var buffer = bufferPool.Rent(8000);
        try
        {
            while (true)
            {
                int bytesRead = await handler.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
                if (bytesRead <= 0)
                    break;

                message.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));

                if (handler.Available == 0)
                {
                    var pureMessage = message.ToString()[6..];
                    var parsedMessage = _transactionDataParser.ParseMessege(pureMessage);

                    var messageStrategy = _messageStrategyFactory.GetStrategy(parsedMessage.MTI.Value);

                    byte[] response = await messageStrategy.HandleMessageAsync(parsedMessage);

                    var responseAsString = Encoding.ASCII.GetString(response);

                    responseAsString = responseAsString.Length.ToString().PadLeft(6, '0') + responseAsString;

                    response = Encoding.ASCII.GetBytes(responseAsString);

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
            bufferPool.Return(buffer);
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
