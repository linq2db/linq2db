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
|                    Method |       Runtime |         Mean |  Ratio |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------------- |-------------- |-------------:|-------:|-------:|------:|------:|----------:|
|           TypeMapperEmpty |    .NET 4.6.2 |    10.776 ns |   7.55 | 0.0153 |     - |     - |      64 B |
|         DirectAccessEmpty |    .NET 4.6.2 |     1.429 ns |   1.00 |      - |     - |     - |         - |
|   TypeMapperAddFireRemove |    .NET 4.6.2 |   122.566 ns |  85.94 | 0.0534 |     - |     - |     225 B |
| DirectAccessAddFireRemove |    .NET 4.6.2 |    73.229 ns |  50.76 | 0.0459 |     - |     - |     193 B |
|      TypeMapperSubscribed |    .NET 4.6.2 |    63.348 ns |  44.41 | 0.0229 |     - |     - |      96 B |
|    DirectAccessSubscribed |    .NET 4.6.2 |     9.648 ns |   6.55 | 0.0153 |     - |     - |      64 B |
|       TypeMapperAddRemove |    .NET 4.6.2 | 1,294.025 ns | 907.15 | 0.0820 |     - |     - |     345 B |
|     DirectAccessAddRemove |    .NET 4.6.2 |    68.602 ns |  48.09 | 0.0362 |     - |     - |     152 B |
|           TypeMapperEmpty | .NET Core 2.1 |    10.850 ns |   7.52 | 0.0152 |     - |     - |      64 B |
|         DirectAccessEmpty | .NET Core 2.1 |     1.347 ns |   0.93 |      - |     - |     - |         - |
|   TypeMapperAddFireRemove | .NET Core 2.1 |   115.079 ns |  80.68 | 0.0533 |     - |     - |     224 B |
| DirectAccessAddFireRemove | .NET Core 2.1 |    75.362 ns |  52.23 | 0.0457 |     - |     - |     192 B |
|      TypeMapperSubscribed | .NET Core 2.1 |    49.245 ns |  34.14 | 0.0228 |     - |     - |      96 B |
|    DirectAccessSubscribed | .NET Core 2.1 |     9.252 ns |   6.49 | 0.0152 |     - |     - |      64 B |
|       TypeMapperAddRemove | .NET Core 2.1 | 1,358.684 ns | 953.09 | 0.0801 |     - |     - |     344 B |
|     DirectAccessAddRemove | .NET Core 2.1 |    69.022 ns |  47.84 | 0.0362 |     - |     - |     152 B |
|           TypeMapperEmpty | .NET Core 3.1 |    11.471 ns |   8.04 | 0.0153 |     - |     - |      64 B |
|         DirectAccessEmpty | .NET Core 3.1 |     1.355 ns |   0.94 |      - |     - |     - |         - |
|   TypeMapperAddFireRemove | .NET Core 3.1 |   110.407 ns |  77.42 | 0.0535 |     - |     - |     224 B |
| DirectAccessAddFireRemove | .NET Core 3.1 |    79.216 ns |  54.90 | 0.0459 |     - |     - |     192 B |
|      TypeMapperSubscribed | .NET Core 3.1 |    48.649 ns |  34.10 | 0.0229 |     - |     - |      96 B |
|    DirectAccessSubscribed | .NET Core 3.1 |    10.475 ns |   7.34 | 0.0153 |     - |     - |      64 B |
|       TypeMapperAddRemove | .NET Core 3.1 |   863.926 ns | 598.75 | 0.0820 |     - |     - |     344 B |
|     DirectAccessAddRemove | .NET Core 3.1 |    76.500 ns |  53.65 | 0.0362 |     - |     - |     152 B |
