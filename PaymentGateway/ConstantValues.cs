using CSharp8583.Models;
using Newtonsoft.Json;

namespace PaymentGateway;

public static class ConstantValues
{
    public static IsoMessage? GetDefaultIsoSpecsFromFile() => 
        JsonConvert.DeserializeObject<IsoMessage>(File.ReadAllText("./DefaultIsoMessage.json"));
}
