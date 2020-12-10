``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.18363.1256 (1909/November2018Update/19H2)
AMD Ryzen 9 3950X, 1 CPU, 32 logical and 16 physical cores
.NET Core SDK=5.0.101
  [Host]     : .NET Core 3.1.10 (CoreCLR 4.700.20.51601, CoreFX 4.700.20.51901), X64 RyuJIT
  Job-BCJDOU : .NET Framework 4.8 (4.8.4250.0), X64 RyuJIT
  Job-WKYYPJ : .NET Core 2.1.23 (CoreCLR 4.6.29321.03, CoreFX 4.6.29321.01), X64 RyuJIT
  Job-NDSFAC : .NET Core 3.1.10 (CoreCLR 4.700.20.51601, CoreFX 4.700.20.51901), X64 RyuJIT

Jit=RyuJit  Platform=X64  

```
|                    Method |       Runtime | DataProvider |       Mean |     Median | Ratio | Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------------- |-------------- |------------- |-----------:|-----------:|------:|------:|------:|------:|----------:|
|             **VwSalesByYear** |    **.NET 4.7.2** |       **Access** |   **359.8 μs** |   **361.1 μs** |  **1.00** |     **-** |     **-** |     **-** |    **128 KB** |
|             VwSalesByYear | .NET Core 2.1 |       Access |   455.7 μs |   455.4 μs |  1.28 |     - |     - |     - |  105.8 KB |
|             VwSalesByYear | .NET Core 3.1 |       Access |   435.7 μs |   432.5 μs |  1.21 |     - |     - |     - | 107.64 KB |
|                           |               |              |            |            |       |       |       |       |           |
|     VwSalesByYearMutation |    .NET 4.7.2 |       Access |   387.2 μs |   383.7 μs |  1.00 |     - |     - |     - |    136 KB |
|     VwSalesByYearMutation | .NET Core 2.1 |       Access |   482.6 μs |   481.8 μs |  1.24 |     - |     - |     - | 115.99 KB |
|     VwSalesByYearMutation | .NET Core 3.1 |       Access |   453.0 μs |   451.0 μs |  1.14 |     - |     - |     - | 111.41 KB |
|                           |               |              |            |            |       |       |       |       |           |
| VwSalesByCategoryContains |    .NET 4.7.2 |       Access | 1,102.3 μs | 1,083.1 μs |  1.00 |     - |     - |     - |  444.3 KB |
| VwSalesByCategoryContains | .NET Core 2.1 |       Access | 1,281.5 μs | 1,277.3 μs |  1.15 |     - |     - |     - | 415.65 KB |
| VwSalesByCategoryContains | .NET Core 3.1 |       Access | 1,603.1 μs | 1,606.2 μs |  1.44 |     - |     - |     - | 418.43 KB |
|                           |               |              |            |            |       |       |       |       |           |
|             **VwSalesByYear** |    **.NET 4.7.2** |     **Firebird** |   **397.2 μs** |   **382.4 μs** |  **1.00** |     **-** |     **-** |     **-** |    **128 KB** |
|             VwSalesByYear | .NET Core 2.1 |     Firebird |   456.9 μs |   455.5 μs |  1.10 |     - |     - |     - | 106.41 KB |
|             VwSalesByYear | .NET Core 3.1 |     Firebird |   435.2 μs |   435.4 μs |  1.04 |     - |     - |     - | 108.23 KB |
|                           |               |              |            |            |       |       |       |       |           |
|     VwSalesByYearMutation |    .NET 4.7.2 |     Firebird |   380.1 μs |   379.9 μs |  1.00 |     - |     - |     - |    136 KB |
|     VwSalesByYearMutation | .NET Core 2.1 |     Firebird |   477.5 μs |   476.4 μs |  1.26 |     - |     - |     - | 117.17 KB |
|     VwSalesByYearMutation | .NET Core 3.1 |     Firebird |   449.8 μs |   450.4 μs |  1.18 |     - |     - |     - | 112.59 KB |
|                           |               |              |            |            |       |       |       |       |           |
| VwSalesByCategoryContains |    .NET 4.7.2 |     Firebird |   804.9 μs |   795.7 μs |  1.00 |     - |     - |     - |    320 KB |
| VwSalesByCategoryContains | .NET Core 2.1 |     Firebird |   994.3 μs |   981.7 μs |  1.24 |     - |     - |     - | 295.92 KB |
| VwSalesByCategoryContains | .NET Core 3.1 |     Firebird | 1,226.6 μs | 1,212.0 μs |  1.52 |     - |     - |     - | 298.84 KB |
