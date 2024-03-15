using CSharp8583.Common;
using PaymentGateway.Services.Interfaces;
using System.Net.Sockets;
using CSharp8583;
using PaymentGateway.Shared.Helpers;

namespace PaymentGateway.Services.Strategies;

public class FinancialTransactionRequestMessageStrategy : IMessageStrategy
{
    public ValueTask<byte[]> HandleMessageAsync(IIsoMessage data, TcpClient? tcpClient = null)
    {
        var iso8583 = new Iso8583(new FieldValidator());

        var asciiMessageBytes = iso8583.Build(data);

        return ValueTask.FromResult(asciiMessageBytes);
    }
}
