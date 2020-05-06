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
|      Linq |    .NET 4.6.2 | 487.204 μs | 375.513 μs | 231.11 |      - |      - |     - |     40 KB |
|  Compiled |    .NET 4.6.2 | 329.719 μs | 306.613 μs | 155.88 |      - |      - |     - |     32 KB |
| RawAdoNet |    .NET 4.6.2 |   2.125 μs |   2.117 μs |   1.00 | 0.3700 | 0.1831 |     - |   1.52 KB |
|      Linq | .NET Core 2.1 | 405.765 μs | 365.126 μs | 191.78 |      - |      - |     - |  31.68 KB |
|  Compiled | .NET Core 2.1 | 283.649 μs | 252.487 μs | 134.04 |      - |      - |     - |  25.72 KB |
| RawAdoNet | .NET Core 2.1 |   2.146 μs |   2.168 μs |   1.01 | 0.3586 | 0.1793 |     - |   1.48 KB |
|      Linq | .NET Core 3.1 | 549.369 μs | 477.765 μs | 259.14 |      - |      - |     - |  30.85 KB |
|  Compiled | .NET Core 3.1 | 335.165 μs | 317.584 μs | 158.21 |      - |      - |     - |  25.13 KB |
| RawAdoNet | .NET Core 3.1 |   2.512 μs |   2.619 μs |   1.18 | 0.3548 | 0.1755 |     - |   1.46 KB |
