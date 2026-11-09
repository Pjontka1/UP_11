using System.Text;

namespace ModemTerminal;

/// <summary>
/// Windows-1250: strona kodowa Europy Środkowej. Polskie znaki
/// (ą, ć, ę, ł, ń, ó, ś, ź, ż) nie mieszczą się w ISO-8859-1.
/// </summary>
public sealed class Cp1250Encoding : Encoding
{
    private static readonly char[] From80 =
    [
        '\u20AC', '\u0081', '\u201A', '\u0083', '\u201E', '\u2026', '\u2020', '\u2021',
        '\u0088', '\u2030', '\u0160', '\u2039', '\u015A', '\u0164', '\u017D', '\u0179',
        '\u0090', '\u2018', '\u2019', '\u201C', '\u201D', '\u2022', '\u2013', '\u2014',
        '\u0098', '\u2122', '\u0161', '\u203A', '\u015B', '\u0165', '\u017E', '\u017A',
        '\u00A0', '\u02C7', '\u02D8', '\u0141', '\u00A4', '\u0104', '\u00A6', '\u00A7',
        '\u00A8', '\u00A9', '\u015E', '\u00AB', '\u00AC', '\u00AD', '\u00AE', '\u017B',
        '\u00B0', '\u00B1', '\u02DB', '\u0142', '\u00B4', '\u00B5', '\u00B6', '\u00B7',
        '\u00B8', '\u0105', '\u015F', '\u00BB', '\u013D', '\u02DD', '\u013E', '\u017C',
        '\u0154', '\u00C1', '\u00C2', '\u0102', '\u00C4', '\u0139', '\u0106', '\u00C7',
        '\u010C', '\u00C9', '\u0118', '\u00CB', '\u011A', '\u00CD', '\u00CE', '\u010E',
        '\u0110', '\u0143', '\u0147', '\u00D3', '\u00D4', '\u0150', '\u00D6', '\u00D7',
        '\u0158', '\u016E', '\u00DA', '\u0170', '\u00DC', '\u00DD', '\u0162', '\u00DF',
        '\u0155', '\u00E1', '\u00E2', '\u0103', '\u00E4', '\u013A', '\u0107', '\u00E7',
        '\u010D', '\u00E9', '\u0119', '\u00EB', '\u011B', '\u00ED', '\u00EE', '\u010F',
        '\u0111', '\u0144', '\u0148', '\u00F3', '\u00F4', '\u0151', '\u00F6', '\u00F7',
        '\u0159', '\u016F', '\u00FA', '\u0171', '\u00FC', '\u00FD', '\u0163', '\u02D9',
    ];

    private static readonly Dictionary<char, byte> ToByte = BuildMap();

    public override int GetByteCount(char[] chars, int index, int count) => count;

    public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
    {
        for (int i = 0; i < charCount; i++)
            bytes[byteIndex + i] = Encode(chars[charIndex + i]);
        return charCount;
    }

    public override int GetCharCount(byte[] bytes, int index, int count) => count;

    public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
    {
        for (int i = 0; i < byteCount; i++)
            chars[charIndex + i] = Decode(bytes[byteIndex + i]);
        return byteCount;
    }

    public override int GetMaxByteCount(int charCount) => charCount;

    public override int GetMaxCharCount(int byteCount) => byteCount;

    public static byte Encode(char c)
    {
        if (c < 0x80) return (byte)c;
        return ToByte.TryGetValue(c, out byte b) ? b : (byte)'?';
    }

    public static char Decode(byte b) => b < 0x80 ? (char)b : From80[b - 0x80];

    private static Dictionary<char, byte> BuildMap()
    {
        var map = new Dictionary<char, byte>(From80.Length);
        for (int i = 0; i < From80.Length; i++)
            map[From80[i]] = (byte)(0x80 + i);
        return map;
    }
}
