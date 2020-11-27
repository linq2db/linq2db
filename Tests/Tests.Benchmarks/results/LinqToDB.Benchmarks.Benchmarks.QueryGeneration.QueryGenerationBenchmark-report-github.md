``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.18363.1198 (1909/November2018Update/19H2)
AMD Ryzen 9 3950X, 1 CPU, 32 logical and 16 physical cores
.NET Core SDK=5.0.100
  [Host]     : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT
  Job-FELQET : .NET Framework 4.8 (4.8.4250.0), X64 RyuJIT
  Job-DZRSGP : .NET Core 2.1.23 (CoreCLR 4.6.29321.03, CoreFX 4.6.29321.01), X64 RyuJIT
  Job-GMTMFB : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT

Jit=RyuJit  Platform=X64  

```
|                    Method |       Runtime | DataProvider |       Mean |     Median | Ratio | Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------------- |-------------- |------------- |-----------:|-----------:|------:|------:|------:|------:|----------:|
|             **VwSalesByYear** |    **.NET 4.7.2** |       **Access** |   **357.0 μs** |   **357.2 μs** |  **1.00** |     **-** |     **-** |     **-** |    **128 KB** |
|             VwSalesByYear | .NET Core 2.1 |       Access |   346.5 μs |   346.1 μs |  0.98 |     - |     - |     - | 103.95 KB |
|             VwSalesByYear | .NET Core 3.1 |       Access |   517.8 μs |   519.5 μs |  1.45 |     - |     - |     - | 107.61 KB |
|                           |               |              |            |            |       |       |       |       |           |
|     VwSalesByYearMutation |    .NET 4.7.2 |       Access |   384.4 μs |   383.7 μs |  1.00 |     - |     - |     - |    136 KB |
|     VwSalesByYearMutation | .NET Core 2.1 |       Access |   372.3 μs |   371.9 μs |  0.97 |     - |     - |     - |  114.1 KB |
|     VwSalesByYearMutation | .NET Core 3.1 |       Access |   545.0 μs |   545.5 μs |  1.41 |     - |     - |     - | 113.23 KB |
|                           |               |              |            |            |       |       |       |       |           |
| VwSalesByCategoryContains |    .NET 4.7.2 |       Access | 1,107.4 μs | 1,107.3 μs |  1.00 |     - |     - |     - |    376 KB |
| VwSalesByCategoryContains | .NET Core 2.1 |       Access | 1,268.9 μs | 1,259.7 μs |  1.15 |     - |     - |     - | 343.45 KB |
| VwSalesByCategoryContains | .NET Core 3.1 |       Access | 1,595.7 μs | 1,584.6 μs |  1.43 |     - |     - |     - | 343.69 KB |
|                           |               |              |            |            |       |       |       |       |           |
|             **VwSalesByYear** |    **.NET 4.7.2** |     **Firebird** |   **357.3 μs** |   **356.7 μs** |  **1.00** |     **-** |     **-** |     **-** |    **128 KB** |
|             VwSalesByYear | .NET Core 2.1 |     Firebird |   348.3 μs |   349.5 μs |  0.97 |     - |     - |     - | 104.57 KB |
|             VwSalesByYear | .NET Core 3.1 |     Firebird |   520.4 μs |   519.2 μs |  1.45 |     - |     - |     - | 108.59 KB |
|                           |               |              |            |            |       |       |       |       |           |
|     VwSalesByYearMutation |    .NET 4.7.2 |     Firebird |   385.7 μs |   381.4 μs |  1.00 |     - |     - |     - |    136 KB |
|     VwSalesByYearMutation | .NET Core 2.1 |     Firebird |   371.7 μs |   372.1 μs |  0.96 |     - |     - |     - | 115.28 KB |
|     VwSalesByYearMutation | .NET Core 3.1 |     Firebird |   631.4 μs |   600.1 μs |  1.69 |     - |     - |     - |  114.4 KB |
|                           |               |              |            |            |       |       |       |       |           |
| VwSalesByCategoryContains |    .NET 4.7.2 |     Firebird |   782.6 μs |   777.3 μs |  1.00 |     - |     - |     - |    288 KB |
| VwSalesByCategoryContains | .NET Core 2.1 |     Firebird |   959.9 μs |   950.0 μs |  1.23 |     - |     - |     - | 263.25 KB |
| VwSalesByCategoryContains | .NET Core 3.1 |     Firebird | 1,199.4 μs | 1,193.6 μs |  1.52 |     - |     - |     - | 262.42 KB |
