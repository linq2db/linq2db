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
|    Method |              Runtime |     Mean | Ratio | Allocated |
|---------- |--------------------- |---------:|------:|----------:|
|      Linq |             .NET 5.0 | 15.22 ms |  0.89 |      8 MB |
|  Compiled |             .NET 5.0 | 16.30 ms |  0.94 |      8 MB |
| RawAdoNet |             .NET 5.0 | 15.59 ms |  0.88 |      8 MB |
|      Linq |        .NET Core 3.1 | 19.39 ms |  1.13 |      8 MB |
|  Compiled |        .NET Core 3.1 | 29.47 ms |  1.69 |      8 MB |
| RawAdoNet |        .NET Core 3.1 | 15.83 ms |  0.88 |      8 MB |
|      Linq | .NET Framework 4.7.2 | 27.06 ms |  1.52 |      8 MB |
|  Compiled | .NET Framework 4.7.2 | 28.79 ms |  1.60 |      8 MB |
| RawAdoNet | .NET Framework 4.7.2 | 17.19 ms |  1.00 |      8 MB |
