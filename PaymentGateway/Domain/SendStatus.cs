namespace PaymentGateway.Domain;

public enum SendStatus
{
    None = 0,
    RecivedFromUFC = 1,
    SentToUFC = 2,
    SentToD8 = 3,
    RecivedFromD8 = 4,
}
