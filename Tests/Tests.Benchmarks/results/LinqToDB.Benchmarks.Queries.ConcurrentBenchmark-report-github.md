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
|   Method |              Runtime | ThreadCount |     Mean |   Median | Ratio |     Gen0 |    Gen1 |  Allocated | Alloc Ratio |
|--------- |--------------------- |------------ |---------:|---------:|------:|---------:|--------:|-----------:|------------:|
|     **Linq** |             **.NET 6.0** |          **16** | **17.77 ms** | **17.11 ms** |  **1.07** |        **-** |       **-** |  **481.75 KB** |        **1.47** |
| Compiled |             .NET 6.0 |          16 | 17.94 ms | 17.66 ms |  1.07 |        - |       - |  302.19 KB |        0.92 |
|     Linq |             .NET 7.0 |          16 | 19.76 ms | 18.14 ms |  1.16 |        - |       - |  429.52 KB |        1.31 |
| Compiled |             .NET 7.0 |          16 | 27.72 ms | 24.76 ms |  1.66 |        - |       - |  301.18 KB |        0.92 |
|     Linq |        .NET Core 3.1 |          16 | 17.15 ms | 16.94 ms |  1.03 |        - |       - |  478.68 KB |        1.46 |
| Compiled |        .NET Core 3.1 |          16 | 30.01 ms | 20.37 ms |  1.80 |        - |       - |  300.28 KB |        0.92 |
|     Linq | .NET Framework 4.7.2 |          16 | 19.22 ms | 16.46 ms |  1.16 |  62.5000 |       - |     602 KB |        1.84 |
| Compiled | .NET Framework 4.7.2 |          16 | 16.74 ms | 16.60 ms |  1.00 |  31.2500 |       - |  327.75 KB |        1.00 |
|          |                      |             |          |          |       |          |         |            |             |
|     **Linq** |             **.NET 6.0** |          **32** | **23.72 ms** | **23.66 ms** |  **0.95** |  **31.2500** |       **-** |  **985.73 KB** |        **1.51** |
| Compiled |             .NET 6.0 |          32 | 29.35 ms | 29.32 ms |  1.17 |  31.2500 |       - |   604.4 KB |        0.93 |
|     Linq |             .NET 7.0 |          32 | 23.71 ms | 23.66 ms |  0.95 |  31.2500 |       - |  860.82 KB |        1.32 |
| Compiled |             .NET 7.0 |          32 | 25.40 ms | 26.37 ms |  1.02 |  31.2500 |       - |  604.59 KB |        0.93 |
|     Linq |        .NET Core 3.1 |          32 | 21.21 ms | 21.26 ms |  0.85 |  31.2500 |       - |  957.08 KB |        1.47 |
| Compiled |        .NET Core 3.1 |          32 | 34.38 ms | 26.87 ms |  1.38 |  31.2500 |       - |  600.68 KB |        0.92 |
|     Linq | .NET Framework 4.7.2 |          32 | 23.14 ms | 16.90 ms |  0.92 | 187.5000 | 31.2500 | 1215.53 KB |        1.87 |
| Compiled | .NET Framework 4.7.2 |          32 | 25.23 ms | 25.37 ms |  1.00 |  93.7500 |       - |  651.52 KB |        1.00 |
|          |                      |             |          |          |       |          |         |            |             |
|     **Linq** |             **.NET 6.0** |          **64** | **17.91 ms** | **17.83 ms** |  **0.88** |  **93.7500** |       **-** | **1925.98 KB** |        **1.46** |
| Compiled |             .NET 6.0 |          64 | 21.31 ms | 21.48 ms |  1.05 |  62.5000 |       - | 1209.31 KB |        0.92 |
|     Linq |             .NET 7.0 |          64 | 19.73 ms | 19.93 ms |  0.97 |  93.7500 | 15.6250 | 1718.97 KB |        1.30 |
| Compiled |             .NET 7.0 |          64 | 20.43 ms | 20.22 ms |  1.01 |  62.5000 |       - |    1204 KB |        0.91 |
|     Linq |        .NET Core 3.1 |          64 | 19.30 ms | 18.41 ms |  0.95 | 109.3750 |       - | 1944.79 KB |        1.47 |
| Compiled |        .NET Core 3.1 |          64 | 20.00 ms | 20.20 ms |  0.98 |  62.5000 |       - | 1202.27 KB |        0.91 |
|     Linq | .NET Framework 4.7.2 |          64 | 17.22 ms | 15.94 ms |  0.85 | 375.0000 | 62.5000 | 2356.53 KB |        1.78 |
| Compiled | .NET Framework 4.7.2 |          64 | 20.72 ms | 20.37 ms |  1.00 | 187.5000 | 31.2500 | 1320.77 KB |        1.00 |
