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
|      Linq |    .NET 4.6.2 | 466.719 μs | 403.160 μs | 202.71 |      - |      - |     - |     32 KB |
|  Compiled |    .NET 4.6.2 | 330.645 μs | 299.298 μs | 143.83 |      - |      - |     - |     24 KB |
| RawAdoNet |    .NET 4.6.2 |   2.312 μs |   2.312 μs |   1.00 | 0.3700 | 0.1831 |     - |   1.52 KB |
|      Linq | .NET Core 2.1 | 446.960 μs | 375.951 μs | 193.11 |      - |      - |     - |  27.56 KB |
|  Compiled | .NET Core 2.1 | 326.095 μs | 276.039 μs | 141.69 |      - |      - |     - |   21.2 KB |
| RawAdoNet | .NET Core 2.1 |   2.147 μs |   2.124 μs |   0.94 | 0.3586 | 0.1793 |     - |   1.48 KB |
|      Linq | .NET Core 3.1 | 573.353 μs | 476.888 μs | 248.94 |      - |      - |     - |   26.8 KB |
|  Compiled | .NET Core 3.1 | 362.422 μs | 328.116 μs | 157.42 |      - |      - |     - |  21.09 KB |
| RawAdoNet | .NET Core 3.1 |   2.169 μs |   2.153 μs |   0.95 | 0.3548 | 0.1755 |     - |   1.46 KB |
