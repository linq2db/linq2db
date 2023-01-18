``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-XCPGVR : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-RHOQGE : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-WEVYVV : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-ORXRGX : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                    Method |              Runtime | DataProvider |       Mean |     Median | Ratio |    Gen0 |   Gen1 | Allocated | Alloc Ratio |
|-------------------------- |--------------------- |------------- |-----------:|-----------:|------:|--------:|-------:|----------:|------------:|
|             **VwSalesByYear** |             **.NET 6.0** |       **Access** |   **547.4 μs** |   **549.4 μs** |  **0.53** |  **4.8828** |      **-** |  **91.72 KB** |        **0.68** |
|             VwSalesByYear |             .NET 7.0 |       Access |   442.0 μs |   451.8 μs |  0.42 |  3.9063 |      - |  75.45 KB |        0.56 |
|             VwSalesByYear |        .NET Core 3.1 |       Access |   714.3 μs |   713.6 μs |  0.69 |  4.8828 |      - |  91.72 KB |        0.68 |
|             VwSalesByYear | .NET Framework 4.7.2 |       Access | 1,040.5 μs | 1,045.2 μs |  1.00 | 21.4844 |      - | 134.33 KB |        1.00 |
|                           |                      |              |            |            |       |         |        |           |             |
|     VwSalesByYearMutation |             .NET 6.0 |       Access |   823.3 μs |   823.3 μs |  0.63 |  7.8125 |      - | 144.94 KB |        0.73 |
|     VwSalesByYearMutation |             .NET 7.0 |       Access |   709.7 μs |   710.6 μs |  0.54 |  6.8359 |      - | 126.01 KB |        0.64 |
|     VwSalesByYearMutation |        .NET Core 3.1 |       Access |   994.5 μs | 1,030.9 μs |  0.75 |  7.8125 |      - | 143.86 KB |        0.73 |
|     VwSalesByYearMutation | .NET Framework 4.7.2 |       Access | 1,330.0 μs | 1,290.2 μs |  1.00 | 31.2500 |      - | 197.55 KB |        1.00 |
|                           |                      |              |            |            |       |         |        |           |             |
| VwSalesByCategoryContains |             .NET 6.0 |       Access | 1,601.7 μs | 1,602.6 μs |  0.60 | 15.6250 |      - | 272.22 KB |        0.79 |
| VwSalesByCategoryContains |             .NET 7.0 |       Access | 1,455.1 μs | 1,442.2 μs |  0.55 | 11.7188 |      - | 250.73 KB |        0.72 |
| VwSalesByCategoryContains |        .NET Core 3.1 |       Access | 1,978.3 μs | 2,203.9 μs |  0.73 | 15.6250 |      - | 277.08 KB |        0.80 |
| VwSalesByCategoryContains | .NET Framework 4.7.2 |       Access | 2,658.8 μs | 2,660.1 μs |  1.00 | 54.6875 | 3.9063 | 346.52 KB |        1.00 |
|                           |                      |              |            |            |       |         |        |           |             |
|             **VwSalesByYear** |             **.NET 6.0** |     **Firebird** |   **541.9 μs** |   **542.2 μs** |  **0.52** |  **4.8828** |      **-** |  **92.36 KB** |        **0.68** |
|             VwSalesByYear |             .NET 7.0 |     Firebird |   431.9 μs |   432.6 μs |  0.42 |  4.3945 |      - |  76.28 KB |        0.56 |
|             VwSalesByYear |        .NET Core 3.1 |     Firebird |   726.4 μs |   728.9 μs |  0.70 |  4.8828 |      - |  92.35 KB |        0.68 |
|             VwSalesByYear | .NET Framework 4.7.2 |     Firebird | 1,037.6 μs | 1,038.1 μs |  1.00 | 21.4844 | 0.9766 | 135.09 KB |        1.00 |
|                           |                      |              |            |            |       |         |        |           |             |
|     VwSalesByYearMutation |             .NET 6.0 |     Firebird |   369.4 μs |   369.6 μs |  0.25 |  7.8125 |      - | 148.12 KB |        0.74 |
|     VwSalesByYearMutation |             .NET 7.0 |     Firebird |   667.4 μs |   707.4 μs |  0.44 |  7.8125 |      - | 128.94 KB |        0.64 |
|     VwSalesByYearMutation |        .NET Core 3.1 |     Firebird | 1,078.5 μs | 1,079.5 μs |  0.73 |  7.8125 |      - | 147.04 KB |        0.73 |
|     VwSalesByYearMutation | .NET Framework 4.7.2 |     Firebird | 1,477.8 μs | 1,480.2 μs |  1.00 | 31.2500 |      - | 200.95 KB |        1.00 |
|                           |                      |              |            |            |       |         |        |           |             |
| VwSalesByCategoryContains |             .NET 6.0 |     Firebird | 1,227.2 μs | 1,233.9 μs |  0.70 | 11.7188 |      - | 200.49 KB |        0.74 |
| VwSalesByCategoryContains |             .NET 7.0 |     Firebird | 1,088.7 μs | 1,091.0 μs |  0.62 |  9.7656 |      - | 178.62 KB |        0.66 |
| VwSalesByCategoryContains |        .NET Core 3.1 |     Firebird | 1,464.1 μs | 1,691.6 μs |  0.78 | 11.7188 |      - | 205.29 KB |        0.75 |
| VwSalesByCategoryContains | .NET Framework 4.7.2 |     Firebird | 2,000.5 μs | 2,120.3 μs |  1.00 | 42.9688 |      - | 272.66 KB |        1.00 |
