using CSharp8583.Common;

namespace PaymentGateway.Services.Interfaces;

public interface ITransactionDataParser
{
    public IIsoMessage ParseMessage(string message);
}
