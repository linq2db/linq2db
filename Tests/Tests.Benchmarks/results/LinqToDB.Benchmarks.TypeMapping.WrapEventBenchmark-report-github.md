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
| TypeMapperEmpty           | .NET 8.0             |    13.655 ns |      64 B |
| DirectAccessEmpty         | .NET 8.0             |    10.132 ns |      64 B |
| TypeMapperAddFireRemove   | .NET 8.0             |    60.569 ns |     224 B |
| DirectAccessAddFireRemove | .NET 8.0             |    49.834 ns |     192 B |
| TypeMapperSubscribed      | .NET 8.0             |    44.054 ns |      96 B |
| DirectAccessSubscribed    | .NET 8.0             |    10.391 ns |      64 B |
| TypeMapperAddRemove       | .NET 8.0             | 1,037.691 ns |     344 B |
| DirectAccessAddRemove     | .NET 8.0             |    32.688 ns |     152 B |
| TypeMapperEmpty           | .NET 9.0             |     4.631 ns |      64 B |
| DirectAccessEmpty         | .NET 9.0             |     8.544 ns |      64 B |
| TypeMapperAddFireRemove   | .NET 9.0             |    78.773 ns |     224 B |
| DirectAccessAddFireRemove | .NET 9.0             |    48.413 ns |     192 B |
| TypeMapperSubscribed      | .NET 9.0             |    43.328 ns |      96 B |
| DirectAccessSubscribed    | .NET 9.0             |     8.610 ns |      64 B |
| TypeMapperAddRemove       | .NET 9.0             | 1,092.629 ns |     344 B |
| DirectAccessAddRemove     | .NET 9.0             |    42.588 ns |     152 B |
| TypeMapperEmpty           | .NET Framework 4.6.2 |    10.735 ns |      64 B |
| DirectAccessEmpty         | .NET Framework 4.6.2 |    13.983 ns |      64 B |
| TypeMapperAddFireRemove   | .NET Framework 4.6.2 |    64.685 ns |     225 B |
| DirectAccessAddFireRemove | .NET Framework 4.6.2 |    64.247 ns |     193 B |
| TypeMapperSubscribed      | .NET Framework 4.6.2 |    67.018 ns |      96 B |
| DirectAccessSubscribed    | .NET Framework 4.6.2 |    14.533 ns |      64 B |
| TypeMapperAddRemove       | .NET Framework 4.6.2 |   608.619 ns |     345 B |
| DirectAccessAddRemove     | .NET Framework 4.6.2 |    70.396 ns |     152 B |
