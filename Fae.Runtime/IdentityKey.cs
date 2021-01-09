using System;
using System.Linq;

namespace Fae.Runtime
{
    public class IdentityKey : IEquatable<IdentityKey>
    {
        private object[] _values;
        private int _hash;

        public IdentityKey(object[] values)
        {
            _values = values;
            _hash = values.Aggregate(0, (a, b) => a ^ b.GetHashCode());
        }

        public override int GetHashCode()
        {
            return _hash;
        }

        public bool Equals(IdentityKey other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            if (_hash != other._hash)
                return false;

            if (_values.Length != other._values.Length)
                return false;

            for (var i = 0; i < _values.Length; i++)
            {
                if (!ReferenceEquals(_values[i], other._values[i]))
                    return false;
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((IdentityKey) obj);
        }

        public override string ToString()
        {
            return "{" + $"{_hash}, " + string.Join(", ", _values.Select(v => v.GetHashCode())) + "}";
        }
    }
}