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
|                    Method |       Runtime |         Mean |      Error |     StdDev |  Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------------- |-------------- |-------------:|-----------:|-----------:|-------:|--------:|-------:|------:|------:|----------:|
|           TypeMapperEmpty |    .NET 4.6.2 |    10.782 ns |  0.3843 ns |  0.0998 ns |   7.68 |    0.12 | 0.0153 |     - |     - |      64 B |
|         DirectAccessEmpty |    .NET 4.6.2 |     1.404 ns |  0.0570 ns |  0.0148 ns |   1.00 |    0.00 |      - |     - |     - |         - |
|   TypeMapperAddFireRemove |    .NET 4.6.2 |   126.402 ns |  1.8802 ns |  0.4883 ns |  90.06 |    1.11 | 0.0534 |     - |     - |     225 B |
| DirectAccessAddFireRemove |    .NET 4.6.2 |    73.599 ns |  1.5934 ns |  0.4138 ns |  52.44 |    0.79 | 0.0459 |     - |     - |     193 B |
|      TypeMapperSubscribed |    .NET 4.6.2 |    64.084 ns |  0.9037 ns |  0.2347 ns |  45.66 |    0.61 | 0.0229 |     - |     - |      96 B |
|    DirectAccessSubscribed |    .NET 4.6.2 |     9.702 ns |  0.5392 ns |  0.1400 ns |   6.91 |    0.13 | 0.0153 |     - |     - |      64 B |
|       TypeMapperAddRemove |    .NET 4.6.2 | 1,369.864 ns | 98.1915 ns | 25.5000 ns | 976.05 |   25.33 | 0.0820 |     - |     - |     345 B |
|     DirectAccessAddRemove |    .NET 4.6.2 |    68.046 ns |  1.7907 ns |  0.4650 ns |  48.48 |    0.73 | 0.0362 |     - |     - |     152 B |
|           TypeMapperEmpty | .NET Core 2.1 |    10.896 ns |  0.1627 ns |  0.0423 ns |   7.76 |    0.09 | 0.0152 |     - |     - |      64 B |
|         DirectAccessEmpty | .NET Core 2.1 |     1.391 ns |  0.0438 ns |  0.0114 ns |   0.99 |    0.01 |      - |     - |     - |         - |
|   TypeMapperAddFireRemove | .NET Core 2.1 |   115.177 ns |  3.0763 ns |  0.7989 ns |  82.06 |    1.32 | 0.0533 |     - |     - |     224 B |
| DirectAccessAddFireRemove | .NET Core 2.1 |    76.006 ns |  2.1693 ns |  0.5634 ns |  54.15 |    0.79 | 0.0457 |     - |     - |     192 B |
|      TypeMapperSubscribed | .NET Core 2.1 |    50.254 ns |  5.8628 ns |  1.5225 ns |  35.80 |    1.15 | 0.0228 |     - |     - |      96 B |
|    DirectAccessSubscribed | .NET Core 2.1 |     9.200 ns |  0.1667 ns |  0.0433 ns |   6.55 |    0.06 | 0.0152 |     - |     - |      64 B |
|       TypeMapperAddRemove | .NET Core 2.1 | 1,267.785 ns |  9.1030 ns |  1.4087 ns | 903.61 |   10.28 | 0.0801 |     - |     - |     344 B |
|     DirectAccessAddRemove | .NET Core 2.1 |    70.612 ns |  3.8097 ns |  0.9894 ns |  50.31 |    0.73 | 0.0362 |     - |     - |     152 B |
|           TypeMapperEmpty | .NET Core 3.1 |    11.886 ns |  2.7115 ns |  0.7042 ns |   8.47 |    0.51 | 0.0153 |     - |     - |      64 B |
|         DirectAccessEmpty | .NET Core 3.1 |     1.364 ns |  0.0376 ns |  0.0098 ns |   0.97 |    0.01 |      - |     - |     - |         - |
|   TypeMapperAddFireRemove | .NET Core 3.1 |   107.136 ns |  2.4374 ns |  0.6330 ns |  76.33 |    1.14 | 0.0535 |     - |     - |     224 B |
| DirectAccessAddFireRemove | .NET Core 3.1 |    73.640 ns |  1.4130 ns |  0.3670 ns |  52.46 |    0.55 | 0.0459 |     - |     - |     192 B |
|      TypeMapperSubscribed | .NET Core 3.1 |    50.579 ns |  2.3840 ns |  0.6191 ns |  36.04 |    0.77 | 0.0229 |     - |     - |      96 B |
|    DirectAccessSubscribed | .NET Core 3.1 |     9.270 ns |  0.3376 ns |  0.0877 ns |   6.60 |    0.11 | 0.0153 |     - |     - |      64 B |
|       TypeMapperAddRemove | .NET Core 3.1 |   845.258 ns | 21.6256 ns |  5.6161 ns | 602.18 |    4.64 | 0.0820 |     - |     - |     344 B |
|     DirectAccessAddRemove | .NET Core 3.1 |    67.175 ns |  2.2800 ns |  0.5921 ns |  47.86 |    0.60 | 0.0362 |     - |     - |     152 B |
