using CSharp8583.Common;
using System.Net.Sockets;

namespace PaymentGateway.Services.Interfaces;

public interface IMessageStrategy
{
    ValueTask<byte[]> HandleMessageAsync(IIsoMessage data, TcpClient? tcpClient = null);
}
