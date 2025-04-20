using BenchmarkDotNet.Running;
using SimpleAutoMapping.Tests.BenchmarkTests;
using System;

namespace SimpleAutoMapping.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--compare-mappers")
            {
                Console.WriteLine("Ejecutando benchmarks de comparación entre mappers...");
                BenchmarkDotNet.Running.BenchmarkRunner.Run<MapperComparison>();
            }
            else
            {
                Console.WriteLine("Ejecutando benchmarks estándar...");
                BenchmarkDotNet.Running.BenchmarkRunner.Run<MappingBenchmarks>();
                BenchmarkDotNet.Running.BenchmarkRunner.Run<CollectionBenchmarks>();
            }
        }
    }
} 