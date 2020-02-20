``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417995 Hz, Resolution=292.5692 ns, Timer=TSC
  [Host]     : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-ZXOHUL : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-TAKBNN : .NET Core 2.1.15 (CoreCLR 4.6.28325.01, CoreFX 4.6.28327.02), X64 RyuJIT
  Job-WOIQBX : .NET Core 3.1.1 (CoreCLR 4.700.19.60701, CoreFX 4.700.19.60801), X64 RyuJIT

Jit=RyuJit  Platform=X64  MaxIterationCount=10  
MinIterationCount=5  WarmupCount=2  

```
|                      Method |       Runtime |        Mean |      Error |    StdDev |  Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------------------- |-------------- |------------:|-----------:|----------:|-------:|--------:|-------:|------:|------:|----------:|
|            TypeMapperString |    .NET 4.6.2 |  14.7581 ns |  0.3621 ns | 0.1608 ns |  10.29 |    0.30 |      - |     - |     - |         - |
|          DirectAccessString |    .NET 4.6.2 |   1.4363 ns |  0.0951 ns | 0.0498 ns |   1.00 |    0.00 |      - |     - |     - |         - |
|   TypeMapperWrappedInstance |    .NET 4.6.2 |  68.9683 ns |  1.5009 ns | 0.9928 ns |  48.24 |    1.99 | 0.0114 |     - |     - |      48 B |
| DirectAccessWrappedInstance |    .NET 4.6.2 |   0.9779 ns |  0.0982 ns | 0.0584 ns |   0.68 |    0.05 |      - |     - |     - |         - |
|     TypeMapperGetEnumerator |    .NET 4.6.2 | 149.2968 ns |  8.3945 ns | 4.9954 ns | 103.39 |    2.62 | 0.0134 |     - |     - |      56 B |
|   DirectAccessGetEnumerator |    .NET 4.6.2 | 134.2418 ns |  3.0130 ns | 1.9929 ns |  93.68 |    4.24 | 0.0134 |     - |     - |      56 B |
|            TypeMapperString | .NET Core 2.1 |   7.6207 ns |  0.2098 ns | 0.0748 ns |   5.32 |    0.19 |      - |     - |     - |         - |
|          DirectAccessString | .NET Core 2.1 |   2.6843 ns |  0.1967 ns | 0.1171 ns |   1.86 |    0.10 |      - |     - |     - |         - |
|   TypeMapperWrappedInstance | .NET Core 2.1 |  58.6504 ns |  6.9434 ns | 4.5927 ns |  42.01 |    3.58 | 0.0114 |     - |     - |      48 B |
| DirectAccessWrappedInstance | .NET Core 2.1 |   1.1359 ns |  0.1441 ns | 0.0953 ns |   0.81 |    0.05 |      - |     - |     - |         - |
|     TypeMapperGetEnumerator | .NET Core 2.1 | 167.4304 ns |  4.2024 ns | 2.7796 ns | 116.85 |    3.80 | 0.0074 |     - |     - |      32 B |
|   DirectAccessGetEnumerator | .NET Core 2.1 | 173.0836 ns |  9.2947 ns | 6.1478 ns | 121.57 |    3.92 | 0.0074 |     - |     - |      32 B |
|            TypeMapperString | .NET Core 3.1 |   6.0086 ns |  0.2154 ns | 0.1282 ns |   4.19 |    0.19 |      - |     - |     - |         - |
|          DirectAccessString | .NET Core 3.1 |   1.1539 ns |  0.1366 ns | 0.0903 ns |   0.81 |    0.07 |      - |     - |     - |         - |
|   TypeMapperWrappedInstance | .NET Core 3.1 |  55.3545 ns |  2.0552 ns | 1.3594 ns |  38.63 |    1.24 | 0.0114 |     - |     - |      48 B |
| DirectAccessWrappedInstance | .NET Core 3.1 |   1.4463 ns |  0.1081 ns | 0.0480 ns |   1.01 |    0.03 |      - |     - |     - |         - |
|     TypeMapperGetEnumerator | .NET Core 3.1 | 131.5543 ns |  8.5124 ns | 5.6304 ns |  92.49 |    4.02 | 0.0076 |     - |     - |      32 B |
|   DirectAccessGetEnumerator | .NET Core 3.1 | 129.6684 ns | 14.4799 ns | 9.5775 ns |  92.13 |    7.11 | 0.0076 |     - |     - |      32 B |
