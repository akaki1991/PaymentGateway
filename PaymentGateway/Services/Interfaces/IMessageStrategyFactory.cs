namespace PaymentGateway.Services.Interfaces;

public interface IMessageStrategyFactory
{
    IMessageStrategy GetStrategy(string notificationType);
}
