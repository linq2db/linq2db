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
|    Method |       Runtime |     Mean | Ratio |     Gen 0 |    Gen 1 |    Gen 2 | Allocated |
|---------- |-------------- |---------:|------:|----------:|---------:|---------:|----------:|
|      Linq |    .NET 4.6.2 | 17.96 ms |  1.06 | 1000.0000 |        - |        - |   7.97 MB |
|  Compiled |    .NET 4.6.2 | 18.12 ms |  1.08 | 1000.0000 |        - |        - |   7.97 MB |
| RawAdoNet |    .NET 4.6.2 | 16.98 ms |  1.00 | 1437.5000 | 593.7500 | 187.5000 |   7.96 MB |
|      Linq | .NET Core 2.1 | 11.81 ms |  0.69 | 1000.0000 |        - |        - |   7.94 MB |
|  Compiled | .NET Core 2.1 | 13.99 ms |  0.75 | 1000.0000 |        - |        - |   7.94 MB |
| RawAdoNet | .NET Core 2.1 | 19.02 ms |  1.12 | 1437.5000 | 593.7500 | 187.5000 |   7.94 MB |
|      Linq | .NET Core 3.1 | 12.24 ms |  0.78 | 1000.0000 |        - |        - |   7.94 MB |
|  Compiled | .NET Core 3.1 | 10.87 ms |  0.61 | 1000.0000 |        - |        - |   7.94 MB |
| RawAdoNet | .NET Core 3.1 | 17.84 ms |  1.05 | 1437.5000 | 593.7500 | 187.5000 |   7.94 MB |
