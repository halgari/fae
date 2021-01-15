using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace Wyld.Test
{
    public class ReaderAndValueTests
    {
        [Fact]
        public void CanReadIntegers()
        {
            Assert.Equal(42, Read("42"));
            Assert.Equal(-1, Read("-1"));
        }

        [Fact]
        public void CanReadMeta()
        {
            Assert.Equal(Symbol.Parse("foo"), Read("^int foo"));
        }

        [Fact]
        public void CanReadLists()
        {
            var lst = (Cons)Read("(0 1 2 3 4 5 6 7 8 9 10)");
            var cnt = 0;
            foreach (var (o, i) in lst.Select((idx, v) => (idx, v)))
            {
                Assert.Equal(o, i);
                cnt++;
            }
            Assert.Equal(11, cnt);
        }

        [Fact]
        public void CanReadSymbols()
        {
            var val = (Cons) Read("(+ 1 2)");
            Assert.Equal(Symbol.Parse("+"), val.Head);
            Assert.Equal(3, val.Count);

            foreach (var (r, e) in val.Zip(new object[] {Symbol.Parse("+"), 1, 2}))
            {
                Assert.Equal(e, r);
            }

        }


        private object Read(string v)
        {
            return new LispReader(new LineNumberingReader(new MemoryStream(Encoding.UTF8.GetBytes(v)))).ReadOne();
        }
        
    }
}