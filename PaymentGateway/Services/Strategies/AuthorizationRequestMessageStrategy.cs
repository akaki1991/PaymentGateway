using PaymentGateway.Services.Interfaces;

namespace PaymentGateway.Services.Strategies;

public class AuthorizationRequestMessageStrategy : IMessageStrategy
{
    public Task HandleMessage(string data)
    {
        throw new NotImplementedException();
    }
}
