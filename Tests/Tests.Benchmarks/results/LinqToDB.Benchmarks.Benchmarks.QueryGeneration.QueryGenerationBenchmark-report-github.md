``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-WUZRIO : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-EMBONI : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-HZWTXS : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-VIGHHX : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                    Method |              Runtime | DataProvider |       Mean |     Median | Ratio |    Gen0 |   Gen1 | Allocated | Alloc Ratio |
|-------------------------- |--------------------- |------------- |-----------:|-----------:|------:|--------:|-------:|----------:|------------:|
|             **VwSalesByYear** |             **.NET 6.0** |       **Access** |   **551.4 μs** |   **547.2 μs** |  **0.53** |  **4.8828** |      **-** |  **91.72 KB** |        **0.68** |
|             VwSalesByYear |             .NET 7.0 |       Access |   452.4 μs |   450.6 μs |  0.43 |  3.9063 |      - |  75.33 KB |        0.56 |
|             VwSalesByYear |        .NET Core 3.1 |       Access |   727.2 μs |   727.0 μs |  0.69 |  4.8828 |      - |  91.71 KB |        0.68 |
|             VwSalesByYear | .NET Framework 4.7.2 |       Access | 1,052.4 μs | 1,051.1 μs |  1.00 | 21.4844 |      - | 134.33 KB |        1.00 |
|                           |                      |              |            |            |       |         |        |           |             |
|     VwSalesByYearMutation |             .NET 6.0 |       Access |   855.3 μs |   854.6 μs |  0.59 |  7.8125 |      - | 144.94 KB |        0.73 |
|     VwSalesByYearMutation |             .NET 7.0 |       Access |   571.6 μs |   706.9 μs |  0.33 |  7.3242 | 0.4883 | 125.69 KB |        0.64 |
|     VwSalesByYearMutation |        .NET Core 3.1 |       Access |   731.5 μs |   640.8 μs |  0.53 |  7.8125 |      - | 143.86 KB |        0.73 |
|     VwSalesByYearMutation | .NET Framework 4.7.2 |       Access | 1,454.2 μs | 1,462.0 μs |  1.00 | 31.2500 |      - | 197.55 KB |        1.00 |
|                           |                      |              |            |            |       |         |        |           |             |
| VwSalesByCategoryContains |             .NET 6.0 |       Access | 1,620.5 μs | 1,609.0 μs |  0.61 | 15.6250 |      - | 272.22 KB |        0.79 |
| VwSalesByCategoryContains |             .NET 7.0 |       Access | 1,423.2 μs | 1,463.2 μs |  0.46 | 13.6719 |      - |  250.9 KB |        0.72 |
| VwSalesByCategoryContains |        .NET Core 3.1 |       Access | 2,181.0 μs | 2,178.2 μs |  0.81 | 15.6250 |      - | 277.09 KB |        0.80 |
| VwSalesByCategoryContains | .NET Framework 4.7.2 |       Access | 2,686.7 μs | 2,704.0 μs |  1.00 | 54.6875 | 3.9063 | 346.52 KB |        1.00 |
|                           |                      |              |            |            |       |         |        |           |             |
|             **VwSalesByYear** |             **.NET 6.0** |     **Firebird** |   **536.4 μs** |   **534.5 μs** |  **0.51** |  **4.8828** |      **-** |  **92.36 KB** |        **0.68** |
|             VwSalesByYear |             .NET 7.0 |     Firebird |   452.0 μs |   452.0 μs |  0.43 |  3.9063 |      - |  75.08 KB |        0.56 |
|             VwSalesByYear |        .NET Core 3.1 |     Firebird |   731.4 μs |   734.9 μs |  0.70 |  4.8828 |      - |  92.35 KB |        0.68 |
|             VwSalesByYear | .NET Framework 4.7.2 |     Firebird | 1,048.3 μs | 1,043.3 μs |  1.00 | 21.4844 |      - |  135.1 KB |        1.00 |
|                           |                      |              |            |            |       |         |        |           |             |
|     VwSalesByYearMutation |             .NET 6.0 |     Firebird |   837.8 μs |   830.0 μs |  0.69 |  7.8125 |      - | 148.12 KB |        0.74 |
|     VwSalesByYearMutation |             .NET 7.0 |     Firebird |   760.1 μs |   761.1 μs |  0.62 |  7.8125 |      - | 128.59 KB |        0.64 |
|     VwSalesByYearMutation |        .NET Core 3.1 |     Firebird | 1,104.3 μs | 1,103.7 μs |  0.91 |  7.8125 |      - | 147.04 KB |        0.73 |
|     VwSalesByYearMutation | .NET Framework 4.7.2 |     Firebird | 1,404.5 μs | 1,484.2 μs |  1.00 | 31.2500 |      - | 200.95 KB |        1.00 |
|                           |                      |              |            |            |       |         |        |           |             |
| VwSalesByCategoryContains |             .NET 6.0 |     Firebird | 1,229.2 μs | 1,216.0 μs |  0.56 | 11.7188 |      - | 200.49 KB |        0.74 |
| VwSalesByCategoryContains |             .NET 7.0 |     Firebird | 1,105.5 μs | 1,100.1 μs |  0.50 |  9.7656 |      - | 178.64 KB |        0.66 |
| VwSalesByCategoryContains |        .NET Core 3.1 |     Firebird | 1,699.9 μs | 1,681.0 μs |  0.78 | 11.7188 |      - | 205.34 KB |        0.75 |
| VwSalesByCategoryContains | .NET Framework 4.7.2 |     Firebird | 2,204.4 μs | 2,216.4 μs |  1.00 | 42.9688 |      - | 272.66 KB |        1.00 |
