``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417995 Hz, Resolution=292.5692 ns, Timer=TSC
  [Host]     : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-FSMYUH : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-TSQXSD : .NET Core 2.1.17 (CoreCLR 4.6.28619.01, CoreFX 4.6.28619.01), X64 RyuJIT
  Job-OUTKHJ : .NET Core 3.1.3 (CoreCLR 4.700.20.11803, CoreFX 4.700.20.12001), X64 RyuJIT

Jit=RyuJit  Platform=X64  MaxIterationCount=5  
MinIterationCount=3  WarmupCount=2  

```
|                      Method |       Runtime |       Mean |  Ratio |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------------------- |-------------- |-----------:|-------:|-------:|------:|------:|----------:|
|            TypeMapperString |    .NET 4.6.2 |  13.386 ns |  12.49 |      - |     - |     - |         - |
|          DirectAccessString |    .NET 4.6.2 |   1.072 ns |   1.00 |      - |     - |     - |         - |
|   TypeMapperWrappedInstance |    .NET 4.6.2 |  59.660 ns |  55.72 | 0.0076 |     - |     - |      32 B |
| DirectAccessWrappedInstance |    .NET 4.6.2 |   1.047 ns |   0.98 |      - |     - |     - |         - |
|     TypeMapperGetEnumerator |    .NET 4.6.2 | 145.496 ns | 136.06 | 0.0134 |     - |     - |      56 B |
|   DirectAccessGetEnumerator |    .NET 4.6.2 | 129.819 ns | 121.19 | 0.0134 |     - |     - |      56 B |
|            TypeMapperString | .NET Core 2.1 |   7.834 ns |   7.28 |      - |     - |     - |         - |
|          DirectAccessString | .NET Core 2.1 |   2.680 ns |   2.49 |      - |     - |     - |         - |
|   TypeMapperWrappedInstance | .NET Core 2.1 |  45.563 ns |  42.39 | 0.0076 |     - |     - |      32 B |
| DirectAccessWrappedInstance | .NET Core 2.1 |   1.078 ns |   1.00 |      - |     - |     - |         - |
|     TypeMapperGetEnumerator | .NET Core 2.1 | 167.759 ns | 157.26 | 0.0074 |     - |     - |      32 B |
|   DirectAccessGetEnumerator | .NET Core 2.1 | 160.129 ns | 150.60 | 0.0074 |     - |     - |      32 B |
|            TypeMapperString | .NET Core 3.1 |   5.679 ns |   5.30 |      - |     - |     - |         - |
|          DirectAccessString | .NET Core 3.1 |   1.339 ns |   1.25 |      - |     - |     - |         - |
|   TypeMapperWrappedInstance | .NET Core 3.1 |  45.044 ns |  42.11 | 0.0076 |     - |     - |      32 B |
| DirectAccessWrappedInstance | .NET Core 3.1 |   1.102 ns |   1.03 |      - |     - |     - |         - |
|     TypeMapperGetEnumerator | .NET Core 3.1 | 121.994 ns | 113.85 | 0.0076 |     - |     - |      32 B |
|   DirectAccessGetEnumerator | .NET Core 3.1 | 121.486 ns | 114.36 | 0.0076 |     - |     - |      32 B |
