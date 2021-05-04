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
|      Linq |             .NET 5.0 | 15.93 ms | 15.57 ms |  0.88 |      8 MB |
|  Compiled |             .NET 5.0 | 16.19 ms | 16.15 ms |  0.95 |      8 MB |
| RawAdoNet |             .NET 5.0 | 14.03 ms | 13.83 ms |  0.80 |      8 MB |
|      Linq |        .NET Core 3.1 | 20.08 ms | 20.04 ms |  1.17 |      8 MB |
|  Compiled |        .NET Core 3.1 | 27.29 ms | 27.33 ms |  1.65 |      8 MB |
| RawAdoNet |        .NET Core 3.1 | 15.95 ms | 16.01 ms |  0.94 |      8 MB |
|      Linq | .NET Framework 4.7.2 | 27.21 ms | 27.23 ms |  1.65 |      8 MB |
|  Compiled | .NET Framework 4.7.2 | 25.97 ms | 25.42 ms |  1.43 |      8 MB |
| RawAdoNet | .NET Framework 4.7.2 | 18.27 ms | 18.09 ms |  1.00 |      8 MB |
