``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-UZBSVL : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-AYZXIO : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-NXXYQT : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-HMCTKM : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                    Method |              Runtime |          Mean |        Median |    Ratio |   Gen0 | Allocated | Alloc Ratio |
|-------------------------- |--------------------- |--------------:|--------------:|---------:|-------:|----------:|------------:|
|           TypeMapperEmpty |             .NET 6.0 |    10.2389 ns |    10.2217 ns |    10.87 | 0.0038 |      64 B |          NA |
|         DirectAccessEmpty |             .NET 6.0 |     1.4282 ns |     1.4358 ns |     1.47 |      - |         - |          NA |
|   TypeMapperAddFireRemove |             .NET 6.0 |    51.0010 ns |    50.9549 ns |    54.15 | 0.0134 |     224 B |          NA |
| DirectAccessAddFireRemove |             .NET 6.0 |    79.7855 ns |    80.1596 ns |    84.71 | 0.0114 |     192 B |          NA |
|      TypeMapperSubscribed |             .NET 6.0 |    50.9327 ns |    50.5237 ns |    54.08 | 0.0057 |      96 B |          NA |
|    DirectAccessSubscribed |             .NET 6.0 |     8.3872 ns |     8.1333 ns |     8.88 | 0.0038 |      64 B |          NA |
|       TypeMapperAddRemove |             .NET 6.0 | 1,122.3437 ns | 1,133.1787 ns | 1,191.61 | 0.0191 |     344 B |          NA |
|     DirectAccessAddRemove |             .NET 6.0 |    71.5669 ns |    71.4067 ns |    75.98 | 0.0091 |     152 B |          NA |
|           TypeMapperEmpty |             .NET 7.0 |    10.6073 ns |    10.7004 ns |    11.23 | 0.0038 |      64 B |          NA |
|         DirectAccessEmpty |             .NET 7.0 |     0.3198 ns |     0.4703 ns |     0.27 |      - |         - |          NA |
|   TypeMapperAddFireRemove |             .NET 7.0 |    93.8651 ns |    93.2296 ns |    99.66 | 0.0134 |     224 B |          NA |
| DirectAccessAddFireRemove |             .NET 7.0 |    58.6600 ns |    58.8920 ns |    62.28 | 0.0114 |     192 B |          NA |
|      TypeMapperSubscribed |             .NET 7.0 |    49.0210 ns |    49.1281 ns |    51.86 | 0.0057 |      96 B |          NA |
|    DirectAccessSubscribed |             .NET 7.0 |     8.8409 ns |     8.8894 ns |     9.39 | 0.0038 |      64 B |          NA |
|       TypeMapperAddRemove |             .NET 7.0 | 1,091.3789 ns | 1,100.8926 ns | 1,158.73 | 0.0191 |     344 B |          NA |
|     DirectAccessAddRemove |             .NET 7.0 |    49.0240 ns |    48.8569 ns |    52.05 | 0.0091 |     152 B |          NA |
|           TypeMapperEmpty |        .NET Core 3.1 |    10.4018 ns |    10.4504 ns |    11.04 | 0.0038 |      64 B |          NA |
|         DirectAccessEmpty |        .NET Core 3.1 |     0.8755 ns |     0.8461 ns |     0.93 |      - |         - |          NA |
|   TypeMapperAddFireRemove |        .NET Core 3.1 |   121.3975 ns |   121.1093 ns |   128.89 | 0.0134 |     224 B |          NA |
| DirectAccessAddFireRemove |        .NET Core 3.1 |    88.1339 ns |    88.9285 ns |    93.57 | 0.0114 |     192 B |          NA |
|      TypeMapperSubscribed |        .NET Core 3.1 |    48.6159 ns |    50.8983 ns |    37.16 | 0.0057 |      96 B |          NA |
|    DirectAccessSubscribed |        .NET Core 3.1 |     9.6149 ns |     9.6006 ns |    10.21 | 0.0038 |      64 B |          NA |
|       TypeMapperAddRemove |        .NET Core 3.1 | 1,167.0572 ns | 1,164.7478 ns | 1,239.07 | 0.0191 |     344 B |          NA |
|     DirectAccessAddRemove |        .NET Core 3.1 |    72.1429 ns |    72.1286 ns |    76.60 | 0.0091 |     152 B |          NA |
|           TypeMapperEmpty | .NET Framework 4.7.2 |    11.1071 ns |    11.1001 ns |    11.79 | 0.0102 |      64 B |          NA |
|         DirectAccessEmpty | .NET Framework 4.7.2 |     0.9419 ns |     0.9418 ns |     1.00 |      - |         - |          NA |
|   TypeMapperAddFireRemove | .NET Framework 4.7.2 |   142.7498 ns |   142.7879 ns |   151.56 | 0.0355 |     225 B |          NA |
| DirectAccessAddFireRemove | .NET Framework 4.7.2 |    78.4409 ns |    77.9958 ns |    83.28 | 0.0305 |     193 B |          NA |
|      TypeMapperSubscribed | .NET Framework 4.7.2 |    79.1549 ns |    79.1440 ns |    84.04 | 0.0153 |      96 B |          NA |
|    DirectAccessSubscribed | .NET Framework 4.7.2 |     9.9744 ns |    10.0257 ns |    10.59 | 0.0102 |      64 B |          NA |
|       TypeMapperAddRemove | .NET Framework 4.7.2 | 1,391.1895 ns | 1,390.9831 ns | 1,477.01 | 0.0534 |     345 B |          NA |
|     DirectAccessAddRemove | .NET Framework 4.7.2 |    70.3788 ns |    70.3968 ns |    74.72 | 0.0242 |     152 B |          NA |
