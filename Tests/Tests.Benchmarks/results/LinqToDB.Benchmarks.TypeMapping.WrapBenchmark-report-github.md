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
|                      Method |       Runtime |       Mean |      Error |    StdDev |  Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------------------- |-------------- |-----------:|-----------:|----------:|-------:|--------:|-------:|------:|------:|----------:|
|            TypeMapperString |    .NET 4.6.2 |  14.059 ns |  0.3107 ns | 0.0807 ns |  10.48 |    0.20 |      - |     - |     - |         - |
|          DirectAccessString |    .NET 4.6.2 |   1.342 ns |  0.0713 ns | 0.0185 ns |   1.00 |    0.00 |      - |     - |     - |         - |
|   TypeMapperWrappedInstance |    .NET 4.6.2 |  67.142 ns |  2.0024 ns | 0.5200 ns |  50.05 |    0.66 | 0.0095 |     - |     - |      40 B |
| DirectAccessWrappedInstance |    .NET 4.6.2 |   1.371 ns |  0.0886 ns | 0.0137 ns |   1.02 |    0.01 |      - |     - |     - |         - |
|     TypeMapperGetEnumerator |    .NET 4.6.2 | 144.694 ns |  5.7595 ns | 1.4957 ns | 107.88 |    2.42 | 0.0134 |     - |     - |      56 B |
|   DirectAccessGetEnumerator |    .NET 4.6.2 | 129.841 ns |  2.7889 ns | 0.7243 ns |  96.80 |    1.46 | 0.0134 |     - |     - |      56 B |
|            TypeMapperString | .NET Core 2.1 |   7.321 ns |  0.1579 ns | 0.0087 ns |   5.46 |    0.09 |      - |     - |     - |         - |
|          DirectAccessString | .NET Core 2.1 |   2.693 ns |  0.0557 ns | 0.0086 ns |   2.00 |    0.03 |      - |     - |     - |         - |
|   TypeMapperWrappedInstance | .NET Core 2.1 |  54.486 ns | 12.8202 ns | 3.3294 ns |  40.62 |    2.57 | 0.0095 |     - |     - |      40 B |
| DirectAccessWrappedInstance | .NET Core 2.1 |   1.000 ns |  0.2248 ns | 0.0584 ns |   0.75 |    0.04 |      - |     - |     - |         - |
|     TypeMapperGetEnumerator | .NET Core 2.1 | 161.078 ns |  7.6739 ns | 1.9929 ns | 120.08 |    2.35 | 0.0074 |     - |     - |      32 B |
|   DirectAccessGetEnumerator | .NET Core 2.1 | 154.578 ns |  2.6809 ns | 0.6962 ns | 115.23 |    1.32 | 0.0074 |     - |     - |      32 B |
|            TypeMapperString | .NET Core 3.1 |   5.709 ns |  0.1950 ns | 0.0107 ns |   4.26 |    0.07 |      - |     - |     - |         - |
|          DirectAccessString | .NET Core 3.1 |   1.065 ns |  0.1296 ns | 0.0336 ns |   0.79 |    0.03 |      - |     - |     - |         - |
|   TypeMapperWrappedInstance | .NET Core 3.1 |  53.967 ns |  5.1136 ns | 1.3280 ns |  40.23 |    1.18 | 0.0095 |     - |     - |      40 B |
| DirectAccessWrappedInstance | .NET Core 3.1 |   1.124 ns |  0.3180 ns | 0.0826 ns |   0.84 |    0.06 |      - |     - |     - |         - |
|     TypeMapperGetEnumerator | .NET Core 3.1 | 123.733 ns |  8.7168 ns | 2.2637 ns |  92.24 |    2.16 | 0.0076 |     - |     - |      32 B |
|   DirectAccessGetEnumerator | .NET Core 3.1 | 126.545 ns | 23.0683 ns | 5.9908 ns |  94.36 |    5.25 | 0.0076 |     - |     - |      32 B |
