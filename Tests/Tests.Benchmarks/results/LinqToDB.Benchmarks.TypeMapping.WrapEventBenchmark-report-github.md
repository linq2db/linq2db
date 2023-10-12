```

BenchmarkDotNet v0.13.9+228a464e8be6c580ad9408e98f18813f6407fb5a, Windows 10 (10.0.17763.4644/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK 7.0.401
  [Host]     : .NET 7.0.11 (7.0.1123.42427), X64 RyuJIT AVX2
  Job-DAXXNM : .NET 6.0.22 (6.0.2223.42425), X64 RyuJIT AVX2
  Job-SLTPYD : .NET 7.0.11 (7.0.1123.42427), X64 RyuJIT AVX2
  Job-YOWJJJ : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-OZLLFF : .NET Framework 4.8 (4.8.4645.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method                    | Runtime              | Mean         | Allocated |
|-------------------------- |--------------------- |-------------:|----------:|
| TypeMapperEmpty           | .NET 6.0             |    13.360 ns |      64 B |
| DirectAccessEmpty         | .NET 6.0             |    11.298 ns |      64 B |
| TypeMapperAddFireRemove   | .NET 6.0             |   123.658 ns |     224 B |
| DirectAccessAddFireRemove | .NET 6.0             |    86.907 ns |     192 B |
| TypeMapperSubscribed      | .NET 6.0             |    50.347 ns |      96 B |
| DirectAccessSubscribed    | .NET 6.0             |    13.057 ns |      64 B |
| TypeMapperAddRemove       | .NET 6.0             | 1,142.382 ns |     344 B |
| DirectAccessAddRemove     | .NET 6.0             |    64.286 ns |     152 B |
| TypeMapperEmpty           | .NET 7.0             |    13.858 ns |      64 B |
| DirectAccessEmpty         | .NET 7.0             |     6.317 ns |      64 B |
| TypeMapperAddFireRemove   | .NET 7.0             |    97.825 ns |     224 B |
| DirectAccessAddFireRemove | .NET 7.0             |    90.552 ns |     192 B |
| TypeMapperSubscribed      | .NET 7.0             |    51.242 ns |      96 B |
| DirectAccessSubscribed    | .NET 7.0             |    27.081 ns |      64 B |
| TypeMapperAddRemove       | .NET 7.0             | 1,084.195 ns |     344 B |
| DirectAccessAddRemove     | .NET 7.0             |    53.281 ns |     152 B |
| TypeMapperEmpty           | .NET Core 3.1        |    14.183 ns |      64 B |
| DirectAccessEmpty         | .NET Core 3.1        |    11.016 ns |      64 B |
| TypeMapperAddFireRemove   | .NET Core 3.1        |   126.294 ns |     224 B |
| DirectAccessAddFireRemove | .NET Core 3.1        |    84.082 ns |     192 B |
| TypeMapperSubscribed      | .NET Core 3.1        |    58.647 ns |      96 B |
| DirectAccessSubscribed    | .NET Core 3.1        |    11.332 ns |      64 B |
| TypeMapperAddRemove       | .NET Core 3.1        | 1,152.130 ns |     344 B |
| DirectAccessAddRemove     | .NET Core 3.1        |    73.812 ns |     152 B |
| TypeMapperEmpty           | .NET Framework 4.7.2 |    17.693 ns |      64 B |
| DirectAccessEmpty         | .NET Framework 4.7.2 |    14.346 ns |      64 B |
| TypeMapperAddFireRemove   | .NET Framework 4.7.2 |   123.233 ns |     225 B |
| DirectAccessAddFireRemove | .NET Framework 4.7.2 |    87.466 ns |     193 B |
| TypeMapperSubscribed      | .NET Framework 4.7.2 |    84.339 ns |      96 B |
| DirectAccessSubscribed    | .NET Framework 4.7.2 |    16.373 ns |      64 B |
| TypeMapperAddRemove       | .NET Framework 4.7.2 |   827.546 ns |     345 B |
| DirectAccessAddRemove     | .NET Framework 4.7.2 |    73.362 ns |     152 B |
