``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417994 Hz, Resolution=292.5693 ns, Timer=TSC
.NET SDK=5.0.402
  [Host]     : .NET 5.0.11 (5.0.1121.47308), X64 RyuJIT
  Job-ODZCDL : .NET 5.0.11 (5.0.1121.47308), X64 RyuJIT
  Job-PCJJBI : .NET Core 3.1.20 (CoreCLR 4.700.21.47003, CoreFX 4.700.21.47101), X64 RyuJIT
  Job-HHEMGO : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT

Jit=RyuJit  Platform=X64  

```
|                      Method |              Runtime |        Mean |  Ratio | Allocated |
|---------------------------- |--------------------- |------------:|-------:|----------:|
|            TypeMapperString |             .NET 5.0 |   6.3929 ns |   4.61 |         - |
|          DirectAccessString |             .NET 5.0 |   0.9494 ns |   0.68 |         - |
|   TypeMapperWrappedInstance |             .NET 5.0 |  41.5204 ns |  29.91 |      32 B |
| DirectAccessWrappedInstance |             .NET 5.0 |   1.0746 ns |   0.77 |         - |
|     TypeMapperGetEnumerator |             .NET 5.0 |  75.9103 ns |  54.60 |      32 B |
|   DirectAccessGetEnumerator |             .NET 5.0 |  67.8515 ns |  48.89 |      32 B |
|            TypeMapperString |        .NET Core 3.1 |   5.6541 ns |   4.08 |         - |
|          DirectAccessString |        .NET Core 3.1 |   1.3334 ns |   0.96 |         - |
|   TypeMapperWrappedInstance |        .NET Core 3.1 |  43.1764 ns |  31.19 |      32 B |
| DirectAccessWrappedInstance |        .NET Core 3.1 |   1.0373 ns |   0.75 |         - |
|     TypeMapperGetEnumerator |        .NET Core 3.1 | 118.0940 ns |  85.09 |      32 B |
|   DirectAccessGetEnumerator |        .NET Core 3.1 | 111.1462 ns |  80.08 |      32 B |
|            TypeMapperString | .NET Framework 4.7.2 |  19.7592 ns |  14.22 |         - |
|          DirectAccessString | .NET Framework 4.7.2 |   1.3918 ns |   1.00 |         - |
|   TypeMapperWrappedInstance | .NET Framework 4.7.2 |  75.3751 ns |  54.19 |      32 B |
| DirectAccessWrappedInstance | .NET Framework 4.7.2 |   1.0339 ns |   0.74 |         - |
|     TypeMapperGetEnumerator | .NET Framework 4.7.2 | 149.3229 ns | 107.59 |      56 B |
|   DirectAccessGetEnumerator | .NET Framework 4.7.2 | 130.7070 ns |  94.40 |      56 B |
