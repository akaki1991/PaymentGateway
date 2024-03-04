namespace PaymentGateway.Services.Interfaces;

public interface IMessageStrategy
{
    Task HandleMessage(string data);
}
