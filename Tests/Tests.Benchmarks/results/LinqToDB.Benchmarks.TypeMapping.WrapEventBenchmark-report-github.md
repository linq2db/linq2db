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
|                    Method |              Runtime |         Mean |  Ratio | Allocated |
|-------------------------- |--------------------- |-------------:|-------:|----------:|
|           TypeMapperEmpty |             .NET 5.0 |     9.370 ns |   6.90 |      64 B |
|         DirectAccessEmpty |             .NET 5.0 |     1.277 ns |   0.94 |         - |
|   TypeMapperAddFireRemove |             .NET 5.0 |   103.443 ns |  76.08 |     224 B |
| DirectAccessAddFireRemove |             .NET 5.0 |    71.707 ns |  52.75 |     192 B |
|      TypeMapperSubscribed |             .NET 5.0 |    44.588 ns |  32.76 |      96 B |
|    DirectAccessSubscribed |             .NET 5.0 |     8.849 ns |   6.51 |      64 B |
|       TypeMapperAddRemove |             .NET 5.0 |   770.767 ns | 567.26 |     344 B |
|     DirectAccessAddRemove |             .NET 5.0 |    68.088 ns |  50.08 |     152 B |
|           TypeMapperEmpty |        .NET Core 3.1 |     9.628 ns |   7.13 |      64 B |
|         DirectAccessEmpty |        .NET Core 3.1 |     1.399 ns |   1.03 |         - |
|   TypeMapperAddFireRemove |        .NET Core 3.1 |   106.660 ns |  78.68 |     224 B |
| DirectAccessAddFireRemove |        .NET Core 3.1 |    70.246 ns |  51.65 |     192 B |
|      TypeMapperSubscribed |        .NET Core 3.1 |    47.990 ns |  35.39 |      96 B |
|    DirectAccessSubscribed |        .NET Core 3.1 |     9.240 ns |   6.81 |      64 B |
|       TypeMapperAddRemove |        .NET Core 3.1 |   860.981 ns | 633.33 |     344 B |
|     DirectAccessAddRemove |        .NET Core 3.1 |    65.759 ns |  49.16 |     152 B |
|           TypeMapperEmpty | .NET Framework 4.7.2 |    10.615 ns |   7.81 |      64 B |
|         DirectAccessEmpty | .NET Framework 4.7.2 |     1.359 ns |   1.00 |         - |
|   TypeMapperAddFireRemove | .NET Framework 4.7.2 |   128.239 ns |  94.52 |     225 B |
| DirectAccessAddFireRemove | .NET Framework 4.7.2 |    68.063 ns |  50.06 |     193 B |
|      TypeMapperSubscribed | .NET Framework 4.7.2 |    65.070 ns |  47.90 |      96 B |
|    DirectAccessSubscribed | .NET Framework 4.7.2 |     8.969 ns |   6.60 |      64 B |
|       TypeMapperAddRemove | .NET Framework 4.7.2 | 1,294.411 ns | 952.27 |     345 B |
|     DirectAccessAddRemove | .NET Framework 4.7.2 |    63.532 ns |  46.74 |     152 B |
