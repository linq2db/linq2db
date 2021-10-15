``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417994 Hz, Resolution=292.5693 ns, Timer=TSC
.NET SDK=5.0.402
  [Host]     : .NET 5.0.11 (5.0.1121.47308), X64 RyuJIT
  Job-ODZCDL : .NET 5.0.11 (5.0.1121.47308), X64 RyuJIT
  Job-PCJJBI : .NET Core 3.1.20 (CoreCLR 4.700.21.47003, CoreFX 4.700.21.47101), X64 RyuJIT
  Job-HHEMGO : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT

Jit=RyuJit  Platform=X64  

```
|                    Method |              Runtime | DataProvider |       Mean |     Median | Ratio | Allocated |
|-------------------------- |--------------------- |------------- |-----------:|-----------:|------:|----------:|
|             **VwSalesByYear** |             **.NET 5.0** |       **Access** |   **503.4 μs** |   **496.9 μs** |  **0.52** |    **128 KB** |
|             VwSalesByYear |        .NET Core 3.1 |       Access |   600.0 μs |   598.5 μs |  0.61 |    130 KB |
|             VwSalesByYear | .NET Framework 4.7.2 |       Access |   975.7 μs |   907.5 μs |  1.00 |    168 KB |
|                           |                      |              |            |            |       |           |
|     VwSalesByYearMutation |             .NET 5.0 |       Access |   789.1 μs |   792.1 μs |  0.57 |    207 KB |
|     VwSalesByYearMutation |        .NET Core 3.1 |       Access |   918.1 μs |   919.6 μs |  0.66 |    208 KB |
|     VwSalesByYearMutation | .NET Framework 4.7.2 |       Access | 1,388.3 μs | 1,332.1 μs |  1.00 |    256 KB |
|                           |                      |              |            |            |       |           |
| VwSalesByCategoryContains |             .NET 5.0 |       Access | 2,169.8 μs | 2,153.5 μs |  0.65 |    473 KB |
| VwSalesByCategoryContains |        .NET Core 3.1 |       Access | 2,516.2 μs | 2,530.0 μs |  0.73 |    474 KB |
| VwSalesByCategoryContains | .NET Framework 4.7.2 |       Access | 3,390.4 μs | 3,310.7 μs |  1.00 |    551 KB |
|                           |                      |              |            |            |       |           |
|             **VwSalesByYear** |             **.NET 5.0** |     **Firebird** |   **500.3 μs** |   **496.4 μs** |  **0.45** |    **129 KB** |
|             VwSalesByYear |        .NET Core 3.1 |     Firebird |   647.5 μs |   641.8 μs |  0.58 |    130 KB |
|             VwSalesByYear | .NET Framework 4.7.2 |     Firebird | 1,114.3 μs | 1,091.6 μs |  1.00 |    168 KB |
|                           |                      |              |            |            |       |           |
|     VwSalesByYearMutation |             .NET 5.0 |     Firebird |   857.4 μs |   853.9 μs |  0.67 |    210 KB |
|     VwSalesByYearMutation |        .NET Core 3.1 |     Firebird |   967.8 μs |   963.3 μs |  0.74 |    211 KB |
|     VwSalesByYearMutation | .NET Framework 4.7.2 |     Firebird | 1,311.0 μs | 1,216.2 μs |  1.00 |    256 KB |
|                           |                      |              |            |            |       |           |
| VwSalesByCategoryContains |             .NET 5.0 |     Firebird | 1,272.8 μs | 1,270.8 μs |  0.62 |    308 KB |
| VwSalesByCategoryContains |        .NET Core 3.1 |     Firebird | 1,386.1 μs | 1,370.0 μs |  0.69 |    309 KB |
| VwSalesByCategoryContains | .NET Framework 4.7.2 |     Firebird | 1,975.4 μs | 1,955.5 μs |  1.00 |    384 KB |
