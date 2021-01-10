using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using static Fae.Runtime.RuntimeObject;

namespace Fae.Runtime
{
    public class LispReader
    {
        internal LineNumberingReader Rdr;
        private string _fileName;

        public LispReader(LineNumberingReader rdr, string fileName = "<unknown>")
        {
            Rdr = rdr;
            _fileName = fileName;
        }

        public object ReadOne(bool eofIsError = false)
        {
            EatWhitespace();
            var lineStart = Rdr.Line;
            var columnStart = Rdr.Column;
            var o = _readOne(eofIsError);
            return RT.With(o, KW.ReaderFile, _fileName, KW.ReaderLine, lineStart, KW.ReaderColumn, columnStart);
        }

        public object _readOne(bool eofIsError = false)
        {
            var ch = Rdr.ReadChar();
            if (!ch.HasValue)
            {
                if (eofIsError)
                {
                    throw new EndOfStreamException();
                }

                return KW.EOS;

            }


            if (_readers.TryGetValue(ch.Value, out var reader))
                return reader.Read(this, ch.Value);

            var sb = new StringBuilder();
            sb.Append(ch);
            while (true)
            {
                ch = Rdr.PeekChar();
                if (!ch.HasValue || _whitespace.Contains(ch.Value) || _readers.ContainsKey(ch.Value))
                    break;
                sb.Append(ch);
                Rdr.ReadChar();
            }

            return InterpretSymbol(sb.ToString());

        }

        private object InterpretSymbol([NotNull] string s)
        {
            if (int.TryParse(s, out var p))
                return p;

            if (s!.StartsWith(":"))
                return Keyword.Intern(s.Substring(1));

            throw new NotImplementedException($"No way to interpret: {s}");
        }

        public Dictionary<char, IReader> _readers = new()
        {
            {'(', new ListReader()},
            {')', new UnmatchedReader('(')}
        };

    private static HashSet<char> _whitespace = " \t\r\n,".ToHashSet();

        internal void EatWhitespace()
        {
            var ch = Rdr.PeekChar();
            while (ch.HasValue && _whitespace.Contains(ch.Value))
            {
                Rdr.ReadChar();
                ch = Rdr.PeekChar();
            }
        }
        
    }

    public interface IReader
    {
        public object Read(LispReader rdr, char start);
    }

    internal class ListReader : IReader
    {
        public object Read(LispReader rdr, char start)
        {
            var vals = new List<object>();
            while (true)
            {
                rdr.EatWhitespace();

                var pk = rdr.Rdr.PeekChar();
                if (!pk.HasValue)
                    throw new Exception("Found End of file before end of list");

                if (pk.Value == ')')
                {
                    rdr.Rdr.Read();
                    return Utils.MakeList(vals);
                }

                vals.Add(rdr.ReadOne());
            }

        }
    }
    
    internal class UnmatchedReader : IReader
    {
        private char _other;

        public UnmatchedReader(char other)
        {
            _other = other;
        }
        public object Read(LispReader rdr, char start)
        {
            throw new Exception($"Found {start} without matching {_other}");

        }
    }
}