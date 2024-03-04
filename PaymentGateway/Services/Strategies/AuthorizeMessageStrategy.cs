using PaymentGateway.Services.Interfaces;

namespace PaymentGateway.Services.Strategies;

public class AuthorizeMessageStrategy : IMessageStrategy
{
    public Task HandleMessage(string data)
    {
        throw new NotImplementedException();
    }
}
