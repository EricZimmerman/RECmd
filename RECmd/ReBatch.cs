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
        [Description("Binary to SID")] Sid = 4
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
        [Description("BCD")] Bcd = 8,
        [Description("DRIVERS")] Drivers = 8,
        [Description("AMCACHE")] Amcache = 9,
        [Description("SYSCACHE")] Syscache = 10
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