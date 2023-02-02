``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-HCNGBR : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-XBFFOD : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-INBZNN : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-THZJXI : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                    Method |              Runtime |          Mean | Allocated |
|-------------------------- |--------------------- |--------------:|----------:|
|           TypeMapperEmpty |             .NET 6.0 |    11.0256 ns |      64 B |
|         DirectAccessEmpty |             .NET 6.0 |     0.9219 ns |         - |
|   TypeMapperAddFireRemove |             .NET 6.0 |   115.8676 ns |     224 B |
| DirectAccessAddFireRemove |             .NET 6.0 |    84.4092 ns |     192 B |
|      TypeMapperSubscribed |             .NET 6.0 |    53.1860 ns |      96 B |
|    DirectAccessSubscribed |             .NET 6.0 |     8.1262 ns |      64 B |
|       TypeMapperAddRemove |             .NET 6.0 | 1,125.7790 ns |     344 B |
|     DirectAccessAddRemove |             .NET 6.0 |    74.0497 ns |     152 B |
|           TypeMapperEmpty |             .NET 7.0 |    20.8678 ns |      64 B |
|         DirectAccessEmpty |             .NET 7.0 |     0.6289 ns |         - |
|   TypeMapperAddFireRemove |             .NET 7.0 |    92.9743 ns |     224 B |
| DirectAccessAddFireRemove |             .NET 7.0 |    61.7045 ns |     192 B |
|      TypeMapperSubscribed |             .NET 7.0 |    55.3557 ns |      96 B |
|    DirectAccessSubscribed |             .NET 7.0 |    12.3039 ns |      64 B |
|       TypeMapperAddRemove |             .NET 7.0 | 1,042.1486 ns |     344 B |
|     DirectAccessAddRemove |             .NET 7.0 |    61.0678 ns |     152 B |
|           TypeMapperEmpty |        .NET Core 3.1 |    13.2283 ns |      64 B |
|         DirectAccessEmpty |        .NET Core 3.1 |     1.1785 ns |         - |
|   TypeMapperAddFireRemove |        .NET Core 3.1 |   131.7174 ns |     224 B |
| DirectAccessAddFireRemove |        .NET Core 3.1 |    98.6437 ns |     192 B |
|      TypeMapperSubscribed |        .NET Core 3.1 |    67.3103 ns |      96 B |
|    DirectAccessSubscribed |        .NET Core 3.1 |    15.2277 ns |      64 B |
|       TypeMapperAddRemove |        .NET Core 3.1 | 1,327.0420 ns |     344 B |
|     DirectAccessAddRemove |        .NET Core 3.1 |    90.2890 ns |     152 B |
|           TypeMapperEmpty | .NET Framework 4.7.2 |    14.5296 ns |      64 B |
|         DirectAccessEmpty | .NET Framework 4.7.2 |     2.0277 ns |         - |
|   TypeMapperAddFireRemove | .NET Framework 4.7.2 |   171.6081 ns |     225 B |
| DirectAccessAddFireRemove | .NET Framework 4.7.2 |    97.7168 ns |     193 B |
|      TypeMapperSubscribed | .NET Framework 4.7.2 |   114.8591 ns |      96 B |
|    DirectAccessSubscribed | .NET Framework 4.7.2 |    12.1653 ns |      64 B |
|       TypeMapperAddRemove | .NET Framework 4.7.2 | 1,576.9454 ns |     345 B |
|     DirectAccessAddRemove | .NET Framework 4.7.2 |   121.5103 ns |     152 B |
