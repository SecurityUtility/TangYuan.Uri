using BenchmarkDotNet.Running;

namespace TangYuan.Uri.Benchmark;

internal static class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run(typeof(Program).Assembly, args: args);
    }
}