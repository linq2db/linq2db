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
|    Method |              Runtime |       Mean |  Ratio | Allocated |
|---------- |--------------------- |-----------:|-------:|----------:|
|      Linq |             .NET 5.0 |  31.304 μs |  20.74 |   7,418 B |
|  Compiled |             .NET 5.0 |  76.882 μs |  50.78 |         - |
| RawAdoNet |             .NET 5.0 |   1.179 μs |   0.78 |   1,520 B |
|      Linq |        .NET Core 3.1 |  36.927 μs |  24.31 |   7,570 B |
|  Compiled |        .NET Core 3.1 |   6.058 μs |   4.05 |   2,472 B |
| RawAdoNet |        .NET Core 3.1 |   1.397 μs |   0.92 |   1,520 B |
|      Linq | .NET Framework 4.7.2 | 154.736 μs | 101.59 |  16,384 B |
|  Compiled | .NET Framework 4.7.2 |  40.109 μs |  26.45 |         - |
| RawAdoNet | .NET Framework 4.7.2 |   1.525 μs |   1.00 |   1,581 B |
