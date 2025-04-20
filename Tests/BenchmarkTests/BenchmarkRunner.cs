using BenchmarkDotNet.Running;
using System;

namespace SimpleAutoMapping.Tests.BenchmarkTests
{
    public class MapperBenchmarkRunner
    {
        public static void Run()
        {
            Console.WriteLine("Ejecutando benchmarks de comparación entre mappers...");
            BenchmarkDotNet.Running.BenchmarkRunner.Run<MapperComparison>();
        }
    }
} 