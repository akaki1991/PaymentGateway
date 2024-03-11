using CSharp8583.Common;
using PaymentGateway.Services.Interfaces;
using System.Text;
using CSharp8583;

namespace PaymentGateway.Services.Implementations;

public class TransactionDataParser : ITransactionDataParser
{
    public IIsoMessage ParseMessage(string message)
    {
        var asciiBytes = Encoding.ASCII.GetBytes(message);

        var iso8583 = new Iso8583(new FieldValidator());

        var result = iso8583.Parse(asciiBytes, ConstantValues.GetDefaultIsoSpecsFromFile());

        return result;
    }
}
