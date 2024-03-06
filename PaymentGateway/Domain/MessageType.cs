namespace PaymentGateway.Domain;

public enum MessageType
{
    None = 0,
    AuthorizationRequest = 1100,
    FinancialTransactionRequest = 1200,
    NetworkManagementRequest = 1804
}
