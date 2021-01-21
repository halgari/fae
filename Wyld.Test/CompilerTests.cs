using System;
using System.Data;
using System.IO;
using System.Linq.Expressions;
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
        public void CanCompileString()
        {
            Assert.Equal("foo", Eval("\"foo\""));
        }

        [Fact]
        public void CanCompileAddition()
        {
            Assert.Equal(42, Eval("(sys/+ 41 1)"));
        }

        [Fact]
        public void CanCompileLambdaExpressions()
        {
            Assert.Equal(42, Eval("((fn add ^int [^int x ^int y] (sys/+ x y)) 1 41)"));
        }

        [Fact]
        public void CanCompileDef()
        {
            Assert.Equal(42, Eval("(def ^int foo 41) (sys/+ foo 1)"));
        }
        
        [Fact]
        public void CanCompileIf()
        {
            Assert.Equal(1, Eval("(if (sys/= 1 1) 1 2)"));
            Assert.Equal(2, Eval("(if (sys/= 1 2) 1 2)"));
        }

        /// <summary>
        /// Exercises tail calls, defn, and recursive functions
        /// </summary>
        [Fact]
        public void CanCompileCountUp()
        {
            Assert.Equal(1000 * 1000,
                        Eval(@"(defn count-up ^int [^int x ^int max] 
                             (if (sys/= x max) 
                                 x 
                                 (count-up (sys/+ x 1) max)))
                           (count-up 0 1000000)"));
        }

        [Fact]
        public void CanReadProperties()
        {
            Assert.Equal(3, Eval("(.-Length \"foo\")"));
        }

        private object Eval(string s)
        {
            object lastObj = "";
            var reader = new LispReader(new LineNumberingReader(new MemoryStream(Encoding.UTF8.GetBytes(s))));
            var compiler = new Compiler2();

            while (true)
            {
                var form = reader.ReadOne();
                if (form == null)
                    return lastObj;

                lastObj = compiler.Compile(form).Invoke();
            }
        }
    }
}