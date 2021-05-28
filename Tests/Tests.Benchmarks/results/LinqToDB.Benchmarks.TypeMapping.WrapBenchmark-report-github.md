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
|                      Method |              Runtime |       Mean |  Ratio | Allocated |
|---------------------------- |--------------------- |-----------:|-------:|----------:|
|            TypeMapperString |             .NET 5.0 |   6.356 ns |   4.32 |         - |
|          DirectAccessString |             .NET 5.0 |   1.357 ns |   0.92 |         - |
|   TypeMapperWrappedInstance |             .NET 5.0 |  42.325 ns |  28.77 |      32 B |
| DirectAccessWrappedInstance |             .NET 5.0 |   1.071 ns |   0.73 |         - |
|     TypeMapperGetEnumerator |             .NET 5.0 |  74.157 ns |  51.57 |      32 B |
|   DirectAccessGetEnumerator |             .NET 5.0 |  67.677 ns |  46.02 |      32 B |
|            TypeMapperString |        .NET Core 3.1 |   5.659 ns |   3.84 |         - |
|          DirectAccessString |        .NET Core 3.1 |   1.042 ns |   0.71 |         - |
|   TypeMapperWrappedInstance |        .NET Core 3.1 |  47.594 ns |  32.92 |      32 B |
| DirectAccessWrappedInstance |        .NET Core 3.1 |   1.102 ns |   0.75 |         - |
|     TypeMapperGetEnumerator |        .NET Core 3.1 | 114.478 ns |  77.04 |      32 B |
|   DirectAccessGetEnumerator |        .NET Core 3.1 | 114.066 ns |  79.36 |      32 B |
|            TypeMapperString | .NET Framework 4.7.2 |  19.764 ns |  13.37 |         - |
|          DirectAccessString | .NET Framework 4.7.2 |   1.448 ns |   1.00 |         - |
|   TypeMapperWrappedInstance | .NET Framework 4.7.2 |  79.746 ns |  54.76 |      32 B |
| DirectAccessWrappedInstance | .NET Framework 4.7.2 |   1.369 ns |   0.93 |         - |
|     TypeMapperGetEnumerator | .NET Framework 4.7.2 | 153.333 ns | 104.35 |      56 B |
|   DirectAccessGetEnumerator | .NET Framework 4.7.2 | 129.055 ns |  89.82 |      56 B |
