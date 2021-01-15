using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using Microsoft.VisualBasic.CompilerServices;

namespace Wyld
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

        public object? ReadOne(bool eofIsError = false)
        {
            EatWhitespace();
            var o = _readOne(eofIsError);
            return o;
        }

        public object? _readOne(bool eofIsError = false)
        {
            var ch = Rdr.ReadChar();
            if (!ch.HasValue)
            {
                if (eofIsError)
                {
                    throw new EndOfStreamException();
                }

                return null;

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

            return Symbol.Parse(s);
        }

        public Dictionary<char, IReader> _readers = new()
        {
            {'(', new ListReader()},
            {')', new UnmatchedReader('(')},
            {'[', new VectorStructReader()},
            {']', new UnmatchedReader('[')},
            {'^', new TypeReader()}
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
        public object? Read(LispReader rdr, char start);
    }

    internal class TypeReader : IReader
    {
        public object? Read(LispReader rdr, char start)
        {
            var type = rdr.ReadOne(true);
            var obj = rdr.ReadOne(true);
            if (obj is IMeta mobj && type is Symbol s)
            {
                var meta = mobj.Meta.Add(KW.Type, s);
                return mobj.WithMeta(meta);
            }

            throw new Exception($"Expected meta object, got {obj!.GetType()} expected symbol, got {type!.GetType()}");
        }
    }

    internal class ListReader : IReader
    {
        public object? Read(LispReader rdr, char start)
        {
            var vals = new List<object>();
            while (true)
            {
                rdr.EatWhitespace();

                var pk = rdr.Rdr.PeekChar();
                switch (pk)
                {
                    case null:
                        throw new Exception("Found End of file before end of list");
                    case ')':
                        rdr.Rdr.Read();
                        return Cons.FromList(vals);
                    default:
                        vals.Add(rdr.ReadOne());
                        break;
                }
            }

        }
    }
    
    internal class VectorStructReader : IReader
    {
        public object? Read(LispReader rdr, char start)
        {
            var vals = new List<object>();
            while (true)
            {
                rdr.EatWhitespace();

                var pk = rdr.Rdr.PeekChar();
                switch (pk)
                {
                    case null:
                        throw new Exception("Found End of file before end of list");
                    case ']':
                        rdr.Rdr.Read();
                        return Cons.FromList(vals);
                    default:
                        vals.Add(rdr.ReadOne());
                        break;
                }
            }

        }
    }
    
    internal class UnmatchedReader : IReader
    {
        private readonly char _other;
        public UnmatchedReader(char other)
        {
            _other = other;
        }
        public object? Read(LispReader rdr, char start)
        {
            throw new Exception($"Found {start} without matching {_other}");

        }
    }
}