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
|           TypeMapperEmpty |             .NET 5.0 |     9.965 ns |     9.926 ns |   7.31 |      64 B |
|         DirectAccessEmpty |             .NET 5.0 |     1.355 ns |     1.355 ns |   0.99 |         - |
|   TypeMapperAddFireRemove |             .NET 5.0 |   109.431 ns |   109.578 ns |  79.84 |     224 B |
| DirectAccessAddFireRemove |             .NET 5.0 |    78.556 ns |    78.162 ns |  57.11 |     192 B |
|      TypeMapperSubscribed |             .NET 5.0 |    46.500 ns |    45.968 ns |  33.83 |      96 B |
|    DirectAccessSubscribed |             .NET 5.0 |     9.049 ns |     9.035 ns |   6.62 |      64 B |
|       TypeMapperAddRemove |             .NET 5.0 |   910.328 ns |   910.337 ns | 665.78 |     344 B |
|     DirectAccessAddRemove |             .NET 5.0 |    71.815 ns |    71.042 ns |  53.50 |     152 B |
|           TypeMapperEmpty |        .NET Core 3.1 |     9.734 ns |     9.743 ns |   7.14 |      64 B |
|         DirectAccessEmpty |        .NET Core 3.1 |     1.388 ns |     1.378 ns |   1.01 |         - |
|   TypeMapperAddFireRemove |        .NET Core 3.1 |   110.475 ns |   109.906 ns |  80.66 |     224 B |
| DirectAccessAddFireRemove |        .NET Core 3.1 |    73.156 ns |    72.508 ns |  53.24 |     192 B |
|      TypeMapperSubscribed |        .NET Core 3.1 |    51.209 ns |    50.601 ns |  37.24 |      96 B |
|    DirectAccessSubscribed |        .NET Core 3.1 |     9.600 ns |     9.429 ns |   6.98 |      64 B |
|       TypeMapperAddRemove |        .NET Core 3.1 |   885.253 ns |   872.269 ns | 640.80 |     344 B |
|     DirectAccessAddRemove |        .NET Core 3.1 |    68.764 ns |    68.216 ns |  50.37 |     152 B |
|           TypeMapperEmpty | .NET Framework 4.7.2 |     9.605 ns |     9.545 ns |   7.16 |      64 B |
|         DirectAccessEmpty | .NET Framework 4.7.2 |     1.379 ns |     1.347 ns |   1.00 |         - |
|   TypeMapperAddFireRemove | .NET Framework 4.7.2 |   128.176 ns |   126.801 ns |  93.47 |     225 B |
| DirectAccessAddFireRemove | .NET Framework 4.7.2 |    72.106 ns |    72.201 ns |  52.85 |     193 B |
|      TypeMapperSubscribed | .NET Framework 4.7.2 |    70.226 ns |    69.617 ns |  51.16 |      96 B |
|    DirectAccessSubscribed | .NET Framework 4.7.2 |     8.336 ns |     8.299 ns |   6.12 |      64 B |
|       TypeMapperAddRemove | .NET Framework 4.7.2 | 1,342.512 ns | 1,317.337 ns | 982.27 |     345 B |
|     DirectAccessAddRemove | .NET Framework 4.7.2 |    65.046 ns |    64.711 ns |  47.44 |     152 B |
