using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Minerva.Localizations.Utilities
{
    public interface IRow : IEnumerable<string>
    {
        string this[string col] { get; set; }
        string Name { get; }
        int Count { get; }

        public static IRow Of(string name, Dictionary<string, string> value)
        {
            return new Row(name, value);
        }

        private readonly struct Row : IRow
        {
            private readonly string key;
            private readonly Dictionary<string, string> value;

            public Row(string key, Dictionary<string, string> value)
            {
                this.key = key;
                this.value = value;
            }

            public string this[string col] { get => value[col]; set => this.value[col] = value; }
            public string Name => key;
            public int Count => value.Count;

            public IEnumerator<string> GetEnumerator()
            {
                return value.Values.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return value.Values.GetEnumerator();
            }
        }
    }

    public interface ITable : IEnumerable, IEnumerable<IRow>
    {
        string this[string row, string col] { get; set; }
        IRow this[string row] { get; }
        int Count { get; }
        string[] ColumnNames { get; }
        string[] RowNames { get; }

        IRow GetOrCreateRow(string rowName);
    }

    public class CSVFile : ITable
    {
        public Dictionary<string, Dictionary<string, string>> table;
        public string[] cols;
        public string[] rows;

        public CSVFile()
        {
            table = new();
            cols = Array.Empty<string>();
            rows = Array.Empty<string>();
        }

        public IRow this[string row] => IRow.Of(row, table[row]);
        public string this[string row, string col] { get => table[row][col]; set => table[row][col] = value; }
        public int Count => table.Count;
        public string[] ColumnNames => cols;
        public string[] RowNames => rows;

        public IRow GetOrCreateRow(string rowName)
        {
            if (table.TryGetValue(rowName, out var value))
            {
                return IRow.Of(rowName, value);
            }

            Array.Resize(ref rows, rows.Length + 1);
            rows[^1] = rowName;
            value = new Dictionary<string, string>();
            table.Add(rowName, value);
            return IRow.Of(rowName, value);
        }

        public IEnumerator<IRow> GetEnumerator()
        {
            foreach (var item in table)
            {
                yield return IRow.Of(item.Key, item.Value);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    /// <summary>
    /// CSV reader/writer used by localization import and export.
    /// </summary>
    public static class CSV
    {
        private const char CSV_SEPARATOR = ',';

        public static string ConvertToCSV(string name, ITable table)
        {
            StringBuilder sb = new(name);
            sb.Append(CSV_SEPARATOR);
            sb.Append(string.Join(CSV_SEPARATOR, table.ColumnNames));
            sb.Append('\n');

            foreach (var item in table)
            {
                sb.Append(item.Name);
                sb.Append(CSV_SEPARATOR);
                sb.AppendJoin(CSV_SEPARATOR, table.ColumnNames.Select(r => item[r]).Select(
                    text => text.Contains('"') || text.Contains(',')
                        ? $"\"{ToProperyString(text)}\""
                        : text));
                sb.Append('\n');
            }

            return sb.ToString();
        }

        private static string ToProperyString(string text)
        {
            StringBuilder builder = new();
            foreach (char c in text)
            {
                switch (c)
                {
                    case '\r':
                        continue;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '"':
                        builder.Append('"', 2);
                        break;
                    default:
                        builder.Append(c);
                        break;
                }
            }
            return builder.ToString();
        }

        public static CSVFile Import(string path)
        {
            string input = File.ReadAllText(path);

            var file = new CSVFile();
            var entries = new Queue<string>(input.Split('\n'));

            file.cols = entries.Dequeue().Split(CSV_SEPARATOR)[1..].ToArray();
            var rows = new List<string>();

            while (entries.Count != 0)
            {
                var entry = entries.Dequeue();
                if (string.IsNullOrWhiteSpace(entry)) continue;
                List<string> words = GetWords(entry);
                while (file.cols.Length > words.Count - 1)
                {
                    words.Add(string.Empty);
                }
                string row = words[0];
                rows.Add(row);

                words.RemoveAt(0);
                var dict = new Dictionary<string, string>();
                for (int i = 0; i < file.cols.Length; i++)
                {
                    dict.Add(file.cols[i], words[i]);
                }
                file.table.Add(row, dict);
            }
            file.rows = rows.ToArray();
            return file;
        }

        private static List<string> GetWords(string entry)
        {
            List<string> words = new();
            StringBuilder stringBuilder = new();
            bool isInQuote = false;
            for (int i = 0; i < entry.Length; i++)
            {
                char c = entry[i];
                switch (c)
                {
                    case ',':
                        if (isInQuote)
                        {
                            stringBuilder.Append(c);
                        }
                        else
                        {
                            words.Add(stringBuilder.ToString());
                            stringBuilder.Clear();
                        }
                        break;
                    case '"':
                        if (!isInQuote && stringBuilder.Length == 0)
                        {
                            isInQuote = true;
                            continue;
                        }
                        if (i + 1 < entry.Length && entry[i + 1] == '"')
                        {
                            stringBuilder.Append(c);
                            i++;
                        }
                        else
                        {
                            isInQuote = false;
                        }
                        break;
                    default:
                        stringBuilder.Append(c);
                        break;
                }
            }
            words.Add(stringBuilder.ToString());
            return words;
        }
    }
}
