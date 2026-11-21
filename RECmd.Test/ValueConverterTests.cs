using System;
using System.Linq;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using RECmd;

namespace RECmd.Test;

/// <summary>Batch file BinaryConvert (issue #117); the expected values were worked out apart from the code (Python)</summary>
[TestFixture]
public class ValueConverterTests
{
    private const string Dt = "yyyy-MM-dd HH:mm:ss";
    private const string When = "2020-01-02 03:04:05";

    private static byte[] Hex(string hex) => Enumerable.Range(0, hex.Length / 2).Select(i => Convert.ToByte(hex.Substring(i * 2, 2), 16)).ToArray();

    private static string Run(Key.BinConvert kind, string hex, string valueType = "RegBinary", string text = null)
    {
        ValueConverter.TryConvert(kind, valueType, Hex(hex), text ?? hex, Dt, out var result).Should().BeTrue();
        return result;
    }

    [TestCase(Key.BinConvert.Filetime, "8000c44a19c1d501", "RegBinary")]
    [TestCase(Key.BinConvert.Filetime, "8000c44a19c1d501", "RegQword")]
    [TestCase(Key.BinConvert.Epoch, "a55d0d5e", "RegDword")]
    [TestCase(Key.BinConvert.EpochMilliseconds, "2ecf35646f010000", "RegQword")]
    [TestCase(Key.BinConvert.DateTimeTicks, "80003b6d308fd708", "RegBinary")]
    [TestCase(Key.BinConvert.WebKit, "4033adbab5f92e00", "RegQword")]
    [TestCase(Key.BinConvert.MacAbsolute, "00008092cadec141", "RegBinary")]
    [TestCase(Key.BinConvert.OLE, "02c93a170467e540", "RegBinary")]
    public void Times(Key.BinConvert kind, string hex, string valueType)
    {
        Run(kind, hex, valueType).Should().Be(When);
    }

    [TestCase(Key.BinConvert.DosTimeDate, "82182250", "RegBinary")]
    [TestCase(Key.BinConvert.DosDateTime, "22508218", "RegDword")]
    public void DosTimes(Key.BinConvert kind, string hex, string valueType)
    {
        //2 second resolution: 03:04:05 is kept as 03:04:04
        Run(kind, hex, valueType).Should().Be("2020-01-02 03:04:04");
    }

    [Test]
    public void SystemTime()
    {
        //year, month, day of week, day, hour, minute, second, milliseconds
        ValueConverter.TryConvert(Key.BinConvert.Systemtime, "RegBinary", Hex("e407010004000200030004000500a602"), "", "yyyy-MM-dd HH:mm:ss.fff", out var r).Should().BeTrue();
        r.Should().Be("2020-01-02 03:04:05.678");
    }

    [TestCase(Key.BinConvert.Epoch, "1577934245")]
    [TestCase(Key.BinConvert.EpochMilliseconds, "1577934245678")]
    [TestCase(Key.BinConvert.Filetime, "132224078450000000")]
    [TestCase(Key.BinConvert.Filetime, "0x1D5C1194AC40080")]
    [TestCase(Key.BinConvert.MacAbsolute, "599627045")]
    public void StringsHoldingNumbers(Key.BinConvert kind, string text)
    {
        //a time kept as text: it used to be read from the bytes of the text
        ValueConverter.TryConvert(kind, "RegSz", Encoding.Unicode.GetBytes(text + "\0"), text, Dt, out var r).Should().BeTrue();
        r.Should().Be(When);
    }

    [Test]
    public void Addresses()
    {
        Run(Key.BinConvert.Ip, "c0a80001").Should().Be("192.168.0.1");
        Run(Key.BinConvert.Ip, "c0a80001", "RegDword").Should().Be("192.168.0.1", "a DWORD (it was only converted for binary values)");
        Run(Key.BinConvert.Ipv6, "20010db8000000000000000000000001").Should().Be("2001:db8::1");
        Run(Key.BinConvert.MacAddress, "001122aabbcc").Should().Be("00:11:22:AA:BB:CC");
    }

    [Test]
    public void SidOnAnyOs()
    {
        Run(Key.BinConvert.Sid, "01050000000000051500000057040000ae080000050d0000e9030000").Should().Be("S-1-5-21-1111-2222-3333-1001");
        Run(Key.BinConvert.Sid, "010100000000000512000000").Should().Be("S-1-5-18");
        //an authority over 32 bits is hex, as SecurityIdentifier writes it
        Run(Key.BinConvert.Sid, "0100010000000000").Should().Be("S-1-0x010000000000");
    }

    [Test]
    public void Text()
    {
        Run(Key.BinConvert.Ascii, Convert.ToHexString(Encoding.ASCII.GetBytes("abc\0"))).Should().Be("abc");
        Run(Key.BinConvert.Unicode, Convert.ToHexString(Encoding.Unicode.GetBytes("Grüße\0"))).Should().Be("Grüße");
        Run(Key.BinConvert.ToBase64, Convert.ToHexString(Encoding.ASCII.GetBytes("plain text"))).Should().Be("cGxhaW4gdGV4dA==");

        ValueConverter.TryConvert(Key.BinConvert.FromBase64, "RegSz", Array.Empty<byte>(), "aABlAGwAbABvAA==", Dt, out var r).Should().BeTrue();
        r.Should().Be("hello", "UTF-16 inside");
        ValueConverter.TryConvert(Key.BinConvert.FromBase64, "RegSz", Array.Empty<byte>(), "cGxhaW4gdGV4dA==", Dt, out r).Should().BeTrue();
        r.Should().Be("plain text");
        ValueConverter.TryConvert(Key.BinConvert.FromBase64, "RegSz", Array.Empty<byte>(), "AAECAw==", Dt, out r).Should().BeTrue();
        r.Should().Be("00-01-02-03", "bytes that aren't text");

        ValueConverter.TryConvert(Key.BinConvert.Rot13, "RegSz", Array.Empty<byte>(), "HRZR_EHACNGU:P:\\Jvaqbjf\\flfgrz32\\pzq.rkr", Dt, out r).Should().BeTrue();
        r.Should().Be("UEME_RUNPATH:C:\\Windows\\system32\\cmd.exe");
    }

