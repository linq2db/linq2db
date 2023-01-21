``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-RNZPMW : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-XCCWXF : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-WSMVMG : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-FMTKFQ : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                    Method |              Runtime |          Mean |        Median |    Ratio |   Gen0 | Allocated | Alloc Ratio |
|-------------------------- |--------------------- |--------------:|--------------:|---------:|-------:|----------:|------------:|
|           TypeMapperEmpty |             .NET 6.0 |    10.5695 ns |    10.5887 ns |    10.95 | 0.0038 |      64 B |          NA |
|         DirectAccessEmpty |             .NET 6.0 |     1.4968 ns |     1.5193 ns |     1.55 |      - |         - |          NA |
|   TypeMapperAddFireRemove |             .NET 6.0 |   118.8779 ns |   118.8079 ns |   123.04 | 0.0134 |     224 B |          NA |
| DirectAccessAddFireRemove |             .NET 6.0 |    82.7007 ns |    82.9334 ns |    84.66 | 0.0114 |     192 B |          NA |
|      TypeMapperSubscribed |             .NET 6.0 |    52.3934 ns |    52.3751 ns |    54.22 | 0.0057 |      96 B |          NA |
|    DirectAccessSubscribed |             .NET 6.0 |     9.8584 ns |     9.8712 ns |    10.20 | 0.0038 |      64 B |          NA |
|       TypeMapperAddRemove |             .NET 6.0 | 1,147.4242 ns | 1,149.2764 ns | 1,188.91 | 0.0191 |     344 B |          NA |
|     DirectAccessAddRemove |             .NET 6.0 |    68.1115 ns |    68.1225 ns |    65.83 | 0.0091 |     152 B |          NA |
|           TypeMapperEmpty |             .NET 7.0 |    19.8304 ns |    20.5152 ns |    19.91 | 0.0038 |      64 B |          NA |
|         DirectAccessEmpty |             .NET 7.0 |     0.5992 ns |     0.5406 ns |     0.65 |      - |         - |          NA |
|   TypeMapperAddFireRemove |             .NET 7.0 |    94.2617 ns |    98.7605 ns |    82.50 | 0.0134 |     224 B |          NA |
| DirectAccessAddFireRemove |             .NET 7.0 |    60.3589 ns |    59.7348 ns |    62.59 | 0.0114 |     192 B |          NA |
|      TypeMapperSubscribed |             .NET 7.0 |    47.9089 ns |    48.0631 ns |    49.47 | 0.0057 |      96 B |          NA |
|    DirectAccessSubscribed |             .NET 7.0 |     9.8940 ns |     9.8692 ns |    10.18 | 0.0038 |      64 B |          NA |
|       TypeMapperAddRemove |             .NET 7.0 | 1,064.4309 ns | 1,071.7346 ns | 1,101.32 | 0.0191 |     344 B |          NA |
|     DirectAccessAddRemove |             .NET 7.0 |    55.6650 ns |    55.4773 ns |    57.62 | 0.0091 |     152 B |          NA |
|           TypeMapperEmpty |        .NET Core 3.1 |    11.1500 ns |    11.0965 ns |    11.54 | 0.0038 |      64 B |          NA |
|         DirectAccessEmpty |        .NET Core 3.1 |     2.2120 ns |     2.7014 ns |     1.26 |      - |         - |          NA |
|   TypeMapperAddFireRemove |        .NET Core 3.1 |   130.8443 ns |   130.7142 ns |   135.67 | 0.0134 |     224 B |          NA |
| DirectAccessAddFireRemove |        .NET Core 3.1 |    74.4406 ns |    85.9095 ns |    92.39 | 0.0114 |     192 B |          NA |
|      TypeMapperSubscribed |        .NET Core 3.1 |    57.5854 ns |    57.5722 ns |    59.61 | 0.0057 |      96 B |          NA |
|    DirectAccessSubscribed |        .NET Core 3.1 |    10.8713 ns |    10.9616 ns |    10.81 | 0.0038 |      64 B |          NA |
|       TypeMapperAddRemove |        .NET Core 3.1 | 1,169.0442 ns | 1,163.3046 ns | 1,211.04 | 0.0191 |     344 B |          NA |
|     DirectAccessAddRemove |        .NET Core 3.1 |    75.7660 ns |    76.0374 ns |    78.50 | 0.0091 |     152 B |          NA |
|           TypeMapperEmpty | .NET Framework 4.7.2 |     9.9941 ns |    11.7008 ns |    11.58 | 0.0102 |      64 B |          NA |
|         DirectAccessEmpty | .NET Framework 4.7.2 |     0.9678 ns |     0.9815 ns |     1.00 |      - |         - |          NA |
|   TypeMapperAddFireRemove | .NET Framework 4.7.2 |   146.2950 ns |   145.0649 ns |   151.41 | 0.0355 |     225 B |          NA |
| DirectAccessAddFireRemove | .NET Framework 4.7.2 |    80.4172 ns |    79.7961 ns |    83.23 | 0.0305 |     193 B |          NA |
|      TypeMapperSubscribed | .NET Framework 4.7.2 |    81.2504 ns |    80.5994 ns |    84.11 | 0.0153 |      96 B |          NA |
|    DirectAccessSubscribed | .NET Framework 4.7.2 |    11.2420 ns |    11.7677 ns |     8.76 | 0.0102 |      64 B |          NA |
|       TypeMapperAddRemove | .NET Framework 4.7.2 | 1,409.9181 ns | 1,417.8797 ns | 1,459.28 | 0.0534 |     345 B |          NA |
|     DirectAccessAddRemove | .NET Framework 4.7.2 |    32.8215 ns |    32.7992 ns |    34.09 | 0.0242 |     152 B |          NA |
