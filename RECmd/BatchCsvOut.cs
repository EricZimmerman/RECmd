using System;

namespace RECmd;

public class BatchCsvOut
{
    public string HivePath { get; set; }
    public string HiveType { get; set; }
    public string Description { get; set; }
    public string Category { get; set; }
    public string KeyPath { get; set; }
    public string ValueName { get; set; }

    public string ValueType { get; set; }
    public string ValueData { get; set; }
    public string ValueData2 { get; set; }
    public string ValueData3 { get; set; }

    public string Comment { get; set; }
    public bool Recursive { get; set; }
    public bool Deleted { get; set; }


    public DateTimeOffset? LastWriteTimestamp { get; set; }

    public string PluginDetailFile { get; set; }
}