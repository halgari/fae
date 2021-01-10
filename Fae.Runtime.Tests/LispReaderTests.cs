using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using static Fae.Runtime.RuntimeObject;

namespace Fae.Runtime.Tests
{
    public class LispReaderTests
    {
        [Fact]
        public void CanReadIntegers()
        {
            var val = Read("42");
            
            Assert.Equal(42, RT.Get(val, KW.ValueInt));
            Assert.Equal("<unknown>", RT.Get(val, KW.ReaderFile));
            Assert.Equal(1, RT.Get(val, KW.ReaderLine));
            Assert.Equal(1, RT.Get(val, KW.ReaderColumn));
        }

        [Fact]
        public void CanReadLists()
        {
            var val = Read($"({string.Join(" ", Enumerable.Range(0, 10))})");
            Assert.Equal(0, RT.Get(RT.Get(val, KW.First), KW.ValueInt));
        }


        public static object Read(string s)
        {
            return new LispReader(new LineNumberingReader(new MemoryStream(Encoding.UTF8.GetBytes(s)))).ReadOne();
        }
        
    }
}