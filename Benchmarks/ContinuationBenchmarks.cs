using System;
using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
    public class ContinuationBenchmarks
    {

        [Benchmark]
        public void RunWithExceptions()
        {
            DoIt(0, 10);
        }

        public int DoIt(int depth, int maxDepth)
        {
            if (depth == maxDepth)
            {
                throw new Exception();
            }
            else
            {
                try
                {
                    return DoIt(depth + 1, maxDepth);

                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }
        }
    }
}