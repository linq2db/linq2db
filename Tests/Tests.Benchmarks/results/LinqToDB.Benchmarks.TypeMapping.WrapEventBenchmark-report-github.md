``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-XCPGVR : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-RHOQGE : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-WEVYVV : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-ORXRGX : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                    Method |              Runtime |          Mean |        Median |    Ratio |   Gen0 | Allocated | Alloc Ratio |
|-------------------------- |--------------------- |--------------:|--------------:|---------:|-------:|----------:|------------:|
|           TypeMapperEmpty |             .NET 6.0 |    10.2958 ns |    10.3290 ns |    11.58 | 0.0038 |      64 B |          NA |
|         DirectAccessEmpty |             .NET 6.0 |     1.4701 ns |     1.5268 ns |     1.65 |      - |         - |          NA |
|   TypeMapperAddFireRemove |             .NET 6.0 |   104.8235 ns |   104.1934 ns |   117.42 | 0.0134 |     224 B |          NA |
| DirectAccessAddFireRemove |             .NET 6.0 |    79.4153 ns |    79.9212 ns |    89.34 | 0.0114 |     192 B |          NA |
|      TypeMapperSubscribed |             .NET 6.0 |    52.3595 ns |    52.4462 ns |    58.90 | 0.0057 |      96 B |          NA |
|    DirectAccessSubscribed |             .NET 6.0 |     9.0307 ns |     9.2196 ns |     9.89 | 0.0038 |      64 B |          NA |
|       TypeMapperAddRemove |             .NET 6.0 | 1,109.1266 ns | 1,127.0989 ns | 1,244.42 | 0.0191 |     344 B |          NA |
|     DirectAccessAddRemove |             .NET 6.0 |    69.6670 ns |    69.5423 ns |    78.22 | 0.0091 |     152 B |          NA |
|           TypeMapperEmpty |             .NET 7.0 |    10.3620 ns |    10.3715 ns |    11.65 | 0.0038 |      64 B |          NA |
|         DirectAccessEmpty |             .NET 7.0 |     0.5258 ns |     0.5092 ns |     0.59 |      - |         - |          NA |
|   TypeMapperAddFireRemove |             .NET 7.0 |    64.8209 ns |    49.9661 ns |    91.24 | 0.0134 |     224 B |          NA |
| DirectAccessAddFireRemove |             .NET 7.0 |    59.4849 ns |    59.0534 ns |    67.02 | 0.0114 |     192 B |          NA |
|      TypeMapperSubscribed |             .NET 7.0 |    49.0911 ns |    48.9715 ns |    55.23 | 0.0057 |      96 B |          NA |
|    DirectAccessSubscribed |             .NET 7.0 |     8.2064 ns |     7.9710 ns |     9.34 | 0.0038 |      64 B |          NA |
|       TypeMapperAddRemove |             .NET 7.0 | 1,022.2239 ns | 1,013.0487 ns | 1,149.94 | 0.0191 |     344 B |          NA |
|     DirectAccessAddRemove |             .NET 7.0 |    53.0567 ns |    53.2982 ns |    59.68 | 0.0091 |     152 B |          NA |
|           TypeMapperEmpty |        .NET Core 3.1 |    11.1054 ns |    11.1780 ns |    12.42 | 0.0038 |      64 B |          NA |
|         DirectAccessEmpty |        .NET Core 3.1 |     1.5022 ns |     1.5058 ns |     1.69 |      - |         - |          NA |
|   TypeMapperAddFireRemove |        .NET Core 3.1 |   120.5093 ns |   120.9811 ns |   135.14 | 0.0134 |     224 B |          NA |
| DirectAccessAddFireRemove |        .NET Core 3.1 |    95.8949 ns |    96.3069 ns |   107.88 | 0.0114 |     192 B |          NA |
|      TypeMapperSubscribed |        .NET Core 3.1 |    55.1911 ns |    55.2382 ns |    62.03 | 0.0057 |      96 B |          NA |
|    DirectAccessSubscribed |        .NET Core 3.1 |     9.3419 ns |     9.3305 ns |    10.49 | 0.0038 |      64 B |          NA |
|       TypeMapperAddRemove |        .NET Core 3.1 | 1,146.3904 ns | 1,146.8476 ns | 1,285.68 | 0.0191 |     344 B |          NA |
|     DirectAccessAddRemove |        .NET Core 3.1 |    72.4248 ns |    72.4052 ns |    81.32 | 0.0091 |     152 B |          NA |
|           TypeMapperEmpty | .NET Framework 4.7.2 |     5.3538 ns |     5.3565 ns |     6.02 | 0.0102 |      64 B |          NA |
|         DirectAccessEmpty | .NET Framework 4.7.2 |     0.8892 ns |     0.8784 ns |     1.00 |      - |         - |          NA |
|   TypeMapperAddFireRemove | .NET Framework 4.7.2 |   146.5358 ns |   147.8334 ns |   164.86 | 0.0355 |     225 B |          NA |
| DirectAccessAddFireRemove | .NET Framework 4.7.2 |    78.3262 ns |    78.0917 ns |    88.11 | 0.0305 |     193 B |          NA |
|      TypeMapperSubscribed | .NET Framework 4.7.2 |    79.3428 ns |    79.3794 ns |    89.18 | 0.0153 |      96 B |          NA |
|    DirectAccessSubscribed | .NET Framework 4.7.2 |    10.3826 ns |    10.4097 ns |    11.68 | 0.0102 |      64 B |          NA |
|       TypeMapperAddRemove | .NET Framework 4.7.2 | 1,272.6400 ns | 1,264.0469 ns | 1,417.92 | 0.0534 |     345 B |          NA |
|     DirectAccessAddRemove | .NET Framework 4.7.2 |    71.1854 ns |    70.5106 ns |    80.13 | 0.0242 |     152 B |          NA |
