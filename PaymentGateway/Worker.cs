using System.Buffers;
using System.Net.Sockets;
using System.Net;
using System.Text;
using PaymentGateway.Services.Interfaces;
using PaymentGateway.Shared;
using CSharp8583.Common;
using CSharp8583;

namespace PaymentGateway;

public class Worker(ILogger<Worker> logger, ITransactionDataParser transactionDataParser) : BackgroundService
{
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
        Task.Delay(1000, cancellationToken).Wait(cancellationToken);
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
                    // Convert the data received into a string
                    var clientMessage = Encoding.ASCII.GetString(buffer, 0, numberOfBytesRead);

                    var message = transactionDataParser.ParseMessege(HexToASCII(clientMessage));

                    //if (message.IsNetworkManagementRequest())
                    {
                        Console.WriteLine($"Received: {clientMessage}");
                        var responseMessage = HandleNetworkManagementRequest(message);
                        await stream.WriteAsync(responseMessage.AsMemory(0, responseMessage.Length), cancellationToken);
                    }
                    //else
                    //{
                    //    // Send a response (echo) back to the client
                    //    await stream.WriteAsync(buffer.AsMemory(0, numberOfBytesRead), cancellationToken);
                    //    Console.WriteLine("Sent back the same message to the client.");
                    //}
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

        var test = Encoding.ASCII.GetString(asciiMessageBytes);
        var hex = AsciiToHex(test);

        return ConvertHexToBytes(hex);
    }

    public static byte[] ConvertAsciiBytesToHexBytes(byte[] asciiBytes)
    {
        List<byte> hexBytes = new(asciiBytes.Length * 2); // Each ASCII byte will be represented by two hex bytes
        foreach (byte b in asciiBytes)
        {
            // Extract the high and low nibbles and convert them to their hex byte representation
            byte highNibble = (byte)((b >> 4) & 0x0F);
            byte lowNibble = (byte)(b & 0x0F);
            hexBytes.Add(Convert.ToByte(highNibble.ToString(), 16));
            hexBytes.Add(Convert.ToByte(lowNibble.ToString(), 16));
        }
        return [.. hexBytes];
    }

    public static string AsciiToHex(string asciiString)
    {
        StringBuilder builder = new();
        foreach (char c in asciiString)
        {
            builder.Append(Convert.ToInt32(c).ToString("X"));
        }
        return builder.ToString();
    }

    public static byte[] ConvertHexToBytes(string hexString)
    {
        if (hexString.Length % 2 != 0)
            throw new ArgumentException("Invalid hex string", nameof(hexString));

        byte[] bytes = new byte[hexString.Length / 2];
        for (int i = 0; i < hexString.Length; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
        }
        return bytes;
    }

    public static string HexToASCII(string hex)
    {
        var ascii = string.Empty;

        for (int i = 0; i < hex.Length; i += 2)
        {
            var part = hex.Substring(i, 2);
            char ch = (char)Convert.ToInt32(part, 16);
            ascii += ch;
        }
        return ascii;
    }
}
