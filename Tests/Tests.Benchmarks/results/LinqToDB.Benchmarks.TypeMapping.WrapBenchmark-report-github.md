``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417995 Hz, Resolution=292.5692 ns, Timer=TSC
  [Host]     : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-FSMYUH : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-TSQXSD : .NET Core 2.1.15 (CoreCLR 4.6.28325.01, CoreFX 4.6.28327.02), X64 RyuJIT
  Job-OUTKHJ : .NET Core 3.1.1 (CoreCLR 4.700.19.60701, CoreFX 4.700.19.60801), X64 RyuJIT

Jit=RyuJit  Platform=X64  MaxIterationCount=5  
MinIterationCount=3  WarmupCount=2  

```
|                      Method |       Runtime |       Mean |     Error |    StdDev |  Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------------------- |-------------- |-----------:|----------:|----------:|-------:|--------:|-------:|------:|------:|----------:|
|            TypeMapperString |    .NET 4.6.2 |  13.898 ns | 0.1083 ns | 0.0059 ns |   9.98 |    0.05 |      - |     - |     - |         - |
|          DirectAccessString |    .NET 4.6.2 |   1.392 ns | 0.0379 ns | 0.0059 ns |   1.00 |    0.00 |      - |     - |     - |         - |
|   TypeMapperWrappedInstance |    .NET 4.6.2 |  61.292 ns | 1.1154 ns | 0.2897 ns |  43.96 |    0.28 | 0.0076 |     - |     - |      32 B |
| DirectAccessWrappedInstance |    .NET 4.6.2 |   1.232 ns | 0.1480 ns | 0.0384 ns |   0.89 |    0.03 |      - |     - |     - |         - |
|     TypeMapperGetEnumerator |    .NET 4.6.2 | 147.154 ns | 5.3797 ns | 1.3971 ns | 105.70 |    1.37 | 0.0134 |     - |     - |      56 B |
|   DirectAccessGetEnumerator |    .NET 4.6.2 | 135.024 ns | 6.5384 ns | 1.6980 ns |  97.08 |    1.61 | 0.0134 |     - |     - |      56 B |
|            TypeMapperString | .NET Core 2.1 |   7.285 ns | 0.2107 ns | 0.0115 ns |   5.23 |    0.02 |      - |     - |     - |         - |
|          DirectAccessString | .NET Core 2.1 |   2.715 ns | 0.1199 ns | 0.0186 ns |   1.95 |    0.02 |      - |     - |     - |         - |
|   TypeMapperWrappedInstance | .NET Core 2.1 |  44.557 ns | 0.6675 ns | 0.1033 ns |  32.01 |    0.15 | 0.0076 |     - |     - |      32 B |
| DirectAccessWrappedInstance | .NET Core 2.1 |   1.152 ns | 0.2726 ns | 0.0708 ns |   0.82 |    0.06 |      - |     - |     - |         - |
|     TypeMapperGetEnumerator | .NET Core 2.1 | 161.866 ns | 4.9457 ns | 1.2844 ns | 116.40 |    1.10 | 0.0074 |     - |     - |      32 B |
|   DirectAccessGetEnumerator | .NET Core 2.1 | 156.028 ns | 2.8689 ns | 0.1573 ns | 112.03 |    0.58 | 0.0074 |     - |     - |      32 B |
|            TypeMapperString | .NET Core 3.1 |   5.697 ns | 0.2091 ns | 0.0543 ns |   4.10 |    0.05 |      - |     - |     - |         - |
|          DirectAccessString | .NET Core 3.1 |   1.078 ns | 0.0829 ns | 0.0215 ns |   0.78 |    0.02 |      - |     - |     - |         - |
|   TypeMapperWrappedInstance | .NET Core 3.1 |  46.275 ns | 1.2819 ns | 0.3329 ns |  33.29 |    0.19 | 0.0076 |     - |     - |      32 B |
| DirectAccessWrappedInstance | .NET Core 3.1 |   1.061 ns | 0.0543 ns | 0.0084 ns |   0.76 |    0.01 |      - |     - |     - |         - |
|     TypeMapperGetEnumerator | .NET Core 3.1 | 126.430 ns | 9.3163 ns | 2.4194 ns |  90.90 |    2.34 | 0.0076 |     - |     - |      32 B |
|   DirectAccessGetEnumerator | .NET Core 3.1 | 116.166 ns | 4.3071 ns | 1.1186 ns |  83.45 |    1.16 | 0.0076 |     - |     - |      32 B |
