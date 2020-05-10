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
|    Method |       Runtime |     Mean |   Median | Ratio |     Gen 0 |    Gen 1 |    Gen 2 | Allocated |
|---------- |-------------- |---------:|---------:|------:|----------:|---------:|---------:|----------:|
|      Linq |    .NET 4.6.2 | 20.57 ms | 20.16 ms |  1.23 | 1000.0000 |        - |        - |  10.38 MB |
|  Compiled |    .NET 4.6.2 | 20.85 ms | 20.40 ms |  1.21 | 1000.0000 |        - |        - |  10.38 MB |
| RawAdoNet |    .NET 4.6.2 | 16.90 ms | 16.77 ms |  1.00 | 1437.5000 | 593.7500 | 187.5000 |   7.96 MB |
|      Linq | .NET Core 2.1 | 13.29 ms | 13.06 ms |  0.79 | 1000.0000 |        - |        - |  10.34 MB |
|  Compiled | .NET Core 2.1 | 13.45 ms | 13.19 ms |  0.80 | 1000.0000 |        - |        - |  10.34 MB |
| RawAdoNet | .NET Core 2.1 | 18.30 ms | 18.28 ms |  1.10 | 1437.5000 | 593.7500 | 187.5000 |   7.94 MB |
|      Linq | .NET Core 3.1 | 15.11 ms | 14.15 ms |  0.98 | 1000.0000 |        - |        - |  10.34 MB |
|  Compiled | .NET Core 3.1 | 13.82 ms | 13.50 ms |  0.85 | 1000.0000 |        - |        - |  10.34 MB |
| RawAdoNet | .NET Core 3.1 | 16.85 ms | 16.81 ms |  0.99 | 1437.5000 | 593.7500 | 187.5000 |   7.94 MB |
