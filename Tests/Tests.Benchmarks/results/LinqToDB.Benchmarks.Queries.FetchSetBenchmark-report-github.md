``` ini

BenchmarkDotNet=v0.12.1.1533-nightly, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417995 Hz, Resolution=292.5692 ns, Timer=TSC
  [Host]     : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-GUCTZK : .NET 5.0.5 (5.0.521.16609), X64 RyuJIT
  Job-IOHEYN : .NET Core 3.1.14 (CoreCLR 4.700.21.16201, CoreFX 4.700.21.16208), X64 RyuJIT
  Job-FWTWYQ : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT

Jit=RyuJit  Platform=X64  

```
|    Method |              Runtime |     Mean |   Median | Ratio | Allocated |
|---------- |--------------------- |---------:|---------:|------:|----------:|
|      Linq |             .NET 5.0 | 27.28 ms | 27.22 ms |  1.67 |      9 MB |
|  Compiled |             .NET 5.0 | 17.55 ms | 17.39 ms |  1.08 |      9 MB |
| RawAdoNet |             .NET 5.0 | 15.44 ms | 15.40 ms |  0.92 |      8 MB |
|      Linq |        .NET Core 3.1 | 21.52 ms | 21.25 ms |  1.33 |      9 MB |
|  Compiled |        .NET Core 3.1 | 18.97 ms | 18.60 ms |  1.17 |      9 MB |
| RawAdoNet |        .NET Core 3.1 | 17.89 ms | 17.89 ms |  1.09 |      8 MB |
|      Linq | .NET Framework 4.7.2 | 39.09 ms | 38.75 ms |  2.47 |      9 MB |
|  Compiled | .NET Framework 4.7.2 | 28.37 ms | 28.69 ms |  1.72 |      9 MB |
| RawAdoNet | .NET Framework 4.7.2 | 16.32 ms | 16.28 ms |  1.00 |      8 MB |