    [Test]
    public void GuidWithItsName()
    {
        var r = Run(Key.BinConvert.Guid, "04f48bf3431df242930567de0b28fc23");
        r.Should().StartWith("f38bf404-1d43-42f2-9305-67de0b28fc23 (").And.EndWith(")");
        Run(Key.BinConvert.Guid, "00112233445566778899aabbccddeeff").Should().Be("33221100-5544-7766-8899-aabbccddeeff", "an unknown GUID has no name");
    }

    [Test]
    public void BigEndian()
    {
        Run(Key.BinConvert.BigEndian, "0000012c", "RegDword").Should().Be("300");
        Run(Key.BinConvert.BigEndian, "012c").Should().Be("300");
        Run(Key.BinConvert.BigEndian, "000000000000012c").Should().Be("300");
    }

    [Test]
    public void ProductKeys()
    {
        //DigitalProductId with the key bytes of a known key at offset 52 (encoded the other way in the test, so the
        //two decoders are checked against an encoder, not against themselves)
        var id = new byte[164];
        Encode("BBBBBBBBBBBBBBBBBBBBBBBBB", id, false);
        Run(Key.BinConvert.ProductKeyOld, Convert.ToHexString(id)).Should().Be("BBBBB-BBBBB-BBBBB-BBBBB-BBBBB");

        id = new byte[164];
        //(the Windows 7 alphabet has no N)
        Encode("YKHFT-KW986-GK4PY-FDWYH-7TP9F", id, false);
        Run(Key.BinConvert.ProductKeyOld, Convert.ToHexString(id)).Should().Be("YKHFT-KW986-GK4PY-FDWYH-7TP9F");
    }

    /// <summary>The Windows 7 encoding: 25 base-24 digits into 15 little-endian bytes at offset 52</summary>
    private static void Encode(string key, byte[] id, bool win8)
    {
        const string digits = "BCDFGHJKMPQRTVWXY2346789";
        var chars = key.Replace("-", "");
        var number = System.Numerics.BigInteger.Zero;
        foreach (var c in chars)
        {
            number = number * 24 + digits.IndexOf(c);
        }

        var bytes = number.ToByteArray();
        Array.Copy(bytes, 0, id, 52, Math.Min(15, bytes.Length));
    }

    [Test]
    public void ProductKeyWindows8()
    {
        //Microsoft's generic Windows 10 Pro key round-trips through the Windows 8 encoding (the 'N' position in the top byte)
        var id = new byte[1272];
        EncodeWin8("VK7JG-NPHTM-C97JM-9MPGT-3V66T", id);
        Run(Key.BinConvert.ProductKey, Convert.ToHexString(id)).Should().Be("VK7JG-NPHTM-C97JM-9MPGT-3V66T");
    }

    /// <summary>The Windows 8 and later encoding: the N's position goes first as a digit, the N is left out, flag 0x08 in byte 66</summary>
    private static void EncodeWin8(string key, byte[] id)
    {
        const string digits = "BCDFGHJKMPQRTVWXY2346789";
        var chars = key.Replace("-", "");
        var n = chars.IndexOf('N');
        var rest = chars.Remove(n, 1);
        var all = digits[n] + rest;
        var number = System.Numerics.BigInteger.Zero;
        foreach (var c in all)
        {
            number = number * 24 + digits.IndexOf(c);
        }

        var bytes = number.ToByteArray();
        Array.Copy(bytes, 0, id, 52, Math.Min(15, bytes.Length));
        id[66] = (byte)((id[66] & 0xF7) | 0x08);
    }

    [TestCase(Key.BinConvert.Filetime, "0102")]
    [TestCase(Key.BinConvert.Systemtime, "00000000000000000000000000000000")]
    [TestCase(Key.BinConvert.Sid, "0201000000000005")]
    [TestCase(Key.BinConvert.Guid, "0011")]
    [TestCase(Key.BinConvert.BigEndian, "010203")]
    [TestCase(Key.BinConvert.MacAbsolute, "000000000000f87f")]
    [TestCase(Key.BinConvert.ProductKey, "00")]
    public void WhatDoesNotFitIsLeftAsItIs(Key.BinConvert kind, string hex)
    {
        ValueConverter.TryConvert(kind, "RegBinary", Hex(hex), hex, Dt, out var r).Should().BeFalse();
        r.Should().BeNull();
        ValueConverter.TryConvert(Key.BinConvert.Epoch, "RegSz", Encoding.Unicode.GetBytes("soon"), "soon", Dt, out _).Should().BeFalse();
    }

    [Test]
    public void RandomDataNeverThrows()
    {
        var rnd = new Random(117);
        var kinds = Enum.GetValues(typeof(Key.BinConvert)).Cast<Key.BinConvert>().ToArray();
        for (var i = 0; i < 20_000; i++)
        {
            var b = new byte[rnd.Next(0, 200)];
            rnd.NextBytes(b);
            var kind = kinds[rnd.Next(kinds.Length)];
            var type = new[] { "RegBinary", "RegSz", "RegDword", "RegQword", "RegExpandSz" }[rnd.Next(5)];
            var act = () => ValueConverter.TryConvert(kind, type, b, Encoding.Unicode.GetString(b, 0, b.Length & ~1), Dt, out _);
            act.Should().NotThrow();
        }
    }
}
