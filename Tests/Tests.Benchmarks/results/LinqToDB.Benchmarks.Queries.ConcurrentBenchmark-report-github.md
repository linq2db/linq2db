``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-TEPEZT : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-ISYUTK : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-SMHCKK : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-DHDWVI : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|   Method |              Runtime | ThreadCount |     Mean |   Median | Ratio |     Gen0 |    Gen1 |  Allocated | Alloc Ratio |
|--------- |--------------------- |------------ |---------:|---------:|------:|---------:|--------:|-----------:|------------:|
|     **Linq** |             **.NET 6.0** |          **16** | **17.16 ms** | **16.89 ms** |  **1.04** |        **-** |       **-** |  **446.29 KB** |        **1.52** |
| Compiled |             .NET 6.0 |          16 | 16.37 ms | 16.45 ms |  0.99 |        - |       - |  276.43 KB |        0.94 |
|     Linq |             .NET 7.0 |          16 | 16.64 ms | 16.54 ms |  1.01 |        - |       - |  375.79 KB |        1.28 |
| Compiled |             .NET 7.0 |          16 | 18.93 ms | 17.58 ms |  1.15 |        - |       - |   275.6 KB |        0.94 |
|     Linq |        .NET Core 3.1 |          16 | 16.58 ms | 16.38 ms |  1.02 |        - |       - |  444.81 KB |        1.51 |
| Compiled |        .NET Core 3.1 |          16 | 16.29 ms | 16.16 ms |  0.99 |        - |       - |  275.18 KB |        0.94 |
|     Linq | .NET Framework 4.7.2 |          16 | 16.39 ms | 16.46 ms |  1.00 |  62.5000 |       - |  473.25 KB |        1.61 |
| Compiled | .NET Framework 4.7.2 |          16 | 16.46 ms | 16.48 ms |  1.00 |  31.2500 |       - |  293.76 KB |        1.00 |
|          |                      |             |          |          |       |          |         |            |             |
|     **Linq** |             **.NET 6.0** |          **32** | **16.92 ms** | **16.88 ms** |  **1.02** |  **31.2500** |       **-** |  **894.88 KB** |        **1.50** |
| Compiled |             .NET 6.0 |          32 | 16.20 ms | 15.99 ms |  0.98 |  31.2500 |       - |  553.33 KB |        0.92 |
|     Linq |             .NET 7.0 |          32 | 16.30 ms | 16.38 ms |  0.98 |  31.2500 |       - |  757.05 KB |        1.27 |
| Compiled |             .NET 7.0 |          32 | 16.97 ms | 16.83 ms |  1.03 |  31.2500 |       - |  552.88 KB |        0.92 |
|     Linq |        .NET Core 3.1 |          32 | 16.26 ms | 16.10 ms |  0.98 |  46.8750 |       - |  891.74 KB |        1.49 |
| Compiled |        .NET Core 3.1 |          32 | 16.23 ms | 16.16 ms |  0.98 |  31.2500 |       - |  553.23 KB |        0.92 |
|     Linq | .NET Framework 4.7.2 |          32 | 15.49 ms | 15.46 ms |  0.94 | 156.2500 |       - |  960.79 KB |        1.61 |
| Compiled | .NET Framework 4.7.2 |          32 | 16.65 ms | 16.41 ms |  1.00 |  93.7500 |       - |  598.26 KB |        1.00 |
|          |                      |             |          |          |       |          |         |            |             |
|     **Linq** |             **.NET 6.0** |          **64** | **16.51 ms** | **16.39 ms** |  **1.01** |  **93.7500** |       **-** | **1784.34 KB** |        **1.48** |
| Compiled |             .NET 6.0 |          64 | 16.72 ms | 16.47 ms |  1.01 |  62.5000 |       - | 1102.89 KB |        0.91 |
|     Linq |             .NET 7.0 |          64 | 15.95 ms | 15.95 ms |  0.97 |  93.7500 |       - | 1504.47 KB |        1.25 |
| Compiled |             .NET 7.0 |          64 | 17.56 ms | 16.90 ms |  1.04 |  62.5000 |       - | 1096.99 KB |        0.91 |
|     Linq |        .NET Core 3.1 |          64 | 15.74 ms | 15.65 ms |  0.95 | 109.3750 |       - | 1777.28 KB |        1.47 |
| Compiled |        .NET Core 3.1 |          64 | 17.00 ms | 16.63 ms |  1.04 |  62.5000 |       - | 1094.14 KB |        0.91 |
|     Linq | .NET Framework 4.7.2 |          64 | 15.86 ms | 15.46 ms |  0.96 | 312.5000 | 62.5000 | 1945.78 KB |        1.61 |
| Compiled | .NET Framework 4.7.2 |          64 | 16.57 ms | 16.53 ms |  1.00 | 187.5000 | 31.2500 | 1206.17 KB |        1.00 |
