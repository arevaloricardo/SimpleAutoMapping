using BenchmarkDotNet.Running;
using SimpleAutoMapping.Tests.BenchmarkTests;

namespace SimpleAutoMapping.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<MappingBenchmarks>();
            BenchmarkRunner.Run<CollectionBenchmarks>();
        }
    }
} 