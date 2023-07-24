``` ini

BenchmarkDotNet=v0.13.5, OS=Windows 10 (10.0.17763.4010/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.201
  [Host]     : .NET 7.0.3 (7.0.323.6910), X64 RyuJIT AVX2
  Job-ZOLDKB : .NET 6.0.14 (6.0.1423.7309), X64 RyuJIT AVX2
  Job-EHWHZK : .NET 7.0.3 (7.0.323.6910), X64 RyuJIT AVX2
  Job-LWJRKG : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-AGOWOF : .NET Framework 4.8 (4.8.4614.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|   Method |              Runtime | ThreadCount |     Mean |  Allocated |
|--------- |--------------------- |------------ |---------:|-----------:|
|     **Linq** |             **.NET 6.0** |          **16** | **15.57 ms** |   **439.6 KB** |
| Compiled |             .NET 6.0 |          16 | 15.50 ms |  274.19 KB |
|     Linq |             .NET 7.0 |          16 | 15.46 ms |  375.15 KB |
| Compiled |             .NET 7.0 |          16 | 15.47 ms |  275.32 KB |
|     Linq |        .NET Core 3.1 |          16 | 15.48 ms |  442.32 KB |
| Compiled |        .NET Core 3.1 |          16 | 15.39 ms |  275.23 KB |
|     Linq | .NET Framework 4.7.2 |          16 | 15.51 ms |  473.13 KB |
| Compiled | .NET Framework 4.7.2 |          16 | 15.41 ms |     294 KB |
|          |                      |             |          |            |
|     **Linq** |             **.NET 6.0** |          **32** | **15.37 ms** |  **882.63 KB** |
| Compiled |             .NET 6.0 |          32 | 15.74 ms |  550.81 KB |
|     Linq |             .NET 7.0 |          32 | 15.75 ms |  754.87 KB |
| Compiled |             .NET 7.0 |          32 | 15.61 ms |  550.22 KB |
|     Linq |        .NET Core 3.1 |          32 | 15.60 ms |  887.67 KB |
| Compiled |        .NET Core 3.1 |          32 | 15.72 ms |  549.61 KB |
|     Linq | .NET Framework 4.7.2 |          32 | 15.51 ms |  961.02 KB |
| Compiled | .NET Framework 4.7.2 |          32 | 15.63 ms |  592.01 KB |
|          |                      |             |          |            |
|     **Linq** |             **.NET 6.0** |          **64** | **15.56 ms** | **1763.81 KB** |
| Compiled |             .NET 6.0 |          64 | 15.48 ms | 1098.95 KB |
|     Linq |             .NET 7.0 |          64 | 15.95 ms | 1506.81 KB |
| Compiled |             .NET 7.0 |          64 | 15.46 ms | 1096.77 KB |
|     Linq |        .NET Core 3.1 |          64 | 15.33 ms | 1795.93 KB |
| Compiled |        .NET Core 3.1 |          64 | 16.36 ms | 1094.19 KB |
|     Linq | .NET Framework 4.7.2 |          64 | 15.66 ms | 1944.92 KB |
| Compiled | .NET Framework 4.7.2 |          64 | 15.71 ms | 1206.54 KB |
