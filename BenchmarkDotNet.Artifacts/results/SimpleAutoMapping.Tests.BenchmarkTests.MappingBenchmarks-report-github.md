``` ini

BenchmarkDotNet=v0.13.5, OS=Windows 11 (10.0.26100.3775)
Unknown processor
.NET SDK=9.0.201
  [Host]     : .NET 9.0.3 (9.0.325.11113), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.3 (9.0.325.11113), X64 RyuJIT AVX2


```
|                    Method |         Mean |      Error |     StdDev |   Gen0 | Allocated |
|-------------------------- |-------------:|-----------:|-----------:|-------:|----------:|
|          Map_SimpleObject |   359.270 ns |  3.1221 ns |  2.9204 ns | 0.0648 |     816 B |
|  MapManually_SimpleObject |     4.676 ns |  0.0896 ns |  0.0794 ns | 0.0032 |      40 B |
|         Map_ComplexObject | 1,183.074 ns | 13.4880 ns | 11.9567 ns | 0.1659 |    2096 B |
| MapManually_ComplexObject |    36.557 ns |  0.7879 ns |  1.2723 ns | 0.0197 |     248 B |
|   PartialMap_SimpleObject |   340.036 ns |  3.8570 ns |  3.6079 ns | 0.0648 |     816 B |
