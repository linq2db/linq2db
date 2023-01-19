``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-UZBSVL : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-AYZXIO : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-NXXYQT : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-HMCTKM : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                    Method |              Runtime | DataProvider |       Mean |     Median | Ratio |    Gen0 |    Gen1 | Allocated | Alloc Ratio |
|-------------------------- |--------------------- |------------- |-----------:|-----------:|------:|--------:|--------:|----------:|------------:|
|             **VwSalesByYear** |             **.NET 6.0** |       **Access** |   **488.1 μs** |   **492.7 μs** |  **0.48** |  **8.7891** |  **0.9766** | **155.61 KB** |        **0.62** |
|             VwSalesByYear |             .NET 7.0 |       Access |   372.2 μs |   438.3 μs |  0.34 |  8.7891 |  1.4648 | 145.65 KB |        0.58 |
|             VwSalesByYear |        .NET Core 3.1 |       Access |   720.8 μs |   720.7 μs |  0.69 |  9.7656 |  0.9766 | 163.02 KB |        0.65 |
|             VwSalesByYear | .NET Framework 4.7.2 |       Access | 1,050.8 μs | 1,057.6 μs |  1.00 | 39.0625 |  5.8594 | 249.12 KB |        1.00 |
|                           |                      |              |            |            |       |         |         |           |             |
|     VwSalesByYearMutation |             .NET 6.0 |       Access |   828.3 μs |   828.1 μs |  0.63 | 15.6250 |  1.9531 | 258.33 KB |        0.92 |
|     VwSalesByYearMutation |             .NET 7.0 |       Access |   669.7 μs |   653.0 μs |  0.51 | 11.7188 |  1.9531 | 197.53 KB |        0.71 |
|     VwSalesByYearMutation |        .NET Core 3.1 |       Access | 1,058.8 μs | 1,064.7 μs |  0.81 | 11.7188 |  0.9766 | 202.31 KB |        0.72 |
|     VwSalesByYearMutation | .NET Framework 4.7.2 |       Access | 1,317.5 μs | 1,289.0 μs |  1.00 | 44.9219 |  5.8594 | 279.94 KB |        1.00 |
|                           |                      |              |            |            |       |         |         |           |             |
| VwSalesByCategoryContains |             .NET 6.0 |       Access | 1,683.0 μs | 1,658.9 μs |  0.63 | 27.3438 |  3.9063 |  484.9 KB |        1.04 |
| VwSalesByCategoryContains |             .NET 7.0 |       Access | 1,524.2 μs | 1,522.5 μs |  0.57 | 27.3438 |  7.8125 | 454.64 KB |        0.97 |
| VwSalesByCategoryContains |        .NET Core 3.1 |       Access | 2,007.1 μs | 2,023.7 μs |  0.76 | 27.3438 |  3.9063 | 483.85 KB |        1.03 |
| VwSalesByCategoryContains | .NET Framework 4.7.2 |       Access | 2,654.5 μs | 2,667.5 μs |  1.00 | 74.2188 | 15.6250 | 467.58 KB |        1.00 |
|                           |                      |              |            |            |       |         |         |           |             |
|             **VwSalesByYear** |             **.NET 6.0** |     **Firebird** |   **530.1 μs** |   **529.9 μs** |  **0.55** |  **8.7891** |  **0.9766** | **156.24 KB** |        **0.63** |
|             VwSalesByYear |             .NET 7.0 |     Firebird |   470.9 μs |   471.8 μs |  0.49 | 10.7422 |  1.9531 | 182.07 KB |        0.73 |
|             VwSalesByYear |        .NET Core 3.1 |     Firebird |   746.9 μs |   744.8 μs |  0.78 |  9.7656 |  0.9766 | 163.65 KB |        0.65 |
|             VwSalesByYear | .NET Framework 4.7.2 |     Firebird |   962.6 μs |   958.7 μs |  1.00 | 40.0391 |  7.8125 | 249.87 KB |        1.00 |
|                           |                      |              |            |            |       |         |         |           |             |
|     VwSalesByYearMutation |             .NET 6.0 |     Firebird |   386.1 μs |   384.7 μs |  0.27 | 15.6250 |  1.9531 | 261.49 KB |        0.92 |
|     VwSalesByYearMutation |             .NET 7.0 |     Firebird |   673.2 μs |   660.4 μs |  0.46 | 10.7422 |  0.9766 |  187.1 KB |        0.66 |
|     VwSalesByYearMutation |        .NET Core 3.1 |     Firebird |   884.3 μs | 1,081.8 μs |  0.67 | 11.7188 |       - | 205.49 KB |        0.73 |
|     VwSalesByYearMutation | .NET Framework 4.7.2 |     Firebird | 1,462.6 μs | 1,482.8 μs |  1.00 | 44.9219 |  5.8594 | 283.33 KB |        1.00 |
|                           |                      |              |            |            |       |         |         |           |             |
| VwSalesByCategoryContains |             .NET 6.0 |     Firebird | 1,332.5 μs | 1,339.3 μs |  0.64 | 23.4375 |  5.8594 | 413.06 KB |        1.05 |
| VwSalesByCategoryContains |             .NET 7.0 |     Firebird | 1,157.9 μs | 1,157.9 μs |  0.55 | 21.4844 |  3.9063 | 375.27 KB |        0.95 |
| VwSalesByCategoryContains |        .NET Core 3.1 |     Firebird | 1,576.3 μs | 1,645.9 μs |  0.75 | 23.4375 |  3.9063 | 413.11 KB |        1.05 |
| VwSalesByCategoryContains | .NET Framework 4.7.2 |     Firebird | 2,093.7 μs | 2,078.0 μs |  1.00 | 62.5000 | 11.7188 | 394.42 KB |        1.00 |
