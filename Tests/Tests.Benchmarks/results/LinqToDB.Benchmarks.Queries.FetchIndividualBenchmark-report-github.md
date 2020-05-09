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
|      Linq |    .NET 4.6.2 | 522.901 μs | 413.108 μs | 233.81 |      - |      - |     - |     32 KB |
|  Compiled |    .NET 4.6.2 | 326.203 μs | 271.504 μs | 145.07 |      - |      - |     - |     24 KB |
| RawAdoNet |    .NET 4.6.2 |   2.178 μs |   2.200 μs |   1.00 | 0.3700 | 0.1831 |     - |   1.52 KB |
|      Linq | .NET Core 2.1 | 436.468 μs | 394.968 μs | 210.00 |      - |      - |     - |   26.9 KB |
|  Compiled | .NET Core 2.1 | 290.607 μs | 259.363 μs | 132.75 |      - |      - |     - |  20.95 KB |
| RawAdoNet | .NET Core 2.1 |   2.171 μs |   2.171 μs |   1.00 | 0.3586 | 0.1793 |     - |   1.48 KB |
|      Linq | .NET Core 3.1 | 586.774 μs | 478.351 μs | 266.04 |      - |      - |     - |  26.55 KB |
|  Compiled | .NET Core 3.1 | 417.671 μs | 346.841 μs | 191.06 |      - |      - |     - |  20.83 KB |
| RawAdoNet | .NET Core 3.1 |   2.040 μs |   2.040 μs |   0.94 | 0.3548 | 0.1755 |     - |   1.46 KB |
