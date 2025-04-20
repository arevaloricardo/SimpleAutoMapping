``` ini

BenchmarkDotNet=v0.13.5, OS=Windows 11 (10.0.26100.3775)
Unknown processor
.NET SDK=9.0.201
  [Host]     : .NET 9.0.3 (9.0.325.11113), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.3 (9.0.325.11113), X64 RyuJIT AVX2


```
|                       Method |              Mean |             Error |            StdDev |       Gen0 |       Gen1 |    Allocated |
|----------------------------- |------------------:|------------------:|------------------:|-----------:|-----------:|-------------:|
|                Map_SmallList |       6,969.33 ns |         53.439 ns |         49.987 ns |     0.9842 |          - |      12408 B |
|        MapManually_SmallList |          89.05 ns |          1.186 ns |          1.109 ns |     0.0484 |          - |        608 B |
|                Map_LargeList |     667,703.80 ns |      4,856.576 ns |      4,542.844 ns |    93.7500 |    12.6953 |    1185020 B |
|        Map_NestedCollections |      14,010.96 ns |         80.252 ns |         67.014 ns |     2.0142 |          - |      25449 B |
| Map_ComplexNestedCollections |     178,816.83 ns |      1,756.474 ns |      1,557.068 ns |    20.9961 |     1.2207 |     263758 B |
|           Map_MillionRecords | 820,487,646.67 ns | 11,638,951.865 ns | 10,887,082.796 ns | 93000.0000 | 25000.0000 | 1184796560 B |
