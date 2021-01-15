using System;
using System.Data;
using System.IO;
using System.Text;
using Xunit;

namespace Wyld.Test
{
    public class CompilerTests
    {

        [Fact]
        public void CanCompileIntegers()
        {
            Assert.Equal(1, Eval("1"));
        }

        [Fact]
        public void CanCompileAddition()
        {
            Assert.Equal(42, Eval("(sys/+ 41 1)"));
        }

        [Fact]
        public void CanCompileLambaExpressions()
        {
            Assert.Equal(42, Eval("((fn ^int [^int x ^int y] (sys/+ x y)) 1 41)"));
        }

        private object Eval(string s)
        {
            var form = new LispReader(new LineNumberingReader(new MemoryStream(Encoding.UTF8.GetBytes(s)))).ReadOne();
            var compiler = new Compiler();
            var result = compiler.Compile(form)();
            return result;
        }
    }
}