using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RECmd
{
 internal   class SimpleKey
    {
        public string KeyPath { get; set; }
        public string KeyName { get; set; }
        public DateTimeOffset LastWriteTimestamp { get; set; }
        public List<SimpleKey>SubKeys { get;set; }
        public List<SimpleValue>Values { get;set; }

        public SimpleKey()
        {
            SubKeys = new List<SimpleKey>();
            Values = new List<SimpleValue>();
        }
    }

    internal class SimpleValue
    {
        public string ValueName { get; set; }
        public string ValueType { get; set; }
        public string ValueData { get; set; }
        public byte[] DataRaw { get; set; }
        public byte[] Slack { get; set; }
    }
}
