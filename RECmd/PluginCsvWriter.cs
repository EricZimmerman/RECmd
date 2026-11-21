using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using CsvHelper;

namespace RECmd;

/// <summary>
///     Writes a plugin's rows (any class from a RegistryPlugin.* assembly, which has no CsvHelper attributes and whose
///     properties are mostly get-only) with the columns and values CsvHelper 33's AutoMap of the row type gave them:
///     <list type="bullet">
///         <item>the public properties with a getter, in reflection order, except collections and BatchValueData*;</item>
///         <item>BatchKeyPath* moved to index 0 and BatchValueName* to index 1, then sorted as 33 sorted them (List.Sort);</item>
///         <item>properties whose type is another class (none in the current plugins) as that class's columns, after the others;</item>
///         <item>dates in <see cref="Program.dtf" /> (current culture), True/False, enums by name, byte arrays as 0x and hex,
///         other values in their default format with the invariant culture, null as an empty field.</item>
///     </list>
///     A source generated record type can't be used: the row types are only known at run time.
/// </summary>
internal sealed class PluginCsvWriter : CsvRecordWriter<object>
{
    private readonly List<Column> _columns;

    public PluginCsvWriter(Type rowType)
    {
        _columns = Columns(rowType, null, 0);
    }

    public override int FieldCount => _columns.Count;

    public override void WriteHeader(CsvWriter writer)
    {
        foreach (var column in _columns)
        {
            WriteColumnName(writer, column.Name);
        }
    }

    public override void Write(CsvWriter writer, object record)
    {
        foreach (var column in _columns)
        {
            writer.WriteField(Format(column.Value(record)));
        }
    }

    /// <summary>The text CsvHelper 33 wrote for a value</summary>
    private static string Format(object value)
    {
        return value switch
        {
            null => null,
            string text => text,
            //RECmd's NullConverter: the current culture
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString(Program.dtf),
            DateTime dateTime => dateTime.ToString(Program.dtf),
            bool flag => flag ? "True" : "False",
            Enum named => named.ToString(),
            byte[] bytes => "0x" + BitConverter.ToString(bytes).Replace("-", ""),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()
        };
    }

    private static List<Column> Columns(Type type, Func<object, object> owner, int depth)
    {
        var members = new List<(int Index, Column Column)>();
        var references = new List<Column>();
        var index = 0;

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetGetMethod() == null || property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            if (propertyType == typeof(object) ||
                (propertyType != typeof(string) && propertyType != typeof(byte[]) &&
                 typeof(IEnumerable).IsAssignableFrom(propertyType)))
            {
                //33 mapped neither collections nor object members
                continue;
            }

            Func<object, object> value = owner == null
                ? property.GetValue
                : record => owner(record) is { } parent ? property.GetValue(parent) : null;

            if (propertyType.IsClass && propertyType != typeof(string) && propertyType != typeof(byte[]) &&
                propertyType != typeof(Uri))
            {
                //a reference map in 33: the class's own columns, after the members
                if (depth < 8)
                {
                    references.AddRange(Columns(propertyType, value, depth + 1));
                }

                index += 1;
                continue;
            }

            var columnIndex = index;
            index += 1;

            if (owner == null)
            {
                if (property.Name.StartsWith("BatchValueData"))
                {
                    //ignored, but still in 33's sorted member list
                    members.Add((columnIndex, null));
                    continue;
                }

                if (property.Name.StartsWith("BatchKeyPath"))
                {
                    columnIndex = 0;
                }
                else if (property.Name.StartsWith("BatchValueName"))
                {
                    columnIndex = 1;
                }
            }

            members.Add((columnIndex, new Column(property.Name, value)));
        }

        //33 sorted its member maps by index with List.Sort, which is not stable: with up to 16 members a moved column
        //goes after the one already at its index, with more the order of equal indexes is the sort's. The same sort
        //over the same list (the ignored members too) gives the same order
        members.Sort((x, y) => x.Index.CompareTo(y.Index));
        return members.Where(t => t.Column != null).Select(t => t.Column).Concat(references).ToList();
    }

    private sealed class Column
    {
        public Column(string name, Func<object, object> value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }
        public Func<object, object> Value { get; }
    }
}
