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
|    Method |       Runtime |     Mean | Ratio |     Gen 0 |    Gen 1 |    Gen 2 | Allocated |
|---------- |-------------- |---------:|------:|----------:|---------:|---------:|----------:|
|      Linq |    .NET 4.6.2 | 16.11 ms |  0.99 | 1000.0000 |        - |        - |   7.97 MB |
|  Compiled |    .NET 4.6.2 | 15.78 ms |  0.94 | 1000.0000 |        - |        - |   7.97 MB |
| RawAdoNet |    .NET 4.6.2 | 16.69 ms |  1.00 | 1468.7500 | 625.0000 | 203.1250 |   7.96 MB |
|      Linq | .NET Core 2.1 | 10.78 ms |  0.64 | 1000.0000 |        - |        - |   7.94 MB |
|  Compiled | .NET Core 2.1 | 10.64 ms |  0.64 | 1000.0000 |        - |        - |   7.94 MB |
| RawAdoNet | .NET Core 2.1 | 17.08 ms |  1.02 | 1437.5000 | 593.7500 | 187.5000 |   7.94 MB |
|      Linq | .NET Core 3.1 | 18.91 ms |  1.13 | 1437.5000 | 593.7500 | 187.5000 |   7.94 MB |
|  Compiled | .NET Core 3.1 | 19.04 ms |  1.14 | 1437.5000 | 593.7500 | 187.5000 |   7.94 MB |
| RawAdoNet | .NET Core 3.1 | 15.77 ms |  0.95 | 1437.5000 | 593.7500 | 187.5000 |   7.94 MB |
