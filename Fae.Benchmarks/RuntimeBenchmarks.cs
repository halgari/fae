using System;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;


namespace Fae.Benchmarks
{
    public class RuntimeBenchmarks
    {
        [Params(1, 2, 4, 8, 16, 32)] 
        public int MAX_DEPTH { get; set; }

        [Benchmark]
        public void RunWithExceptions()
        {
            try
            {

                DoItWithException(0, MAX_DEPTH);
            }
            catch (Exception e)
            {
            }
        }

        public int DoItWithException(int depth, int maxDepth)
        {
            if (depth == maxDepth)
            {
                return depth;
            }
            else
            {

                try
                {
                    return DoItWithException(depth + 1, maxDepth) - 1;

                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }
        }
        
        
        
        [Benchmark]
        public void RunWithStruct()
        {

            DoItWithStruct(0, MAX_DEPTH);

        }

        public Result<int> DoItWithStruct(int depth, int maxDepth)
        {
            if (depth == maxDepth)
            {
                return new Result<int> {Value = 0};
            }
            else
            {

                var end =  DoItWithStruct(depth + 1, maxDepth);
                if (end.Next != null)
                {
                    return new Result<int>() {Next = end};
                }

                return new Result<int>() {Value = end.Value - 1};
            }
        }
        
        public struct Result<T>
        {
            public T Value;
            public object Next;
        }
        
        [Benchmark]
        public void RunWithCast()
        {

            DoItWithCast(0, MAX_DEPTH);

        }

        public object DoItWithCast(int depth, int maxDepth)
        {
            if (depth == maxDepth)
            {
                return 0;
            }
            else
            {

                var end = DoItWithCast(depth + 1, maxDepth);
                if (end is Effect)
                {
                    return new Effect {Next = end};
                }

                return (int)end - 1;
            }
        }

        public class Effect
        {
            public object Next;
        }

        
        [Benchmark]
        public void RunWithFullNative()
        {

            DoItWithFullNative(0, MAX_DEPTH);

        }

        public int DoItWithFullNative(int depth, int maxDepth)
        {
            if (depth == maxDepth)
            {
                return 0;
            }
            else
            {

                var end = DoItWithFullNative(depth + 1, maxDepth);

                return end - 1;
            }
        }
        
        [Benchmark]
        public void RunWithRef()
        {

            Effect effect = null;
            DoItWithRef(0, MAX_DEPTH, ref effect);

        }

        public int DoItWithRef(int depth, int maxDepth, ref Effect effect)
        {
            if (depth == maxDepth)
            {
                return depth;
            }
            else
            {

                var end = DoItWithRef(depth + 1, maxDepth, ref effect);
                if (effect != null)
                {
                    effect = new Effect {Next = end};
                }

                return end;
            }
        }

    }
}