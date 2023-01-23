``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-HCNGBR : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-XBFFOD : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-INBZNN : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-THZJXI : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|   Method |              Runtime | ThreadCount |      Mean |  Allocated |
|--------- |--------------------- |------------ |----------:|-----------:|
|     **Linq** |             **.NET 6.0** |          **16** | **16.208 ms** |  **446.81 KB** |
| Compiled |             .NET 6.0 |          16 | 16.847 ms |  277.56 KB |
|     Linq |             .NET 7.0 |          16 | 17.700 ms |  377.48 KB |
| Compiled |             .NET 7.0 |          16 | 17.321 ms |  275.56 KB |
|     Linq |        .NET Core 3.1 |          16 | 17.015 ms |  442.24 KB |
| Compiled |        .NET Core 3.1 |          16 | 17.401 ms |  274.03 KB |
|     Linq | .NET Framework 4.7.2 |          16 | 16.027 ms |  481.26 KB |
| Compiled | .NET Framework 4.7.2 |          16 | 16.494 ms |     294 KB |
|          |                      |             |           |            |
|     **Linq** |             **.NET 6.0** |          **32** | **17.076 ms** |  **894.77 KB** |
| Compiled |             .NET 6.0 |          32 | 17.334 ms |  551.59 KB |
|     Linq |             .NET 7.0 |          32 | 16.132 ms |  757.59 KB |
| Compiled |             .NET 7.0 |          32 | 17.381 ms |  551.89 KB |
|     Linq |        .NET Core 3.1 |          32 | 17.093 ms |   891.3 KB |
| Compiled |        .NET Core 3.1 |          32 | 16.242 ms |  549.77 KB |
|     Linq | .NET Framework 4.7.2 |          32 | 15.547 ms |  960.02 KB |
| Compiled | .NET Framework 4.7.2 |          32 | 12.804 ms |  588.14 KB |
|          |                      |             |           |            |
|     **Linq** |             **.NET 6.0** |          **64** |  **2.235 ms** | **1772.29 KB** |
| Compiled |             .NET 6.0 |          64 |  2.216 ms | 1091.38 KB |
|     Linq |             .NET 7.0 |          64 |  2.276 ms | 1500.27 KB |
| Compiled |             .NET 7.0 |          64 |  2.325 ms | 1089.69 KB |
|     Linq |        .NET Core 3.1 |          64 |  2.520 ms | 1773.22 KB |
| Compiled |        .NET Core 3.1 |          64 |  2.426 ms | 1090.08 KB |
|     Linq | .NET Framework 4.7.2 |          64 |  7.243 ms | 2018.35 KB |
| Compiled | .NET Framework 4.7.2 |          64 |  2.679 ms | 1208.09 KB |
