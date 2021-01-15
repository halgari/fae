using System.Collections.Immutable;

namespace Wyld
{
    public interface IMeta
    {
        public ImmutableDictionary<Keyword, object> Meta { get; }
        public object WithMeta(ImmutableDictionary<Keyword, object> meta);
    }
}