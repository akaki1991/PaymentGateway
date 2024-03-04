using CSharp8583.Common;

namespace PaymentGateway.Shared;

public static class IIsoMessageExtensions
{
    public static bool IsNetworkManagementRequest(this IIsoMessage isoMessage) =>
        isoMessage?.MTI?.Value == "1804";
}
