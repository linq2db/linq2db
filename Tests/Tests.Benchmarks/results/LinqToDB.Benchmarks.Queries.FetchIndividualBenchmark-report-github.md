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
|    Method |              Runtime |       Mean |     Median |  Ratio | Allocated |
|---------- |--------------------- |-----------:|-----------:|-------:|----------:|
|      Linq |             .NET 5.0 | 185.435 μs | 171.153 μs | 114.20 |   8,584 B |
|  Compiled |             .NET 5.0 |  60.484 μs |  57.490 μs |  37.27 |         - |
| RawAdoNet |             .NET 5.0 |   1.302 μs |   1.251 μs |   0.80 |   1,520 B |
|      Linq |        .NET Core 3.1 | 205.641 μs | 188.268 μs | 125.74 |   8,464 B |
|  Compiled |        .NET Core 3.1 |  48.570 μs |  30.427 μs |  29.59 |         - |
| RawAdoNet |        .NET Core 3.1 |   1.482 μs |   1.473 μs |   0.92 |   1,520 B |
|      Linq | .NET Framework 4.7.2 | 138.297 μs | 120.685 μs |  85.81 |  16,384 B |
|  Compiled | .NET Framework 4.7.2 |  31.338 μs |  26.185 μs |  19.40 |         - |
| RawAdoNet | .NET Framework 4.7.2 |   1.634 μs |   1.622 μs |   1.00 |   1,581 B |
