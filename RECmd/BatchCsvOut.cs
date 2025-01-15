using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace RECmd
{
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

        // Method to write the CSV
        public static void WriteCsv(string filePath, List<BatchCsvOut> data)
        {
            string dateTimeFormat = "yyyy-MM-dd HH:mm:ss.fffffff";
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                // Write CSV header
                writer.WriteLine("HivePath,HiveType,Description,Category,KeyPath,ValueName,ValueType,ValueData,ValueData2,ValueData3,Comment,Recursive,Deleted,LastWriteTimestamp,PluginDetailFile");

                // Write CSV rows
                foreach (var item in data)
                {
                    string formattedTimestamp = item.LastWriteTimestamp?.ToString(dateTimeFormat, CultureInfo.InvariantCulture) ?? string.Empty;
                    writer.WriteLine($"{item.HivePath},{item.HiveType},{item.Description},{item.Category},{item.KeyPath},{item.ValueName},{item.ValueType},{item.ValueData},{item.ValueData2},{item.ValueData3},{item.Comment},{item.Recursive},{item.Deleted},{formattedTimestamp},{item.PluginDetailFile}");
                }
            }
        }
    }
}
