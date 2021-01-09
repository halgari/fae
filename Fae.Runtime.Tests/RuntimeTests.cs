using Xunit;
using static Fae.Runtime.RuntimeObject;

namespace Fae.Runtime.Tests
{
    public class RuntimeTests
    {
        [Fact]
        public void CanGetMembers()
        {
            var val = RT.Get(KW.KeywordName, KW.KeywordName, KW.MemberNotFound);
            Assert.Equal(val, "name");
        }

        [Fact]
        public void CanCreateStructs()
        {
            var val = RT.New(KW.First, 42, KW.Next, KW.EOL);
            Assert.NotNull(val);
            Assert.Equal(42, RT.Get(val, KW.First));
            Assert.Equal(KW.EOL, RT.Get(val, KW.Next));
        }
        
        
        [Fact]
        public void StructsSortMembers()
        {
            var val = RT.New(KW.First, 42, KW.Next, KW.EOL);
            var val2 = RT.New(KW.Next, KW.EOL, KW.First, 42);
            Assert.Same(val.GetType(), val2.GetType());
        }

        [Fact]
        public void CanAddToStructs()
        {
            var val1 = RT.With(RT.New(KW.First, 42), KW.Next, KW.EOL);
            Assert.NotNull(val1);
            Assert.Equal(42, RT.Get(val1, KW.First));
            Assert.Equal(KW.EOL, RT.Get(val1, KW.Next));

            
            var val2 = RT.New(KW.First, 42, KW.Next, KW.EOL);
            Assert.Same(val1.GetType(), val2.GetType());
            
        }
    }
}