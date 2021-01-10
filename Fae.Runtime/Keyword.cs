#nullable enable
using System;
using System.Collections.Concurrent;
using System.Dynamic;
using System.Linq.Expressions;

namespace Fae.Runtime
{
    public class Keyword : IComparable<Keyword>
    {
        static Keyword()
        {
            DynamicRuntime.StructDefinitions.TryAdd(typeof(Keyword), new KeywordStructDefinition());
            DynamicRuntime.StructDefinitions.TryAdd(typeof(FalseyKeyword), new FalseyKeywordStructDefinition());
        }
        
        [StructMember("keyword/namespace")]
        private string _ns;
        
        [StructMember("keyword/name")]
        private string _name;
        
        [StructMember("keyword/string")]
        private string _str;
        
        [StructMember("value/hash")]
        private readonly int _hash;

        [StructMember("keyword/value")] 
        private Keyword Value => this;

        public bool IsMeta { get; }
        public bool IsFalsey { get; }
        public bool IsUndefinedNS { get; }


        protected Keyword(string name)
        {
            var offset = name.IndexOf("/", StringComparison.Ordinal);
            _ns = offset == -1 ? "ns.undefined" : name.Substring(0, offset);

            _name = name.Substring(offset + 1);
            _str = String.Intern(_ns + "/" + _name);
            _hash = name.GetHashCode() ^ 0xBEEF;

            IsMeta = _ns.StartsWith("meta.");
            IsFalsey = _name.EndsWith("!");
            IsUndefinedNS = _ns == "ns.undefined";
        }

        
        private static readonly ConcurrentDictionary<string, Keyword> _registry = new();
        
        public static Keyword Intern(string name)
        {
            if (_registry.TryGetValue(name, out var found))
                return found;

            var kw = name.EndsWith("!") ? new FalseyKeyword(name) : new Keyword(name);
            return _registry.TryAdd(name, kw) ? kw : _registry[name];
        }

        public override string ToString()
        {
            return ":" + _str;
        }

        public override int GetHashCode()
        {
            return _hash;
        }

        public int CompareTo(Keyword? other)
        {
            return String.Compare(_str, other!._str, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj)
        {
            return obj != null && ReferenceEquals(this, obj);
        }
        public string GetStr()
        {
            return _str;
        }
    }

    public class FalseyKeyword : Keyword
    {
        public FalseyKeyword(string input) : base(input)
        {
            
        }
    }



    public static class KW
    {
        public static Keyword KeywordName = Keyword.Intern("keyword/name");
        public static Keyword KeywordNamespace = Keyword.Intern("keyword/namespace");
        public static Keyword KeywordString = Keyword.Intern("keyword/string");
        public static Keyword KeywordValue = Keyword.Intern("keyword/value");
        public static Keyword MemberNotFound = Keyword.Intern("fae.member/not-found!");

        public static Keyword EOL = Keyword.Intern("fae.list/end-of-list!");
        public static Keyword First = Keyword.Intern("fae.list/first");
        public static Keyword Next = Keyword.Intern("fae.list/next");
        public static Keyword ELSE = Keyword.Intern("fae.conditional/branch");
        public static Keyword Flag = Keyword.Intern("fae.flag/set");
        public static Keyword EOS = Keyword.Intern("fae.reader/end-of-stream!");
        public static Keyword ReaderFile = Keyword.Intern("meta.fae.reader/file");
        public static Keyword ReaderLine = Keyword.Intern("meta.fae.reader/line");
        public static Keyword ReaderColumn = Keyword.Intern("meta.fae.reader/column");

        public static Keyword ValueInt = Keyword.Intern("fae.int/value");
        public static Keyword SizedCount = Keyword.Intern("fae.sized/size");
        public static Keyword SymbolKeyword = Keyword.Intern("fae.symbol/keyword");
    }

    internal class KeywordStructDefinition : IStructDefinition
    {
        public virtual (Keyword, Type)[] GetMembers()
        {
            return new[]
            {
                (KW.KeywordName, typeof(string)),
                (KW.KeywordNamespace, typeof(string)),
                (KW.KeywordString, typeof(string)),
                (KW.KeywordValue, typeof(Keyword)),
            };
        }

        public virtual Expression MemberGetter(Expression self, Keyword memberName)
        {
            if (ReferenceEquals(memberName, KW.KeywordName))
            {
                return Expression.Field(Expression.Convert(self, typeof(Keyword)), "_name");
            }

            if (ReferenceEquals(memberName, KW.KeywordNamespace))
            {
                return Expression.Field(Expression.Convert(self, typeof(Keyword)), "_ns");
            }

            if (ReferenceEquals(memberName, KW.KeywordString))
            {
                return Expression.Field(Expression.Convert(self, typeof(Keyword)), "_str");
            }

            if (ReferenceEquals(memberName, KW.KeywordValue))
            {
                return self;
            }

            throw new InvalidOperationException();
        }
    }
    
    internal class FalseyKeywordStructDefinition : KeywordStructDefinition
    {
        public override (Keyword, Type)[] GetMembers()
        {
            return new[]
            {
                (KW.KeywordName, typeof(string)),
                (KW.KeywordNamespace, typeof(string)),
                (KW.KeywordString, typeof(string)),
                (KW.KeywordValue, typeof(Keyword)),
                (KW.ELSE, typeof(Keyword))
            };
        }

        public override Expression MemberGetter(Expression self, Keyword memberName)
        {
            return ReferenceEquals(memberName, KW.ELSE) ? Expression.Constant(KW.Flag) : base.MemberGetter(self, memberName);
        }
    }
}