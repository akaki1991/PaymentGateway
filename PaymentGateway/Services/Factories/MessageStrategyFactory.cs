using PaymentGateway.Services.Interfaces;
using PaymentGateway.Services.Strategies;

namespace PaymentGateway.Services.Factories;

public class MessageStrategyFactory : IMessageStrategyFactory
{
    public IMessageStrategy GetStrategy(string notificationType)
    {
        return notificationType switch
        {
            "1100" => new AuthorizationRequestMessageStrategy(),
            "1200" => new FinancialTransactionRequestMessageStrategy(),
            "1804" => new NetworkmanagementrequestMessageStrategy(),
            _ => throw new ArgumentException("Invalid message type", nameof(notificationType)),
        };
    }
}
