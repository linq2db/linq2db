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
|                    Method |       Runtime |         Mean |       Error |     StdDev |    Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------------- |-------------- |-------------:|------------:|-----------:|---------:|--------:|-------:|------:|------:|----------:|
|           TypeMapperEmpty |    .NET 4.6.2 |    10.009 ns |   0.7806 ns |  0.2027 ns |     7.28 |    0.10 | 0.0153 |     - |     - |      64 B |
|         DirectAccessEmpty |    .NET 4.6.2 |     1.367 ns |   0.0357 ns |  0.0020 ns |     1.00 |    0.00 |      - |     - |     - |         - |
|   TypeMapperAddFireRemove |    .NET 4.6.2 |   129.015 ns |   4.6312 ns |  1.2027 ns |    94.31 |    0.59 | 0.0553 |     - |     - |     233 B |
| DirectAccessAddFireRemove |    .NET 4.6.2 |    74.727 ns |   2.5505 ns |  0.6624 ns |    54.72 |    0.72 | 0.0459 |     - |     - |     193 B |
|      TypeMapperSubscribed |    .NET 4.6.2 |    70.705 ns |   9.8873 ns |  2.5677 ns |    52.43 |    2.21 | 0.0248 |     - |     - |     104 B |
|    DirectAccessSubscribed |    .NET 4.6.2 |     8.899 ns |   0.2933 ns |  0.0762 ns |     6.51 |    0.02 | 0.0153 |     - |     - |      64 B |
|       TypeMapperAddRemove |    .NET 4.6.2 | 3,385.032 ns |  66.7323 ns | 17.3302 ns | 2,476.00 |   21.09 | 0.1335 |     - |     - |     562 B |
|     DirectAccessAddRemove |    .NET 4.6.2 |    69.271 ns |   8.2238 ns |  2.1357 ns |    50.49 |    2.09 | 0.0362 |     - |     - |     152 B |
|           TypeMapperEmpty | .NET Core 2.1 |    11.809 ns |   1.0892 ns |  0.2829 ns |     8.54 |    0.03 | 0.0152 |     - |     - |      64 B |
|         DirectAccessEmpty | .NET Core 2.1 |     1.388 ns |   0.1071 ns |  0.0278 ns |     1.02 |    0.02 |      - |     - |     - |         - |
|   TypeMapperAddFireRemove | .NET Core 2.1 |   122.802 ns |   7.7308 ns |  2.0077 ns |    89.26 |    1.81 | 0.0551 |     - |     - |     232 B |
| DirectAccessAddFireRemove | .NET Core 2.1 |    84.788 ns |  18.2598 ns |  4.7420 ns |    63.45 |    3.90 | 0.0457 |     - |     - |     192 B |
|      TypeMapperSubscribed | .NET Core 2.1 |    64.549 ns |  12.1152 ns |  3.1463 ns |    48.66 |    1.76 | 0.0247 |     - |     - |     104 B |
|    DirectAccessSubscribed | .NET Core 2.1 |    10.023 ns |   0.5287 ns |  0.1373 ns |     7.39 |    0.08 | 0.0152 |     - |     - |      64 B |
|       TypeMapperAddRemove | .NET Core 2.1 | 3,054.955 ns |  66.3455 ns | 17.2297 ns | 2,241.21 |   12.35 | 0.1297 |     - |     - |     552 B |
|     DirectAccessAddRemove | .NET Core 2.1 |    74.038 ns |   8.6262 ns |  2.2402 ns |    53.35 |    1.61 | 0.0362 |     - |     - |     152 B |
|           TypeMapperEmpty | .NET Core 3.1 |    10.723 ns |   1.1352 ns |  0.2948 ns |     7.93 |    0.25 | 0.0153 |     - |     - |      64 B |
|         DirectAccessEmpty | .NET Core 3.1 |     1.367 ns |   0.0400 ns |  0.0022 ns |     1.00 |    0.00 |      - |     - |     - |         - |
|   TypeMapperAddFireRemove | .NET Core 3.1 |   125.176 ns |  24.6815 ns |  6.4097 ns |    89.68 |    5.54 | 0.0553 |     - |     - |     232 B |
| DirectAccessAddFireRemove | .NET Core 3.1 |    74.180 ns |   1.2903 ns |  0.0707 ns |    54.27 |    0.09 | 0.0459 |     - |     - |     192 B |
|      TypeMapperSubscribed | .NET Core 3.1 |    54.993 ns |   3.1252 ns |  0.8116 ns |    40.58 |    0.51 | 0.0249 |     - |     - |     104 B |
|    DirectAccessSubscribed | .NET Core 3.1 |    10.332 ns |   0.6164 ns |  0.1601 ns |     7.53 |    0.15 | 0.0153 |     - |     - |      64 B |
|       TypeMapperAddRemove | .NET Core 3.1 | 2,229.692 ns | 327.7139 ns | 85.1063 ns | 1,595.86 |   48.61 | 0.1297 |     - |     - |     552 B |
|     DirectAccessAddRemove | .NET Core 3.1 |    75.753 ns |  17.0698 ns |  4.4330 ns |    56.49 |    3.63 | 0.0362 |     - |     - |     152 B |
