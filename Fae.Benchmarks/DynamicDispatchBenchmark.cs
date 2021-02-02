using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Validators;

namespace Fae.Benchmarks
{
    public class DynamicDispatchBenchmark
    {
        private IInterface cls;
        private Func<string, int> fnDirect;
        private dynamic fDynamic;
        private dynamic sDynamic = "test";

        [GlobalSetup]
        public void Setup()
        {
            cls = new Foo();
            fnDirect = x => x.Length;
            fDynamic = fnDirect;
        }
        
        [Benchmark]
        public void SingleInterfaceDispatch()
        {
            cls.DoIt("test");
        }
        
        public interface IInterface
        {
            int DoIt(string s);
        }

        public class Foo : IInterface
        {
            public int DoIt(string s)
            {
                return s.Length;
            }
        }

        [Benchmark]
        public void DynamicDispatch()
        {
            fDynamic("test");
        }        
        
        [Benchmark]
        public void FunctionDispatch()
        {
            fnDirect("test");
        }
        
        [Benchmark]
        public void DynamicExpr()
        {
            int s = sDynamic.Length;
        }
        
        
        
    }
}