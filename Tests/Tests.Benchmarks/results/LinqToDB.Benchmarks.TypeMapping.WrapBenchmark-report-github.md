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
|                      Method |              Runtime |       Mean |     Median |  Ratio | Allocated |
|---------------------------- |--------------------- |-----------:|-----------:|-------:|----------:|
|            TypeMapperString |             .NET 5.0 |   6.491 ns |   6.366 ns |   5.42 |         - |
|          DirectAccessString |             .NET 5.0 |   1.109 ns |   1.108 ns |   0.97 |         - |
|   TypeMapperWrappedInstance |             .NET 5.0 |  41.586 ns |  41.149 ns |  34.19 |      32 B |
| DirectAccessWrappedInstance |             .NET 5.0 |   1.042 ns |   1.000 ns |   0.87 |         - |
|     TypeMapperGetEnumerator |             .NET 5.0 |  81.138 ns |  80.965 ns |  68.02 |      32 B |
|   DirectAccessGetEnumerator |             .NET 5.0 |  74.160 ns |  73.720 ns |  61.56 |      32 B |
|            TypeMapperString |        .NET Core 3.1 |   5.830 ns |   5.650 ns |   5.07 |         - |
|          DirectAccessString |        .NET Core 3.1 |   1.302 ns |   1.306 ns |   1.11 |         - |
|   TypeMapperWrappedInstance |        .NET Core 3.1 |  47.209 ns |  47.390 ns |  41.97 |      32 B |
| DirectAccessWrappedInstance |        .NET Core 3.1 |   1.327 ns |   1.312 ns |   1.13 |         - |
|     TypeMapperGetEnumerator |        .NET Core 3.1 | 117.116 ns | 115.202 ns | 101.21 |      32 B |
|   DirectAccessGetEnumerator |        .NET Core 3.1 | 112.659 ns | 111.775 ns |  96.62 |      32 B |
|            TypeMapperString | .NET Framework 4.7.2 |  19.772 ns |  19.419 ns |  16.98 |         - |
|          DirectAccessString | .NET Framework 4.7.2 |   1.173 ns |   1.066 ns |   1.00 |         - |
|   TypeMapperWrappedInstance | .NET Framework 4.7.2 |  82.656 ns |  82.417 ns |  70.24 |      32 B |
| DirectAccessWrappedInstance | .NET Framework 4.7.2 |   1.429 ns |   1.428 ns |   1.24 |         - |
|     TypeMapperGetEnumerator | .NET Framework 4.7.2 | 209.091 ns | 209.131 ns | 179.08 |      56 B |
|   DirectAccessGetEnumerator | .NET Framework 4.7.2 | 129.647 ns | 129.324 ns | 111.01 |      56 B |
