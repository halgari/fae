using System;
using System.Collections.Immutable;

namespace Wyld
{
    public class Symbol : IEquatable<Symbol>, IComparable<Symbol>, IMeta
    {
        public string? Namespace { get; }
        public string Name { get; }
        private readonly int _hash;


        public string _str;
        
        public ImmutableDictionary<Keyword, object> Meta { get; } = ImmutableDictionary<Keyword, object>.Empty;

        private Symbol(string? ns, string name, string str)
        {
            Namespace = ns;
            Name = name;
            _hash = HashCode.Combine(Namespace, Name) ^ 0xBEAF;
            _str = str;
        }

        private Symbol(string? ns, string name, string str, int hashCode, ImmutableDictionary<Keyword, object> meta)
        {
            Namespace = ns;
            Name = name;
            _str = str;
            _hash = hashCode;
            Meta = meta;
        }

        public static Symbol Intern(string? ns, string name)
        {
            return new(ns  == null ? null : string.Intern(ns), 
                string.Intern(name),
                ns == null ? name : ns + "/" + name);
        }

        public static Symbol Parse(string nsAndName)
        {
            var indexOf = nsAndName.IndexOf("/", StringComparison.InvariantCulture);
            if (indexOf == -1 || nsAndName == "/")
            {
                return Intern(null, nsAndName);
            }

            return Intern(nsAndName.Substring(0, indexOf), nsAndName.Substring(indexOf + 1));
        }

        public object WithMeta(ImmutableDictionary<Keyword, object> meta)
        {
            return new Symbol(Namespace, Name, _str, _hash, meta);
        }
        
        public override bool Equals(object? obj)
        {
            return obj switch
            {
                null => false,
                Symbol other when other.Namespace == Namespace && other.Name == Name => true,
                _ => false
            };
        }

        public bool Equals(Symbol other)
        {
            if (ReferenceEquals(this, other)) return true;
            return Namespace == other.Namespace && Name == other.Name;
        }

        public override int GetHashCode()
        {
            return _hash;
        }

        public override string ToString()
        {
            return _str;
        }

        public int CompareTo(Symbol other)
        {
            if (ReferenceEquals(this, other)) return 0;
            var nsComparison = string.Compare(Namespace, other.Namespace, StringComparison.Ordinal);
            if (nsComparison != 0) return nsComparison;
            var nameComparison = string.Compare(Name, other.Name, StringComparison.Ordinal);
            if (nameComparison != 0) return nameComparison;
            return _hash.CompareTo(other._hash);
        }


    }
}