using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Wyld
{
    public class Vector : IMeta, IEnumerable<object>
    {
        private object[] _vals;

        public Vector(object[] vals)
        {
            _vals = vals;
            Meta = ImmutableDictionary<Keyword, object>.Empty;
        }

        public Cons? ToCons()
        {
            return Cons.FromArray(_vals);
        }

        public Vector(object[] vals, ImmutableDictionary<Keyword, object> meta)
        {
            _vals = vals;
            Meta = meta;
        }

        public ImmutableDictionary<Keyword, object> Meta { get; }
        public int Length => _vals.Length;

        public object WithMeta(ImmutableDictionary<Keyword, object> meta)
        {
            return new Vector(_vals, meta);
        }

        public IEnumerator<object> GetEnumerator()
        {
            return ((IEnumerable<object>) _vals).GetEnumerator();
        }

        public override string ToString()
        {
            return "[" + string.Join(",", _vals) + "]";
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}