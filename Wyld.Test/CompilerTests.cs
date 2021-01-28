using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
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

        [Fact]
        public void CanCompileClosures()
        {
            Assert.Equal(8, Eval("(((fn ^int [^int y] (fn ^int [^int x] (sys/+ x y))) 3) 5)"));
        }

        [Fact]
        public void CanCompileLet()
        {
            Assert.Equal(42, Eval("(let [x 42] x)"));
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
        
        /// <summary>
        /// Exercises tail calls, defn, and recursive functions
        /// </summary>
        [Fact]
        public void CanCompileCountUpWithEffect()
        {
            Assert.Equal(Enumerable.Range(0, 100000 + 1)
                    .Concat(new []{100000})
                    .Select(i => (object)i)
                    .ToArray(),
                EvalPauseStream(@"(defn count-up ^int [^int x ^int max] 
                             (if (sys/= (pause x) max) 
                                 x 
                                 (count-up (sys/+ x 1) max)))
                           (count-up 0 100000)"));
        }

        [Fact]
        public void CanReadProperties()
        {
            Assert.Equal(3, Eval("(.-Length \"foo\")"));
        }

        [Fact]
        public void CanRaiseEffects()
        {
            Assert.Equal(new object[] {41, 42, 43}, 
                EvalPauseStream(@"
                     (defn inc ^int [^int x]
                        (sys/+ (pause x) 1))
                      
                     (let [x (inc 41)] (sys/+ 1 (pause x)))"));
        }

        [Fact]
        public void CanRaiseSimplestEffect()
        {
            Assert.Equal(new object[] {1, 1},
                EvalPauseStream("(^int sys/raise :pause 1)"));
        }
        
        [Fact]
        public void CanRaiseEffectsWithTailCall()
        {
            Assert.Equal(new object[] {1, 1}, 
                EvalPauseStream(@"
                     (defn foo ^int [^int x]
                        (pause x))

                     (defn bar ^int [^int x]
                        (foo x))
                      
                     (bar 1)"));
        }
        
        [Fact]
        public void CanRaiseEffectsInIfBranch()
        {
            Assert.Equal(new object[] {1, 1}, 
                EvalPauseStream(@"
                     (if (sys/= 1 (pause 1)) 1 2)"));
        }
        
        [Fact]
        public void CanRaiseEffectsInIfThen()
        {
            Assert.Equal(new object[] {2, 2}, 
                EvalPauseStream(@"
                     (if (sys/= 0 1) 1 (pause 2))"));
        }
        
        [Fact]
        public void CanRaiseEffectsInIfElse()
        {
            Assert.Equal(new object[] {1, 1}, 
                EvalPauseStream(@"
                     (if (sys/= 1 1) (pause 1) 2)"));
        }

        [Fact]
        public void CanUseEffectInTopLevel()
        {
            Assert.Equal(new object[] {1, 1},
                EvalPauseStream(@"(pause 1)"));
        }
        
        [Fact]
        public void CanCompileSimpleClosure()
        {
            Assert.Equal(1,
                Eval(@"(let [x 1]
                                ((fn ^int [] x)))"));
        }

        [Fact]
        public void CanCompileASimpleLet()
        {
            Assert.Equal(1, Eval("(let [x 1] x)"));
        }
        
        [Fact]
        public void CanPauseInLetBinding()
        {
            Assert.Equal(new object[] {1, 1}, EvalPauseStream("(let [x (pause 1)] x)"));
        }
        
        [Fact]
        public void CanPauseInLetBody()
        {
            Assert.Equal(new object[] {1, 1}, EvalPauseStream("(let [x 1] (pause x))"));
        }
        
        [Fact]
        public void CanPauseInLetBindingAndBody()
        {
            Assert.Equal(new object[] {1, 1, 1}, EvalPauseStream("(let [x (pause 1)] (pause x))"));
        }
        
        [Fact]
        public void CanCompileASimpleLetWithEffect()
        {
            Assert.Equal(new object[] {1, 1}, EvalPauseStream("(let [x 1] (pause x))"));
        }
        
        [Fact]
        public void CanUseEffectInAddition()
        {
            Assert.Equal(new object[] {1, 2, 3},
                EvalPauseStream(@"(sys/+ (pause 1) (pause 2))"));
        }
        
        [Fact]
        public void CanUseArgumentsAfterEffectContinuation()
        {
            Assert.Equal(new object[] {1, 2, 3},
                EvalPauseStream(@"(defn add ^int [^int x ^int y]
                                            (sys/+ (pause x) (pause y)))
                                         (add 1 2)"));
        }

        private object Eval(string s)
        {
            Result<object> lastObj = default;
            var reader = new LispReader(new LineNumberingReader(new MemoryStream(Encoding.UTF8.GetBytes(s))));
            var compiler = new Compiler2();

            while (true)
            {
                var form = reader.ReadOne();
                if (form == null)
                    return lastObj.Value;

                lastObj = compiler.Compile(form).Invoke();
                if (lastObj.Effect != null)
                    throw new Exception("Got Effect inside eval test");
            }
        }
        
        private object[] EvalPauseStream(string s)
        {
            Environment.Reset();
            s = @"(defn pause ^int [^int x]
                        (^int sys/raise :pause x))
                 " + s;
            Result<object> lastObj = default;
            var reader = new LispReader(new LineNumberingReader(new MemoryStream(Encoding.UTF8.GetBytes(s))));
            var compiler = new Compiler2();

            var results = new List<object>();
            
            
            while (true)
            {
                var form = reader.ReadOne();
                if (form == null)
                {
                    results.Add(lastObj.Value);
                    return results.ToArray();
                }

                lastObj = compiler.Compile(form).Invoke();
                TOP:
                if (lastObj.Effect != null)
                {
                    //if (!ReferenceEquals(lastObj.Effect.FlagValue, KW.Pause))
                    //    throw new Exception($"Got unexpected effect {lastObj.Effect.FlagValue}");
                    results.Add(lastObj.Effect.Data);
                    var eff = lastObj.Effect;
                    lastObj = ((Func<object, object, Result<object>>)eff.K)(eff, eff.Data);
                    goto TOP;
                }
            }

        }
    }
}