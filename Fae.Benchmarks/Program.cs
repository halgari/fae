using System;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Running;

[module:SkipLocalsInit]

namespace Fae.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}