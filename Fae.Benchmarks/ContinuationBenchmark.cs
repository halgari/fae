using System;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;



namespace Fae.Benchmarks
{
    public class ContinuationBenchmarks
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
                throw new Exception();
            }
            else
            {
                try
                {
                    return DoItWithException(depth + 1, maxDepth);

                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }
        }
        
        [Benchmark]
        public void RunWithExceptionsSealed()
        {
            try
            {

                DoItWithExceptionSealed(0, MAX_DEPTH);
            }
            catch (EffectException e)
            {
            }
        }

        public int DoItWithExceptionSealed(int depth, int maxDepth)
        {
            if (depth == maxDepth)
            {
                throw new EffectException();
            }
            else
            {

                try
                {
                    return DoItWithExceptionSealed(depth + 1, maxDepth) - 1;

                }
                catch (EffectException ex)
                {
                    throw new EffectException() {Parent = ex};
                }
            }
        }

        public sealed class EffectException : Exception
        {
            public object Parent;
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
                return new Result<int> {Next = new object()};
            }
            else
            {

                var end =  DoItWithStruct(depth + 1, maxDepth);
                if (end.Next != null)
                {
                    return new Result<int>() {Next = end};
                }

                return new Result<int>() {Value = 1};
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
                return new Effect {Next = new object()};
            }
            else
            {

                var end = DoItWithCast(depth + 1, maxDepth);
                if (end is Effect)
                {
                    return new Effect {Next = end};
                }

                return 42;
            }
        }

        public class Effect
        {
            public object Next;
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
                effect = new Effect {Next = new object()};
                return default;
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