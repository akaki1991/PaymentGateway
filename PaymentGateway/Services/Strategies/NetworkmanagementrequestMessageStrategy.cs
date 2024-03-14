using CSharp8583;
using CSharp8583.Common;
using PaymentGateway.Services.Interfaces;
using PaymentGateway.Shared.Helpers;
using System.Net.Sockets;

namespace PaymentGateway.Services.Strategies;

public class NetworkmanagementrequestMessageStrategy : IMessageStrategy
{
    public ValueTask<byte[]> HandleMessageAsync(IIsoMessage data, TcpClient? tcpClient = null)
    {
        var iso8583 = new Iso8583(new FieldValidator());
        data.MTI.Value = "1814";
        data.SetFieldValue(39, "800");

        var asciiMessageBytes = iso8583.Build(data);

        return ValueTask.FromResult(ByteArrayHelpers.AsciiByteArrayToHexByteArray(asciiMessageBytes));
    }
}
