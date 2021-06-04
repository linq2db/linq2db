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
|                    Method |              Runtime |         Mean |       Median |  Ratio | Allocated |
|-------------------------- |--------------------- |-------------:|-------------:|-------:|----------:|
|           TypeMapperEmpty |             .NET 5.0 |     9.495 ns |     9.501 ns |   6.85 |      64 B |
|         DirectAccessEmpty |             .NET 5.0 |     1.347 ns |     1.341 ns |   0.96 |         - |
|   TypeMapperAddFireRemove |             .NET 5.0 |   104.227 ns |   104.020 ns |  73.92 |     224 B |
| DirectAccessAddFireRemove |             .NET 5.0 |    73.263 ns |    72.635 ns |  51.94 |     192 B |
|      TypeMapperSubscribed |             .NET 5.0 |    45.036 ns |    45.049 ns |  31.94 |      96 B |
|    DirectAccessSubscribed |             .NET 5.0 |     8.890 ns |     8.901 ns |   6.30 |      64 B |
|       TypeMapperAddRemove |             .NET 5.0 |   871.340 ns |   871.857 ns | 617.85 |     344 B |
|     DirectAccessAddRemove |             .NET 5.0 |    64.492 ns |    64.493 ns |  46.56 |     152 B |
|           TypeMapperEmpty |        .NET Core 3.1 |     9.318 ns |     9.331 ns |   6.66 |      64 B |
|         DirectAccessEmpty |        .NET Core 3.1 |     1.394 ns |     1.348 ns |   1.01 |         - |
|   TypeMapperAddFireRemove |        .NET Core 3.1 |   107.869 ns |   106.690 ns |  76.65 |     224 B |
| DirectAccessAddFireRemove |        .NET Core 3.1 |    74.334 ns |    74.274 ns |  52.71 |     192 B |
|      TypeMapperSubscribed |        .NET Core 3.1 |    46.295 ns |    46.258 ns |  32.81 |      96 B |
|    DirectAccessSubscribed |        .NET Core 3.1 |     9.215 ns |     9.136 ns |   6.53 |      64 B |
|       TypeMapperAddRemove |        .NET Core 3.1 |   816.768 ns |   814.297 ns | 584.23 |     344 B |
|     DirectAccessAddRemove |        .NET Core 3.1 |    63.757 ns |    63.481 ns |  45.24 |     152 B |
|           TypeMapperEmpty | .NET Framework 4.7.2 |     9.935 ns |     9.837 ns |   7.04 |      64 B |
|         DirectAccessEmpty | .NET Framework 4.7.2 |     1.400 ns |     1.372 ns |   1.00 |         - |
|   TypeMapperAddFireRemove | .NET Framework 4.7.2 |   126.875 ns |   126.951 ns |  89.97 |     225 B |
| DirectAccessAddFireRemove | .NET Framework 4.7.2 |    69.299 ns |    68.823 ns |  49.11 |     193 B |
|      TypeMapperSubscribed | .NET Framework 4.7.2 |    68.892 ns |    68.741 ns |  48.89 |      96 B |
|    DirectAccessSubscribed | .NET Framework 4.7.2 |     8.895 ns |     8.895 ns |   6.36 |      64 B |
|       TypeMapperAddRemove | .NET Framework 4.7.2 | 1,371.111 ns | 1,369.169 ns | 989.79 |     345 B |
|     DirectAccessAddRemove | .NET Framework 4.7.2 |    65.507 ns |    65.501 ns |  46.47 |     152 B |
