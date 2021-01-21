using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Wyld
{
    public class Cons : IEnumerable<object>, IMeta
    {
        private int _hash;
        public int Count { get; }

        private Cons(object head, Cons? tail, int hash, int count, ImmutableDictionary<Keyword, object> meta)
        {
            Head = head;
            Tail = tail;
            Meta = meta;
            Count = count;
            _hash = hash;
        }
        
        public Cons(object head, Cons? tail)
        {
            Head = head;
            Tail = tail;
            _hash = 0;
            Count = tail?.Count + 1 ?? 1;
        }

        public object WithMeta(ImmutableDictionary<Keyword, object> meta)
        {
            return new Cons(Head, Tail, _hash, Count, meta);
        }

        public static Cons? FromList<T>(List<T> objs)
            where T : notnull
        {
            Cons? acc = null;
            for (var idx = objs.Count - 1; idx >= 0; idx--)
            {
                acc = new Cons(objs[idx], acc);
            }
            return acc;
        }
        
        public static Cons? FromArray<T>(T[] objs)
            where T : notnull
        {
            Cons? acc = null;
            for (var idx = objs.Length - 1; idx >= 0; idx--)
            {
                acc = new Cons(objs[idx], acc);
            }
            return acc;
        }
        
        public Cons? Tail { get; }
        public object Head { get; }
        public ImmutableDictionary<Keyword, object> Meta { get; } = ImmutableDictionary<Keyword, object>.Empty;

        public override string ToString()
        {
            return "(" + string.Join(" ", this.Select(s => s.ToString())) + ")";
        }

        public IEnumerator<object> GetEnumerator()
        {
            var acc = this;
            while (acc != null)
            {
                yield return acc.Head;
                acc = acc.Tail;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}