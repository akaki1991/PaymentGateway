using System.Text;

namespace PaymentGateway.Shared.Helpers;

public static class ByteArrayHelpers
{
    public static byte[] AsciiByteArrayToHexByteArray(byte[] asciiMessageBytes)
    {
        var hexString = BitConverter.ToString(asciiMessageBytes).Replace("-", string.Empty);

        return Encoding.ASCII.GetBytes(hexString);
    }

    public static string HexToASCII(string hex)
    {
        var ascii = string.Empty;

        for (int i = 0; i < hex.Length; i += 2)
        {
            var part = hex.Substring(i, 2);
            char ch = (char)Convert.ToInt32(part, 16);
            ascii += ch;
        }
        return ascii;
    }
}
