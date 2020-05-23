``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417996 Hz, Resolution=292.5691 ns, Timer=TSC
  [Host]     : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-OGAWJV : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-ZLSLVN : .NET Core 2.1.18 (CoreCLR 4.6.28801.04, CoreFX 4.6.28802.05), X64 RyuJIT
  Job-IOHEYN : .NET Core 3.1.4 (CoreCLR 4.700.20.20201, CoreFX 4.700.20.22101), X64 RyuJIT

Jit=RyuJit  Platform=X64  

```
|    Method |       Runtime |       Mean |     Median |  Ratio |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|---------- |-------------- |-----------:|-----------:|-------:|-------:|-------:|------:|----------:|
|      Linq |    .NET 4.6.2 | 198.938 μs | 164.131 μs | 112.45 |      - |      - |     - |   16384 B |
|  Compiled |    .NET 4.6.2 |  42.014 μs |  39.497 μs |  23.79 |      - |      - |     - |         - |
| RawAdoNet |    .NET 4.6.2 |   1.785 μs |   1.805 μs |   1.00 | 0.3757 | 0.1869 |     - |    1581 B |
|      Linq | .NET Core 2.1 | 245.610 μs | 219.719 μs | 139.19 |      - |      - |     - |    8336 B |
|  Compiled | .NET Core 2.1 |  44.994 μs |  41.399 μs |  25.31 |      - |      - |     - |         - |
| RawAdoNet | .NET Core 2.1 |   1.575 μs |   1.545 μs |   0.89 | 0.3643 | 0.1812 |     - |    1536 B |
|      Linq | .NET Core 3.1 | 326.384 μs | 282.037 μs | 183.83 |      - |      - |     - |         - |
|  Compiled | .NET Core 3.1 |  82.044 μs |  62.464 μs |  46.11 |      - |      - |     - |         - |
| RawAdoNet | .NET Core 3.1 |   1.552 μs |   1.517 μs |   0.88 | 0.3624 | 0.1812 |     - |    1520 B |
