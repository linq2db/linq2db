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
|      Linq |    .NET 4.6.2 | 23.22 ms | 22.79 ms |  1.31 | 1000.0000 |        - |        - |   7.97 MB |
|  Compiled |    .NET 4.6.2 | 19.33 ms | 18.74 ms |  1.08 | 1000.0000 |        - |        - |   7.97 MB |
| RawAdoNet |    .NET 4.6.2 | 17.85 ms | 17.68 ms |  1.00 | 1437.5000 | 593.7500 | 187.5000 |   7.96 MB |
|      Linq | .NET Core 2.1 | 13.80 ms | 13.34 ms |  0.77 | 1000.0000 |        - |        - |   7.94 MB |
|  Compiled | .NET Core 2.1 | 12.17 ms | 12.13 ms |  0.68 | 1000.0000 |        - |        - |   7.94 MB |
| RawAdoNet | .NET Core 2.1 | 18.56 ms | 18.48 ms |  1.03 | 1437.5000 | 593.7500 | 187.5000 |   7.94 MB |
|      Linq | .NET Core 3.1 | 12.02 ms | 12.03 ms |  0.67 | 1000.0000 |        - |        - |   7.94 MB |
|  Compiled | .NET Core 3.1 | 11.17 ms | 10.79 ms |  0.63 | 1000.0000 |        - |        - |   7.94 MB |
| RawAdoNet | .NET Core 3.1 | 17.09 ms | 17.12 ms |  0.94 | 1437.5000 | 593.7500 | 187.5000 |   7.94 MB |
