``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417994 Hz, Resolution=292.5693 ns, Timer=TSC
.NET Core SDK=5.0.101
  [Host]     : .NET Core 5.0.1 (CoreCLR 5.0.120.57516, CoreFX 5.0.120.57516), X64 RyuJIT
  Job-PRZVDA : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-SSEXHR : .NET Core 5.0.1 (CoreCLR 5.0.120.57516, CoreFX 5.0.120.57516), X64 RyuJIT
  Job-GGHGRH : .NET Core 3.1.10 (CoreCLR 4.700.20.51601, CoreFX 4.700.20.51901), X64 RyuJIT

Jit=RyuJit  Platform=X64  

```
|   Method |       Runtime | ThreadCount |       Mean |     Median | Ratio |  Allocated |
|--------- |-------------- |------------ |-----------:|-----------:|------:|-----------:|
|     **Linq** |    **.NET 4.7.2** |          **16** | **1,375.5 μs** | **1,333.8 μs** |  **2.22** |     **904 KB** |
| Compiled |    .NET 4.7.2 |          16 |   633.9 μs |   611.2 μs |  1.00 |     776 KB |
|     Linq |      .NET 5.0 |          16 | 1,527.6 μs | 1,498.1 μs |  2.47 |  795.25 KB |
| Compiled |      .NET 5.0 |          16 |   925.7 μs |   895.3 μs |  1.50 |   641.5 KB |
|     Linq | .NET Core 3.1 |          16 | 1,526.6 μs | 1,470.3 μs |  2.47 |  808.38 KB |
| Compiled | .NET Core 3.1 |          16 | 1,017.0 μs | 1,000.4 μs |  1.64 |     644 KB |
|          |               |             |            |            |       |            |
|     **Linq** |    **.NET 4.7.2** |          **32** | **2,460.2 μs** | **2,487.4 μs** |  **1.75** |    **1800 KB** |
| Compiled |    .NET 4.7.2 |          32 | 1,482.9 μs | 1,562.2 μs |  1.00 |    1544 KB |
|     Linq |      .NET 5.0 |          32 | 2,523.2 μs | 2,531.9 μs |  1.81 | 1583.14 KB |
| Compiled |      .NET 5.0 |          32 | 1,731.1 μs | 1,752.1 μs |  1.23 |    1283 KB |
|     Linq | .NET Core 3.1 |          32 | 3,054.4 μs | 3,029.0 μs |  2.18 | 1609.19 KB |
| Compiled | .NET Core 3.1 |          32 | 2,013.2 μs | 2,012.9 μs |  1.44 |    1288 KB |
|          |               |             |            |            |       |            |
|     **Linq** |    **.NET 4.7.2** |          **64** | **4,120.3 μs** | **4,096.3 μs** |  **2.02** |    **3592 KB** |
| Compiled |    .NET 4.7.2 |          64 | 2,071.1 μs | 1,979.8 μs |  1.00 |    3080 KB |
|     Linq |      .NET 5.0 |          64 | 3,980.4 μs | 4,039.2 μs |  1.95 | 3159.25 KB |
| Compiled |      .NET 5.0 |          64 | 2,505.3 μs | 2,481.9 μs |  1.22 |    2566 KB |
|     Linq | .NET Core 3.1 |          64 | 5,518.1 μs | 5,602.7 μs |  2.69 | 3208.77 KB |
| Compiled | .NET Core 3.1 |          64 | 3,472.6 μs | 3,502.5 μs |  1.70 |    2576 KB |
