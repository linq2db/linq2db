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
|             **VwSalesByYear** |             **.NET 5.0** |       **Access** |   **267.1 μs** |   **263.5 μs** |  **0.36** |     **66 KB** |
|             VwSalesByYear |        .NET Core 3.1 |       Access |   373.7 μs |   373.9 μs |  0.49 |     80 KB |
|             VwSalesByYear | .NET Framework 4.7.2 |       Access |   771.8 μs |   720.3 μs |  1.00 |    104 KB |
|                           |                      |              |            |            |       |           |
|     VwSalesByYearMutation |             .NET 5.0 |       Access |   529.9 μs |   529.7 μs |  0.43 |    154 KB |
|     VwSalesByYearMutation |        .NET Core 3.1 |       Access |   659.0 μs |   653.7 μs |  0.54 |    171 KB |
|     VwSalesByYearMutation | .NET Framework 4.7.2 |       Access | 1,239.5 μs | 1,191.0 μs |  1.00 |    200 KB |
|                           |                      |              |            |            |       |           |
| VwSalesByCategoryContains |             .NET 5.0 |       Access | 1,155.8 μs | 1,155.8 μs |  0.56 |    283 KB |
| VwSalesByCategoryContains |        .NET Core 3.1 |       Access | 1,305.3 μs | 1,304.6 μs |  0.64 |    301 KB |
| VwSalesByCategoryContains | .NET Framework 4.7.2 |       Access | 2,118.2 μs | 2,107.8 μs |  1.00 |    348 KB |
|                           |                      |              |            |            |       |           |
|             **VwSalesByYear** |             **.NET 5.0** |     **Firebird** |   **260.0 μs** |   **260.5 μs** |  **0.31** |     **67 KB** |
|             VwSalesByYear |        .NET Core 3.1 |     Firebird |   387.0 μs |   387.3 μs |  0.47 |     81 KB |
|             VwSalesByYear | .NET Framework 4.7.2 |     Firebird |   767.8 μs |   720.6 μs |  1.00 |    104 KB |
|                           |                      |              |            |            |       |           |
|     VwSalesByYearMutation |             .NET 5.0 |     Firebird |   530.7 μs |   530.5 μs |  0.44 |    157 KB |
|     VwSalesByYearMutation |        .NET Core 3.1 |     Firebird |   681.6 μs |   682.9 μs |  0.55 |    174 KB |
|     VwSalesByYearMutation | .NET Framework 4.7.2 |     Firebird | 1,216.5 μs | 1,165.0 μs |  1.00 |    208 KB |
|                           |                      |              |            |            |       |           |
| VwSalesByCategoryContains |             .NET 5.0 |     Firebird |   761.2 μs |   762.0 μs |  0.48 |    204 KB |
| VwSalesByCategoryContains |        .NET Core 3.1 |     Firebird |   880.4 μs |   882.3 μs |  0.55 |    222 KB |
| VwSalesByCategoryContains | .NET Framework 4.7.2 |     Firebird | 1,701.3 μs | 1,694.1 μs |  1.00 |    272 KB |
