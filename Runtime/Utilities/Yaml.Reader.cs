using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Minerva.Localizations
{
    public static partial class Yaml
    {
        public class Reader
        {
            private const char SyntaxListElement = '-';
            private const char SyntaxEscape = '\\';
            private const char SyntaxDoubleQuote = '"';
            private const char SyntaxSingleQuote = '\'';
            public static readonly string ObjectSelf = "$self";


            class KeyStack
            {
                List<(int indent, string identifier, int? index)> values = new();

                public int Count => values.Count;

                public void Add(int indent, string identifier)
                {
                    values.Add((indent, identifier, null));
                }

                public void Traceback(int indent)
                {
                    for (int i = 0; i < values.Count; i++)
                    {
                        // too large
                        if (values[i].indent >= indent)
                        {
                            TracebackAt(i);
                            break;
                        }
                    }
                }

                private void TracebackAt(int i)
                {
                    values.RemoveRange(i, values.Count - i);
                }

                public string Peek()
                {
                    return values[^1].identifier;
                }

                public void Pop()
                {
                    values.RemoveAt(Count - 1);
                }

                public void NextElement()
                {
                    var entry = values[^1];
                    if (entry.index.HasValue)
                    {
                        entry.index++;
                    }
                    else entry.index = 0;
                    values[^1] = entry;
                }

                public override string ToString()
                {
                    if (values[^1].identifier == ObjectSelf)
                    {
                        return string.Join('.', values.Take(values.Count - 1).Select(c => c.index.HasValue ? $"{c.identifier}.{c.index.Value}" : c.identifier));
                    }
                    return string.Join('.', values.Select(c => c.index.HasValue ? $"{c.identifier}.{c.index.Value}" : c.identifier));
                }

                public void Clear()
                {
                    values.Clear();
                }
            }


            /// <summary>
            /// Debug, line count
            /// </summary>
            int line;
            /// <summary>
            /// Debug, char count of the line
            /// </summary>
            int charCount;

            int cursor;
            int currentIndentation;
            string content;
            KeyStack keyStack;

            public string CurrentKey => keyStack.ToString();
            public int Cursor { get => cursor; set => cursor = value; }
            public string String { get => content; set => content = value; }


            public Reader(string content)
            {
                this.cursor = 0;
                this.line = 1;
                this.String = content;
                this.keyStack = new();
            }

            public bool CanRead(int length) => Cursor + length <= String.Length;
            public bool CanRead() => CanRead(1);

            public char Next()
            {
                return String[Cursor++];
            }

            public char Peek()
            {
                return String[Cursor];
            }

            public char Peek(int offset)
            {
                return String[Cursor + offset];
            }


            public void Skip() { Cursor++; }
            public void SkipLine()
            {
                while (CanRead() && Next() != '\n') { }
                line++;
                charCount = cursor;
            }

            public int PeekIndentation()
            {
                int selfCursor = 0;
                // new lline
                while (selfCursor < String.Length && Peek(selfCursor++) != '\n') { }
                if (selfCursor >= String.Length)
                {
                    return 0;
                }
                int spaceCount = 0;
                while (selfCursor < String.Length && Peek(selfCursor + spaceCount) == ' ') { spaceCount++; }
                return spaceCount;
            }

            public int PeekLineEndNonWhitespace()
            {
                int selfCursor = 0;
                // new lline
                while (Cursor + selfCursor < String.Length && Peek(selfCursor) != '\n') { selfCursor++; }
                do
                {
                    selfCursor--;
                }
                while (Peek(selfCursor) == ' ');
                return Cursor + selfCursor;
            }

            /// <summary>
            /// Skipping comment, but not the indentation whitespace
            /// </summary>
            void SkipComments()
            {
                while (CanRead())
                {
                    int start = Cursor;
                    SkipWhitespace();
                    if (CanRead() && (Peek() == '\n' || Peek() == '#'))
                    {
                        // skip comment line
                        SkipLine();
                        continue;

                    }
                    else
                    {
                        Cursor = start;
                        break;
                    }
                }
            }

            int ReadIndentation(bool isMultiline = false)
            {
                SkipComments();
                int start = Cursor;
                while (CanRead() && Peek() == ' ') { Next(); }
                int length = Cursor - start;
                // treat list element with more index
                if (CanRead(2) && !isMultiline && Peek() == SyntaxListElement && char.IsWhiteSpace(Peek(1))) return length + 2;
                return length;
            }

            string ReadKey()
            {
                if (!CanRead())
                {
                    throw Throw();
                }

                char c = Peek();
                // list element
                while (c == SyntaxListElement && CanRead(2) && char.IsWhiteSpace(Peek(1)))
                {
                    Next();
                    keyStack.NextElement();
                    SkipComments();
                    SkipWhitespace();
                    c = Peek();
                }
                // quoted key
                if (c == SyntaxDoubleQuote || c == SyntaxSingleQuote)
                {
                    return ReadQuotedKey();
                }

                int start = Cursor;
                int lastNonspace = start;
                while (CanRead())
                {
                    char next = Next();
                    if (next == '\n') throw Throw();
                    if (next == ':')
                    {
                        return String[start..lastNonspace];
                    }
                    if (next != ' ')
                    {
                        lastNonspace = Cursor;
                    }
                }
                // key should end with : anyway
                throw Throw();
            }

            string ReadQuotedKey()
            {
                var key = ReadQuotedString();
                while (CanRead() && Peek() == ' ') { Skip(); }
                if (CanRead() && Peek() == ':')
                {
                    Skip();
                    return key;
                }
                // key should end with : anyway
                throw Throw();
            }

            string ReadValue()
            {
                // skip space between value and key
                SkipWhitespace();
                // object value, no string value yet
                if (Peek() == '\n')
                {
                    SkipLine();
                    return string.Empty;
                }
                string value;
                // multiline, keep line break
                if (Peek() == '|')
                {
                    SkipLine();
                    value = ReadValueMultiline(true);
                }
                // multiline, keep line break
                else if (Peek() == '>')
                {
                    SkipLine();
                    value = ReadValueMultiline(false);
                }
                else if (!IsAllowedInYamlRawStringHead(Peek()))
                {
                    // value cannot start with SOME SYMBOLS
                    throw Throw();
                }
                else
                {
                    value = ReadString();
                    SkipLine();
                }

                return value;
            }

            private string ReadValueMultiline(bool linebreak)
            {
                var blockIndentation = ReadIndentation();
                // an empty multiline
                if (blockIndentation <= currentIndentation)
                {
                    // skip \n to next line
                    SkipLine();
                    return string.Empty;
                }
                StringBuilder sb = new StringBuilder();
                int nextIndentation;
                do
                {
                    int lineStart = Cursor;
                    while (CanRead() && Peek() != '\n') Next();
                    sb.Append(String, lineStart, Cursor - lineStart);
                    if (linebreak) sb.AppendLine();
                    else sb.Append(' ');
                    // skip \n to next line
                    SkipLine();
                }
                while ((nextIndentation = ReadIndentation(true)) == blockIndentation);
                Cursor -= nextIndentation;
                // remove last \n
                sb.Length--;
                return sb.ToString();
            }

            /// <summary>
            /// Try read next key value pair
            /// </summary>
            /// <param name="keyValuePair"></param>
            /// <returns></returns>
            public bool TryRead(out KeyValuePair<string, string> keyValuePair)
            {
                keyValuePair = default;
                try
                {
                    keyValuePair = Read();
                    return true;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            }

            /// <summary>
            /// Read next key value
            /// </summary>
            /// <returns></returns>
            /// <exception cref="InvalidOperationException"></exception>
            public KeyValuePair<string, string> Read()
            {
                SkipComments();
                int indentation = ReadIndentation();
                // child class
                if (indentation <= currentIndentation)
                {
                    keyStack.Traceback(indentation);
                }
                currentIndentation = indentation;

                SkipComments();
                SkipWhitespace();
                if (!CanRead())
                {
                    throw new InvalidOperationException("Empty Yaml");
                }
                var identifier = ReadKey();
                keyStack.Add(indentation, identifier);
                // trailing key, treated as empty object, then no more value
                if (!CanRead())
                {
                    throw new InvalidOperationException("Empty Yaml");
                }
                // read object, line end with :
                if (String[PeekLineEndNonWhitespace()] == ':')
                {
                    SkipLine();
                    SkipComments();
                    if (!CanRead())
                    {
                        throw new InvalidOperationException("Empty Yaml");
                    }
                    return Read();
                }
                // read string value
                var value = ReadValue();
                var key = CurrentKey;
                keyStack.Pop();
                return new(key, value);
            }

            private static bool IsQuotedStringStart(char c)
            {
                return c == SyntaxDoubleQuote || c == SyntaxSingleQuote;
            }

            public void SkipWhitespace()
            {
                while (CanRead() && char.IsWhiteSpace(Peek()))
                {
                    Skip();
                }
            }

            public static bool IsAllowedInUnquotedString(char c)
            {
                return c != ':' && c != '#' && c != '\n';
            }

            public static bool IsAllowedInYamlRawStringHead(char c)
            {
                return c != '-'
                    && c != ':'
                    && c != '{'
                    && c != '}'
                    && c != '['
                    && c != ']'
                    && c != '@'
                    && c != '%'
                    && c != '>'
                    && c != '|'
                    && c != '!'
                    && c != '*'
                    && c != '&'
                    && c != '#';
            }

            public ReadOnlySpan<char> ReadUnquotedString()
            {
                var start = Cursor;
                while (CanRead() && IsAllowedInUnquotedString(Peek()))
                {
                    Skip();
                }

                return String.AsSpan(start, Cursor - start);
            }

            /// <exception cref="CommandSyntaxException" />
            public string ReadQuotedString()
            {
                if (!CanRead())
                {
                    return "";
                }
                var next = Peek();
                if (!IsQuotedStringStart(next))
                {
                    throw Throw();
                }

                Skip();
                return ReadStringUntil(next);
            }

            private string ReadStringUntil(char terminator)
            {
                var result = new StringBuilder();
                var escaped = false;
                while (CanRead())
                {
                    var c = Next();
                    if (escaped)
                    {
                        if (c == terminator || c == SyntaxEscape)
                        {
                            result.Append(c);
                            escaped = false;
                        }
                        else if (c == 'n')
                        {
                            result.Append('\n');
                            escaped = false;
                        }
                        else if (c == SyntaxEscape)
                        {
                            result.Append(SyntaxEscape);
                            escaped = false;
                        }
                        else
                        {
                            Cursor--;
                            throw Throw();
                        }
                    }
                    else if (c == SyntaxEscape)
                    {
                        escaped = true;
                    }
                    else if (c == terminator)
                    {
                        return result.ToString();
                    }
                    else
                    {
                        result.Append(c);
                    }
                }

                throw Throw();
            }

            public string ReadString()
            {
                if (!CanRead())
                {
                    return "";
                }
                var next = Peek();
                if (IsQuotedStringStart(next))
                {
                    Skip();
                    return ReadStringUntil(next);
                }
                return new string(ReadUnquotedString());
            }

            public bool Expect(char c)
            {
                if (!CanRead() || Peek() != c)
                {
                    return false;
                }

                Skip();
                return true;
            }

            InvalidDataException Throw()
            {
                string context = GetContext();
                return new InvalidDataException($"invalid data format at line {line}, char {cursor - charCount} ..{context}");

            }

            private string GetContext()
            {
                var ending = cursor + 10 < String.Length ? cursor + 10 : String.Length;
                string context = content[cursor..ending];
                context = ToProperString(context);
                return context;
            }

            public void Reset()
            {
                this.keyStack.Clear();
                this.line = 0;
                this.charCount = 0;
                this.cursor = 0;
                this.currentIndentation = 0;
            }

            public IEnumerable<KeyValuePair<string, string>> ReadAll()
            {
                return new Enumerator(this);
            }

            struct Enumerator : IEnumerable<KeyValuePair<string, string>>, IEnumerator<KeyValuePair<string, string>>
            {
                private Reader reader;
                private KeyValuePair<string, string> current;

                public Enumerator(Reader reader)
                {
                    this.reader = reader;
                    current = default;
                }

                public KeyValuePair<string, string> Current => current;

                object IEnumerator.Current => this.Current;

                public void Dispose() { }

                public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
                {
                    return this;
                }

                public bool MoveNext()
                {
                    try
                    {
                        var (key, value) = reader.Read();
                        current = new KeyValuePair<string, string>(key, value);
                        return true;
                    }
                    catch (InvalidOperationException)
                    {
                        return false;
                    }
                }

                public void Reset()
                {
                    reader.Reset();
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    throw new NotImplementedException();
                }
            }
        }
    }

}