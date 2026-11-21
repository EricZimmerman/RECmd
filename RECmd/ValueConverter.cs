using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;

namespace RECmd;

/// <summary>
///     Batch file BinaryConvert (RECmd issue #117): turns a value's data into what it holds. Binary and number values are
///     read from their bytes; string values are read as text, and as a number (decimal or 0x hex) for the conversions
///     that want one. Input may be anything: a conversion that doesn't fit gives false
/// </summary>
public static class ValueConverter
{
    private static readonly DateTimeOffset MacEpoch = new(2001, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly Encoding Ansi = CodePagesEncodingProvider.Instance.GetEncoding(1252);

    /// <param name="kind">The conversion</param>
    /// <param name="valueType">Registry value type, as the Registry package names them (RegBinary, RegSz, RegDword, ...)</param>
    /// <param name="raw">The value's data</param>
    /// <param name="text">The value's data as the Registry package shows it (the string, for string types)</param>
    /// <param name="dateFormat">--dt</param>
    /// <param name="result">The converted data</param>
    public static bool TryConvert(Key.BinConvert kind, string valueType, byte[] raw, string text, string dateFormat, out string result)
    {
        result = null;
        try
        {
            result = Convert(kind, valueType, raw ?? Array.Empty<byte>(), text ?? string.Empty, dateFormat);
        }
        catch (Exception e) when (e is ArgumentException or FormatException or OverflowException or IndexOutOfRangeException or InvalidOperationException)
        {
            result = null;
        }

        return result != null;
    }

    private static bool IsString(string valueType) => valueType is "RegSz" or "RegExpandSz" or "RegMultiSz";

    /// <summary>The bytes to read a number from: the value's bytes, or for a string value the number it holds, as 8 little-endian bytes</summary>
    private static byte[] NumberBytes(string valueType, byte[] raw, string text)
    {
        if (IsString(valueType) == false)
        {
            return raw;
        }

        var t = text.Trim().TrimEnd('\0');
        if (t.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return BitConverter.GetBytes(ulong.Parse(t.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
        }

        if (long.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l))
        {
            return BitConverter.GetBytes(l);
        }

        if (double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
        {
            return BitConverter.GetBytes(d);
        }

        throw new FormatException($"'{t}' is not a number");
    }

    private static byte[] Need(byte[] b, int count)
    {
        if (b.Length < count)
        {
            throw new ArgumentException($"{count} bytes needed, the value has {b.Length}");
        }

        return b;
    }

    private static string Convert(Key.BinConvert kind, string valueType, byte[] raw, string text, string dt)
    {
        switch (kind)
        {
            case Key.BinConvert.Filetime:
                return DateTimeOffset.FromFileTime((long)BitConverter.ToUInt64(Need(NumberBytes(valueType, raw, text), 8), 0)).ToUniversalTime().ToString(dt);
            case Key.BinConvert.Ip:
                return new IPAddress(BitConverter.ToUInt32(Need(NumberBytes(valueType, raw, text), 4), 0)).ToString();
            case Key.BinConvert.Epoch:
                return DateTimeOffset.FromUnixTimeSeconds(BitConverter.ToUInt32(Need(NumberBytes(valueType, raw, text), 4), 0)).ToUniversalTime().ToString(dt);
            case Key.BinConvert.Sid:
                return Sid(Need(raw, 8));
            case Key.BinConvert.Systemtime:
            {
                var b = Need(raw, 16);
                int S(int i) => BitConverter.ToInt16(b, i);
                return new DateTimeOffset(new DateTime(S(0), S(2), S(6), S(8), S(10), S(12), S(14), DateTimeKind.Utc)).ToUniversalTime().ToString(dt);
            }
            case Key.BinConvert.DateTimeTicks:
                return new DateTime(BitConverter.ToInt64(Need(NumberBytes(valueType, raw, text), 8), 0), DateTimeKind.Utc).ToUniversalTime().ToString(dt);
            case Key.BinConvert.OLE:
                return DateTime.FromOADate(BitConverter.ToDouble(Need(NumberBytes(valueType, raw, text), 8), 0)).ToString(dt);

            case Key.BinConvert.EpochMilliseconds:
                return DateTimeOffset.FromUnixTimeMilliseconds(BitConverter.ToInt64(Need(NumberBytes(valueType, raw, text), 8), 0)).ToUniversalTime().ToString(dt);
            case Key.BinConvert.WebKit:
            {
                //microseconds since 1601-01-01 (Chrome and other WebKit/Chromium programs)
                var us = BitConverter.ToInt64(Need(NumberBytes(valueType, raw, text), 8), 0);
                return DateTimeOffset.FromFileTime(checked(us * 10)).ToUniversalTime().ToString(dt);
            }
            case Key.BinConvert.MacAbsolute:
            {
                //seconds since 2001-01-01 (Cocoa/Core Data), stored as a double; a string may hold a whole number
                double seconds;
                if (IsString(valueType))
                {
                    seconds = double.Parse(text.Trim().TrimEnd('\0'), NumberStyles.Float, CultureInfo.InvariantCulture);
                }
                else
                {
                    seconds = BitConverter.ToDouble(Need(raw, 8), 0);
                }

                if (double.IsNaN(seconds) || double.IsInfinity(seconds))
                {
                    throw new ArgumentException("not a time");
                }

                return MacEpoch.AddSeconds(seconds).ToString(dt);
            }
            case Key.BinConvert.DosTimeDate:
                return Dos(Need(raw, 4), 2, 0).ToString(dt);
            case Key.BinConvert.DosDateTime:
                return Dos(Need(raw, 4), 0, 2).ToString(dt);

            case Key.BinConvert.Ascii:
                return Ansi.GetString(raw).TrimEnd('\0');
            case Key.BinConvert.Unicode:
                return IsString(valueType) ? text : Encoding.Unicode.GetString(raw, 0, raw.Length & ~1).TrimEnd('\0');
            case Key.BinConvert.ToBase64:
                return System.Convert.ToBase64String(raw);
            case Key.BinConvert.FromBase64:
                return FromBase64(IsString(valueType) ? text : Encoding.Unicode.GetString(raw, 0, raw.Length & ~1));
            case Key.BinConvert.Rot13:
                return Rot13(IsString(valueType) ? text : Encoding.Unicode.GetString(raw, 0, raw.Length & ~1).TrimEnd('\0'));

            case Key.BinConvert.Guid:
            {
                var g = new Guid(Need(raw, 16).Take(16).ToArray()).ToString("D");
                var name = GuidMapping.GuidMapping.GetDescriptionFromGuid(g);
                return string.IsNullOrEmpty(name) || name.StartsWith("Unmapped GUID", StringComparison.OrdinalIgnoreCase) ? g : $"{g} ({name})";
            }
            case Key.BinConvert.Ipv6:
                return new IPAddress(Need(raw, 16).Take(16).ToArray()).ToString();
            case Key.BinConvert.MacAddress:
                return string.Join(":", Need(raw, 6).Take(6).Select(t => t.ToString("X2")));
            case Key.BinConvert.BigEndian:
                return BigEndian(IsString(valueType) ? NumberBytes(valueType, raw, text) : raw);

            case Key.BinConvert.ProductKey:
                return ProductKeyWin8(Need(raw, 67));
            case Key.BinConvert.ProductKeyOld:
                return ProductKeyOld(Need(raw, 67));

            default:
                return null;
        }
    }

    /// <summary>S-R-I-S-S..., as SecurityIdentifier writes it (the authority in hex when it doesn't fit in 32 bits), on any OS</summary>
    private static string Sid(byte[] b)
    {
        var revision = b[0];
        var count = b[1];
        if (revision != 1 || count > 15 || b.Length < 8 + count * 4)
        {
            throw new ArgumentException("not a SID");
        }

        ulong authority = 0;
        for (var i = 2; i < 8; i++)
        {
            authority = (authority << 8) | b[i];
        }

        var sb = new StringBuilder("S-1-");
        sb.Append(authority < 0x100000000 ? authority.ToString(CultureInfo.InvariantCulture) : $"0x{authority:X12}");
        for (var i = 0; i < count; i++)
        {
            sb.Append('-').Append(BitConverter.ToUInt32(b, 8 + i * 4).ToString(CultureInfo.InvariantCulture));
        }

        return sb.ToString();
    }

    /// <summary>An MS-DOS date and time: the date word at <paramref name="dateAt" />, the time word at <paramref name="timeAt" /> (2 second resolution)</summary>
    private static DateTime Dos(byte[] b, int dateAt, int timeAt)
    {
        var date = BitConverter.ToUInt16(b, dateAt);
        var time = BitConverter.ToUInt16(b, timeAt);
        return new DateTime(((date >> 9) & 0x7F) + 1980, (date >> 5) & 0x0F, date & 0x1F, (time >> 11) & 0x1F, (time >> 5) & 0x3F, (time & 0x1F) * 2);
    }

    /// <summary>Base64 text decoded: as text when it is UTF-16 or printable Windows-1252, else as hex bytes</summary>
    private static string FromBase64(string s)
    {
        var bytes = System.Convert.FromBase64String(s.Trim().TrimEnd('\0'));
        if (bytes.Length >= 2 && bytes.Length % 2 == 0 && bytes.Where((_, i) => i % 2 == 1).All(t => t == 0))
        {
            return Encoding.Unicode.GetString(bytes).TrimEnd('\0');
        }

        var ansi = Ansi.GetString(bytes).TrimEnd('\0');
        return ansi.All(c => c >= 0x20 || c is '\r' or '\n' or '\t') ? ansi : BitConverter.ToString(bytes);
    }

    private static string Rot13(string s)
    {
        var c = s.ToCharArray();
        for (var i = 0; i < c.Length; i++)
        {
            if (c[i] is >= 'a' and <= 'z')
            {
                c[i] = (char)('a' + (c[i] - 'a' + 13) % 26);
            }
            else if (c[i] is >= 'A' and <= 'Z')
            {
                c[i] = (char)('A' + (c[i] - 'A' + 13) % 26);
            }
        }

        return new string(c);
    }

    /// <summary>2, 4 or 8 bytes as a big-endian unsigned number</summary>
    private static string BigEndian(byte[] b)
    {
        if (b.Length is not (2 or 4 or 8))
        {
            throw new ArgumentException("2, 4 or 8 bytes needed");
        }

        ulong v = 0;
        foreach (var x in b)
        {
            v = (v << 8) | x;
        }

        return v.ToString(CultureInfo.InvariantCulture);
    }

    private const string KeyDigits = "BCDFGHJKMPQRTVWXY2346789";

    /// <summary>DigitalProductId's product key, Windows 8 and later (as Registry Explorer's data interpreter decodes it)</summary>
    private static string ProductKeyWin8(byte[] digitalProductId)
    {
        var id = digitalProductId.Take(67).ToArray();
        const int keyOffset = 52;
        var isWin8 = (byte)((id[66] / 6) & 1);
        id[66] = (byte)((id[66] & 0xf7) | ((isWin8 & 2) * 4));

        var key = string.Empty;
        var last = 0;
        for (var i = 24; i >= 0; i--)
        {
            var current = 0;
            for (var j = 14; j >= 0; j--)
            {
                current = current * 256 + id[j + keyOffset];
                id[j + keyOffset] = (byte)(current / 24);
                current %= 24;
                last = current;
            }

            key = KeyDigits[current] + key;
        }

        key = key.Substring(1, last) + "N" + key.Substring(last + 1);
        for (var i = 5; i < key.Length; i += 6)
        {
            key = key.Insert(i, "-");
        }

        return key;
    }

    /// <summary>DigitalProductId's product key, Windows 7 and earlier (as Registry Explorer's data interpreter decodes it)</summary>
    private static string ProductKeyOld(byte[] digitalProductId)
    {
        var hexPid = digitalProductId.Skip(52).Take(15).ToArray();
        var decoded = new char[29];
        for (var i = 28; i >= 0; i--)
        {
            if ((i + 1) % 6 == 0)
            {
                decoded[i] = '-';
                continue;
            }

            var digitMapIndex = 0;
            for (var j = 14; j >= 0; j--)
            {
                var byteValue = (digitMapIndex << 8) | hexPid[j];
                hexPid[j] = (byte)(byteValue / 24);
                digitMapIndex = byteValue % 24;
                decoded[i] = KeyDigits[digitMapIndex];
            }
        }

        return new string(decoded);
    }
}
