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
|      Linq |    .NET 4.6.2 | 534.244 μs | 468.550 μs | 194.19 |      - |      - |     - |    112 KB |
|  Compiled |    .NET 4.6.2 | 313.985 μs | 301.639 μs | 113.05 |      - |      - |     - |    104 KB |
| RawAdoNet |    .NET 4.6.2 |   2.807 μs |   2.802 μs |   1.00 | 0.3700 | 0.1831 |     - |   1.52 KB |
|      Linq | .NET Core 2.1 | 578.506 μs | 478.351 μs | 212.95 |      - |      - |     - | 110.67 KB |
|  Compiled | .NET Core 2.1 | 386.835 μs | 358.982 μs | 140.13 |      - |      - |     - | 104.88 KB |
| RawAdoNet | .NET Core 2.1 |   2.179 μs |   2.140 μs |   0.80 | 0.3586 | 0.1793 |     - |   1.48 KB |
|      Linq | .NET Core 3.1 | 666.011 μs | 593.623 μs | 242.04 |      - |      - |     - | 109.51 KB |
|  Compiled | .NET Core 3.1 | 494.075 μs | 446.022 μs | 178.92 |      - |      - |     - | 103.79 KB |
| RawAdoNet | .NET Core 3.1 |   2.063 μs |   2.015 μs |   0.76 | 0.3548 | 0.1755 |     - |   1.46 KB |
