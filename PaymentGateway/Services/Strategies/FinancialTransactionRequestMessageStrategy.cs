using PaymentGateway.Services.Interfaces;

namespace PaymentGateway.Services.Strategies;

public class FinancialTransactionRequestMessageStrategy : IMessageStrategy
{
    public Task HandleMessage(string data)
    {
        throw new NotImplementedException();
    }
}
