using System.Collections.Generic;
using System.ComponentModel;
using FluentValidation;

namespace RECmd;

public class ReBatch
{
    public ReBatch()
    {
        Keys = new List<Key>();
    }

    public string Description { get; set; }
    public string Author { get; set; }
    public string Version { get; set; }
    public string Id { get; set; }

    public List<Key> Keys { get; set; }
}

public class Key
{
    public enum BinConvert
    {
        [Description("None")] None = 0,

        [Description("64 bit Windows FILETIME")]
        Filetime = 1,
        [Description("IPv4 address")] Ip = 2,
        [Description("DWord to Epoch")] Epoch = 3,
        [Description("Binary to SID")] Sid = 4,
        [Description("128 bit Windows SYSTEMTIME")] Systemtime = 5,
        [Description("DateTime.Ticks")] DateTimeTicks = 6,
        [Description("OLE Automation Date OLE2.0")] OLE = 7,

        //2026.11 (issue #117), as Registry Explorer's data interpreter has them; they work on any value type
        [Description("Unix time in milliseconds")] EpochMilliseconds = 8,
        [Description("Text, one byte per character (Windows-1252)")] Ascii = 9,
        [Description("Text, UTF-16")] Unicode = 10,
        [Description("Bytes as Base64")] ToBase64 = 11,
        [Description("Base64 text decoded")] FromBase64 = 12,
        [Description("GUID, with the folder or item it names")] Guid = 13,
        [Description("MS-DOS time then date (as Internet Explorer stores them)")] DosTimeDate = 14,
        [Description("MS-DOS date then time (FAT)")] DosDateTime = 15,
        [Description("IPv6 address")] Ipv6 = 16,
        [Description("MAC address")] MacAddress = 17,
        [Description("ROT13 text (UserAssist names)")] Rot13 = 18,
        [Description("WebKit/Chrome time (microseconds since 1601)")] WebKit = 19,
        [Description("Mac absolute time (seconds since 2001, a double)")] MacAbsolute = 20,
        [Description("Big-endian number (2, 4 or 8 bytes)")] BigEndian = 21,
        [Description("Product key from DigitalProductId (Windows 8 and later)")] ProductKey = 22,
        [Description("Product key from DigitalProductId (Windows 7 and earlier)")] ProductKeyOld = 23
    }

    public enum HiveType_
    {
        [Description("Other")] Other = 0,
        [Description("NTUSER")] NtUser = 1,
        [Description("SAM")] Sam = 2,
        [Description("SECURITY")] Security = 3,
        [Description("SOFTWARE")] Software = 4,
        [Description("SYSTEM")] System = 5,
        [Description("USRCLASS")] UsrClass = 6,
        [Description("COMPONENTS")] Components = 7,
        [Description("DRIVERS")] Drivers = 8,
        [Description("AMCACHE")] Amcache = 9,
        [Description("SYSCACHE")] Syscache = 10,
        [Description("BCD")] Bcd = 11,
        [Description("BCD-Template")] BcdTemplate = 12,
        [Description("ELAM")] Elam = 13,
        [Description("userdiff")] userdiff = 14,
        [Description("BBI")] Bbi = 15,
        [Description("VSMIDK")] Vsmidk = 16,
        [Description("DEFAULT")] Default = 17,
        [Description("User")] User = 18,
        [Description("UserClasses")] UserClasses = 19,
        [Description("settings")] settings = 20,
        [Description("Registry")] Registry = 21
    }

    public string Description { get; set; }

    public HiveType_ HiveType { get; set; }
    public string Category { get; set; }
    public string KeyPath { get; set; }
    public string ValueName { get; set; }

    public bool Recursive { get; set; }
    public bool DisablePlugin { get; set; }
    public bool IncludeBinary { get; set; }
    public BinConvert BinaryConvert { get; set; }

    public string Comment { get; set; }
}

internal class ReBatchValidator : AbstractValidator<ReBatch>
{
    public ReBatchValidator()
    {
        RuleFor(target => target.Description).NotNull();
        RuleFor(target => target.Author).NotNull();
        RuleFor(target => target.Version).NotNull();

        RuleFor(target => target.Id).NotNull();
        RuleFor(target => target.Keys).NotNull();
        RuleFor(target => target.Keys.Count).GreaterThan(0).When(t => t.Keys != null);

        RuleForEach(target => target.Keys).NotNull().WithMessage("Keys cannot be null")
            .SetValidator(new KeyValidator());
    }
}

internal class KeyValidator : AbstractValidator<Key>
{
    public KeyValidator()
    {
        RuleFor(target => target.Description).NotNull();
        RuleFor(target => target.HiveType).NotNull();

        RuleFor(target => target.Category).NotNull();
        RuleFor(target => target.KeyPath).NotNull();
        RuleFor(target => target.Recursive).NotNull();
    }
}