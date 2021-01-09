using System;
using System.Linq.Expressions;
using Xunit;

namespace Fae.Runtime.Tests
{
    public class KeywordTests
    {

        [Fact]
        public void KeywordsInternProperly()
        {
            Assert.Same(KW.KeywordName, KW.KeywordName);
            Assert.Same(KW.KeywordName, Keyword.Intern("keyword/name"));
            Assert.NotSame(KW.KeywordName, KW.KeywordNamespace);
            Assert.NotSame(KW.KeywordName, KW.KeywordString);
        }

        [Fact]
        public void CanGetKeywordMembers()
        {
            Assert.Same(Getter(KW.KeywordName, KW.KeywordValue), KW.KeywordName);
            Assert.Equal("name", Getter(KW.KeywordName, KW.KeywordName));
            Assert.Equal("keyword", Getter(KW.KeywordName, KW.KeywordNamespace));
            Assert.Equal("keyword/name", Getter(KW.KeywordName, KW.KeywordString));
        }

        private object Getter(object obj, Keyword keywordValue)
        {
            var definition = DynamicRuntime.GetDefintion(obj);
            var expr = definition.MemberGetter(Expression.Constant(obj), keywordValue);
            return Expression.Lambda<Func<object>>(expr).Compile()();
        }
    }
}