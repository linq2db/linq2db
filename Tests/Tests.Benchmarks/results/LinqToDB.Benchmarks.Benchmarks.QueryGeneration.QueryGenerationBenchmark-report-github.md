``` ini

BenchmarkDotNet=v0.12.1.1533-nightly, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417995 Hz, Resolution=292.5692 ns, Timer=TSC
  [Host]     : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-GUCTZK : .NET 5.0.5 (5.0.521.16609), X64 RyuJIT
  Job-IOHEYN : .NET Core 3.1.14 (CoreCLR 4.700.21.16201, CoreFX 4.700.21.16208), X64 RyuJIT
  Job-FWTWYQ : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT

Jit=RyuJit  Platform=X64  

```
|                    Method |              Runtime | DataProvider |       Mean |     Median | Ratio | Allocated |
|-------------------------- |--------------------- |------------- |-----------:|-----------:|------:|----------:|
|             **VwSalesByYear** |             **.NET 5.0** |       **Access** |   **283.3 μs** |   **278.2 μs** |  **0.37** |     **67 KB** |
|             VwSalesByYear |        .NET Core 3.1 |       Access |   399.1 μs |   399.0 μs |  0.50 |     80 KB |
|             VwSalesByYear | .NET Framework 4.7.2 |       Access |   770.3 μs |   769.3 μs |  1.00 |    104 KB |
|                           |                      |              |            |            |       |           |
|     VwSalesByYearMutation |             .NET 5.0 |       Access |   556.5 μs |   556.7 μs |  0.46 |    148 KB |
|     VwSalesByYearMutation |        .NET Core 3.1 |       Access |   687.4 μs |   686.4 μs |  0.57 |    160 KB |
|     VwSalesByYearMutation | .NET Framework 4.7.2 |       Access | 1,237.7 μs | 1,168.2 μs |  1.00 |    192 KB |
|                           |                      |              |            |            |       |           |
| VwSalesByCategoryContains |             .NET 5.0 |       Access | 1,246.3 μs | 1,225.6 μs |  0.54 |    272 KB |
| VwSalesByCategoryContains |        .NET Core 3.1 |       Access | 1,305.8 μs | 1,304.6 μs |  0.56 |    293 KB |
| VwSalesByCategoryContains | .NET Framework 4.7.2 |       Access | 2,298.7 μs | 2,291.7 μs |  1.00 |    340 KB |
|                           |                      |              |            |            |       |           |
|             **VwSalesByYear** |             **.NET 5.0** |     **Firebird** |   **280.1 μs** |   **279.9 μs** |  **0.42** |     **68 KB** |
|             VwSalesByYear |        .NET Core 3.1 |     Firebird |   382.9 μs |   383.3 μs |  0.57 |     80 KB |
|             VwSalesByYear | .NET Framework 4.7.2 |     Firebird |   723.3 μs |   724.3 μs |  1.00 |    104 KB |
|                           |                      |              |            |            |       |           |
|     VwSalesByYearMutation |             .NET 5.0 |     Firebird |   593.6 μs |   594.7 μs |  0.55 |    152 KB |
|     VwSalesByYearMutation |        .NET Core 3.1 |     Firebird |   736.8 μs |   726.0 μs |  0.68 |    163 KB |
|     VwSalesByYearMutation | .NET Framework 4.7.2 |     Firebird | 1,102.3 μs | 1,072.0 μs |  1.00 |    192 KB |
|                           |                      |              |            |            |       |           |
| VwSalesByCategoryContains |             .NET 5.0 |     Firebird |   793.6 μs |   793.6 μs |  0.46 |    194 KB |
| VwSalesByCategoryContains |        .NET Core 3.1 |     Firebird |   922.5 μs |   933.5 μs |  0.53 |    212 KB |
| VwSalesByCategoryContains | .NET Framework 4.7.2 |     Firebird | 1,775.6 μs | 1,747.4 μs |  1.00 |    264 KB |
