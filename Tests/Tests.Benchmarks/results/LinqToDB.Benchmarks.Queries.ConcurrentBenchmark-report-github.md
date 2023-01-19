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
|   Method |              Runtime | ThreadCount |     Mean |   Median | Ratio |     Gen0 |    Gen1 |  Allocated | Alloc Ratio |
|--------- |--------------------- |------------ |---------:|---------:|------:|---------:|--------:|-----------:|------------:|
|     **Linq** |             **.NET 6.0** |          **16** | **17.33 ms** | **17.05 ms** |  **1.04** |        **-** |       **-** |  **480.92 KB** |        **1.49** |
| Compiled |             .NET 6.0 |          16 | 18.69 ms | 18.55 ms |  1.13 |        - |       - |  302.19 KB |        0.94 |
|     Linq |             .NET 7.0 |          16 | 17.25 ms | 17.06 ms |  1.04 |        - |       - |  412.67 KB |        1.28 |
| Compiled |             .NET 7.0 |          16 | 20.91 ms | 20.71 ms |  1.20 |        - |       - |  301.44 KB |        0.93 |
|     Linq |        .NET Core 3.1 |          16 | 17.37 ms | 17.13 ms |  1.05 |        - |       - |  478.89 KB |        1.48 |
| Compiled |        .NET Core 3.1 |          16 | 25.03 ms | 21.36 ms |  1.54 |        - |       - |  300.14 KB |        0.93 |
|     Linq | .NET Framework 4.7.2 |          16 | 16.48 ms | 16.41 ms |  0.99 |  62.5000 |       - |   522.5 KB |        1.62 |
| Compiled | .NET Framework 4.7.2 |          16 | 16.59 ms | 16.41 ms |  1.00 |  31.2500 |       - |   322.5 KB |        1.00 |
|          |                      |             |          |          |       |          |         |            |             |
|     **Linq** |             **.NET 6.0** |          **32** | **21.02 ms** | **21.10 ms** |  **1.07** |  **31.2500** |       **-** |  **962.14 KB** |        **1.48** |
| Compiled |             .NET 6.0 |          32 | 24.65 ms | 24.78 ms |  1.26 |  31.2500 |       - |  604.39 KB |        0.93 |
|     Linq |             .NET 7.0 |          32 | 21.41 ms | 22.08 ms |  1.11 |  31.2500 |       - |  825.79 KB |        1.27 |
| Compiled |             .NET 7.0 |          32 | 23.98 ms | 23.53 ms |  1.23 |  31.2500 |       - |  602.09 KB |        0.92 |
|     Linq |        .NET Core 3.1 |          32 | 23.92 ms | 23.30 ms |  1.24 |        - |       - |  958.61 KB |        1.47 |
| Compiled |        .NET Core 3.1 |          32 | 25.91 ms | 25.89 ms |  1.32 |  31.2500 |       - |  601.21 KB |        0.92 |
|     Linq | .NET Framework 4.7.2 |          32 | 16.12 ms | 16.26 ms |  0.88 | 156.2500 |       - | 1056.51 KB |        1.62 |
| Compiled | .NET Framework 4.7.2 |          32 | 20.14 ms | 20.62 ms |  1.00 |  93.7500 |       - |  651.76 KB |        1.00 |
|          |                      |             |          |          |       |          |         |            |             |
|     **Linq** |             **.NET 6.0** |          **64** | **18.37 ms** | **17.82 ms** |  **0.87** |  **93.7500** |       **-** | **1924.95 KB** |        **1.46** |
| Compiled |             .NET 6.0 |          64 | 20.30 ms | 20.44 ms |  0.97 |  62.5000 |       - | 1210.71 KB |        0.92 |
|     Linq |             .NET 7.0 |          64 | 18.90 ms | 18.68 ms |  0.90 |  62.5000 |       - | 1649.94 KB |        1.25 |
| Compiled |             .NET 7.0 |          64 | 20.42 ms | 19.77 ms |  0.97 |  62.5000 |       - | 1203.87 KB |        0.91 |
|     Linq |        .NET Core 3.1 |          64 | 19.12 ms | 18.20 ms |  0.91 |  93.7500 |       - | 1920.73 KB |        1.45 |
| Compiled |        .NET Core 3.1 |          64 | 21.93 ms | 22.13 ms |  1.04 |  62.5000 |       - | 1202.24 KB |        0.91 |
|     Linq | .NET Framework 4.7.2 |          64 | 15.75 ms | 15.70 ms |  0.74 | 343.7500 | 62.5000 | 2193.78 KB |        1.66 |
| Compiled | .NET Framework 4.7.2 |          64 | 21.18 ms | 20.97 ms |  1.00 | 187.5000 | 31.2500 | 1322.02 KB |        1.00 |
