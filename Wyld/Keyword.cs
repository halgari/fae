using System;
using System.Collections.Concurrent;

namespace Wyld
{
    public class Keyword : IComparable<Keyword>
    {
        private string Namespace;

        private string Name;

        private string _str;

        private readonly int _hash;

        private Keyword Value => this;

        public bool IsMeta { get; }
        public bool IsFalsey { get; }
        public bool IsUndefinedNS { get; }


        protected Keyword(string name)
        {
            var offset = name.IndexOf("/", StringComparison.Ordinal);
            Namespace = offset == -1 ? "ns.undefined" : name.Substring(0, offset);

            Name = string.Intern(name.Substring(offset + 1));
            _str = string.Intern(Namespace + "/" + Name);
            _hash = name.GetHashCode() ^ 0xBEEF;

            IsMeta = Namespace.StartsWith("meta.");
            IsFalsey = Name.EndsWith("!");
            IsUndefinedNS = Namespace == "ns.undefined";
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
            return string.Compare(_str, other!._str, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj)
        {
            return obj != null && ReferenceEquals(this, obj);
        }

        public static Keyword Intern(string ns, string name)
        {
            return Intern(ns + "/" + name);
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
        public static Keyword Type = Keyword.Intern("wyld.compiler/type");
    }
}