``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417995 Hz, Resolution=292.5692 ns, Timer=TSC
  [Host]     : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-OGAWJV : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-ZLSLVN : .NET Core 2.1.17 (CoreCLR 4.6.28619.01, CoreFX 4.6.28619.01), X64 RyuJIT
  Job-IOHEYN : .NET Core 3.1.3 (CoreCLR 4.700.20.11803, CoreFX 4.700.20.12001), X64 RyuJIT

Jit=RyuJit  Platform=X64  

```
|    Method |       Runtime |       Mean |     Median |  Ratio |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|---------- |-------------- |-----------:|-----------:|-------:|-------:|-------:|------:|----------:|
|      Linq |    .NET 4.6.2 | 239.892 μs | 227.765 μs |  93.53 |      - |      - |     - |   16384 B |
|  Compiled |    .NET 4.6.2 |  57.107 μs |  50.322 μs |  21.75 |      - |      - |     - |         - |
| RawAdoNet |    .NET 4.6.2 |   2.586 μs |   2.588 μs |   1.00 | 0.3700 | 0.1831 |     - |    1557 B |
|      Linq | .NET Core 2.1 | 232.660 μs | 193.973 μs |  85.81 |      - |      - |     - |    8312 B |
|  Compiled | .NET Core 2.1 |  74.417 μs |  66.267 μs |  29.01 |      - |      - |     - |         - |
| RawAdoNet | .NET Core 2.1 |   2.355 μs |   2.390 μs |   0.93 | 0.3586 | 0.1793 |     - |    1512 B |
|      Linq | .NET Core 3.1 | 288.539 μs | 224.986 μs | 115.67 |      - |      - |     - |         - |
|  Compiled | .NET Core 3.1 |  67.730 μs |  53.540 μs |  25.61 |      - |      - |     - |         - |
| RawAdoNet | .NET Core 3.1 |   1.783 μs |   1.784 μs |   0.70 | 0.3567 | 0.1774 |     - |    1496 B |
