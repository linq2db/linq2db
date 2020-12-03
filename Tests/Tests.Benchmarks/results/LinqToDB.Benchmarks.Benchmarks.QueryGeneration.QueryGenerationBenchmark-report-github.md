``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.18363.1198 (1909/November2018Update/19H2)
AMD Ryzen 9 3950X, 1 CPU, 32 logical and 16 physical cores
.NET Core SDK=5.0.100
  [Host]     : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT
  Job-CNGKFU : .NET Framework 4.8 (4.8.4250.0), X64 RyuJIT
  Job-KUAWIC : .NET Core 2.1.23 (CoreCLR 4.6.29321.03, CoreFX 4.6.29321.01), X64 RyuJIT
  Job-MYFSVO : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT

Jit=RyuJit  Platform=X64  

```
|                    Method |       Runtime | DataProvider |       Mean |     Median | Ratio | Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------------- |-------------- |------------- |-----------:|-----------:|------:|------:|------:|------:|----------:|
|             **VwSalesByYear** |    **.NET 4.7.2** |       **Access** |   **361.7 μs** |   **360.8 μs** |  **1.00** |     **-** |     **-** |     **-** |    **128 KB** |
|             VwSalesByYear | .NET Core 2.1 |       Access |   422.3 μs |   402.8 μs |  1.15 |     - |     - |     - | 104.56 KB |
|             VwSalesByYear | .NET Core 3.1 |       Access |   516.1 μs |   516.8 μs |  1.41 |     - |     - |     - | 107.92 KB |
|                           |               |              |            |            |       |       |       |       |           |
|     VwSalesByYearMutation |    .NET 4.7.2 |       Access |   389.0 μs |   391.1 μs |  1.00 |     - |     - |     - |    136 KB |
|     VwSalesByYearMutation | .NET Core 2.1 |       Access |   378.7 μs |   377.4 μs |  0.99 |     - |     - |     - | 113.18 KB |
|     VwSalesByYearMutation | .NET Core 3.1 |       Access |   547.4 μs |   545.1 μs |  1.41 |     - |     - |     - | 113.15 KB |
|                           |               |              |            |            |       |       |       |       |           |
| VwSalesByCategoryContains |    .NET 4.7.2 |       Access | 1,142.5 μs | 1,127.1 μs |  1.00 |     - |     - |     - |  444.3 KB |
| VwSalesByCategoryContains | .NET Core 2.1 |       Access | 1,309.6 μs | 1,309.3 μs |  1.13 |     - |     - |     - | 415.82 KB |
| VwSalesByCategoryContains | .NET Core 3.1 |       Access | 1,621.0 μs | 1,609.4 μs |  1.40 |     - |     - |     - | 413.92 KB |
|                           |               |              |            |            |       |       |       |       |           |
|             **VwSalesByYear** |    **.NET 4.7.2** |     **Firebird** |   **364.9 μs** |   **366.3 μs** |  **1.00** |     **-** |     **-** |     **-** |    **128 KB** |
|             VwSalesByYear | .NET Core 2.1 |     Firebird |   356.4 μs |   353.7 μs |  0.99 |     - |     - |     - | 105.18 KB |
|             VwSalesByYear | .NET Core 3.1 |     Firebird |   521.3 μs |   521.6 μs |  1.41 |     - |     - |     - | 108.52 KB |
|                           |               |              |            |            |       |       |       |       |           |
|     VwSalesByYearMutation |    .NET 4.7.2 |     Firebird |   381.9 μs |   382.2 μs |  1.00 |     - |     - |     - |    136 KB |
|     VwSalesByYearMutation | .NET Core 2.1 |     Firebird |   373.9 μs |   372.8 μs |  0.98 |     - |     - |     - | 114.36 KB |
|     VwSalesByYearMutation | .NET Core 3.1 |     Firebird |   544.5 μs |   544.6 μs |  1.42 |     - |     - |     - | 114.32 KB |
|                           |               |              |            |            |       |       |       |       |           |
| VwSalesByCategoryContains |    .NET 4.7.2 |     Firebird |   817.5 μs |   808.1 μs |  1.00 |     - |     - |     - |    320 KB |
| VwSalesByCategoryContains | .NET Core 2.1 |     Firebird | 1,000.4 μs |   988.5 μs |  1.22 |     - |     - |     - | 289.66 KB |
| VwSalesByCategoryContains | .NET Core 3.1 |     Firebird | 1,214.7 μs | 1,207.5 μs |  1.47 |     - |     - |     - | 288.02 KB |
