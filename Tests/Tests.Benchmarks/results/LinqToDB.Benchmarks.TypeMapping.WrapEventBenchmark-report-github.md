```

BenchmarkDotNet v0.15.2, Windows 10 (10.0.17763.7553/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X 3.39GHz, 2 CPU, 32 logical and 16 physical cores
.NET SDK 9.0.302
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-FTOCRB : .NET 8.0.18 (8.0.1825.31117), X64 RyuJIT AVX2
  Job-DHTNJT : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-QIENBV : .NET Framework 4.8 (4.8.4795.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method                    | Runtime              | Mean         | Allocated |
|-------------------------- |--------------------- |-------------:|----------:|
| TypeMapperEmpty           | .NET 8.0             |    13.517 ns |      64 B |
| DirectAccessEmpty         | .NET 8.0             |     9.773 ns |      64 B |
| TypeMapperAddFireRemove   | .NET 8.0             |    71.060 ns |     224 B |
| DirectAccessAddFireRemove | .NET 8.0             |    36.447 ns |     192 B |
| TypeMapperSubscribed      | .NET 8.0             |    42.898 ns |      96 B |
| DirectAccessSubscribed    | .NET 8.0             |    10.024 ns |      64 B |
| TypeMapperAddRemove       | .NET 8.0             |   855.838 ns |     344 B |
| DirectAccessAddRemove     | .NET 8.0             |    33.575 ns |     152 B |
| TypeMapperEmpty           | .NET 9.0             |    31.452 ns |      64 B |
| DirectAccessEmpty         | .NET 9.0             |     7.971 ns |      64 B |
| TypeMapperAddFireRemove   | .NET 9.0             |    67.790 ns |     224 B |
| DirectAccessAddFireRemove | .NET 9.0             |    49.406 ns |     192 B |
| TypeMapperSubscribed      | .NET 9.0             |    43.408 ns |      96 B |
| DirectAccessSubscribed    | .NET 9.0             |     6.993 ns |      64 B |
| TypeMapperAddRemove       | .NET 9.0             |   970.457 ns |     344 B |
| DirectAccessAddRemove     | .NET 9.0             |    41.282 ns |     152 B |
| TypeMapperEmpty           | .NET Framework 4.6.2 |    13.610 ns |      64 B |
| DirectAccessEmpty         | .NET Framework 4.6.2 |    13.704 ns |      64 B |
| TypeMapperAddFireRemove   | .NET Framework 4.6.2 |   157.461 ns |     225 B |
| DirectAccessAddFireRemove | .NET Framework 4.6.2 |    86.914 ns |     193 B |
| TypeMapperSubscribed      | .NET Framework 4.6.2 |    63.829 ns |      96 B |
| DirectAccessSubscribed    | .NET Framework 4.6.2 |    14.843 ns |      64 B |
| TypeMapperAddRemove       | .NET Framework 4.6.2 | 1,377.337 ns |     345 B |
| DirectAccessAddRemove     | .NET Framework 4.6.2 |    70.806 ns |     152 B |
