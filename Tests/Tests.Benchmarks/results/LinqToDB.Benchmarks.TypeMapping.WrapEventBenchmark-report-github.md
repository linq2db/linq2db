```

BenchmarkDotNet v0.14.0, Windows 10 (10.0.17763.6766/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4775.0), X64 RyuJIT VectorSize=256
  Job-GEKMDY : .NET 6.0.36 (6.0.3624.51421), X64 RyuJIT AVX2
  Job-WEIMGV : .NET 8.0.12 (8.0.1224.60305), X64 RyuJIT AVX2
  Job-ARZZBJ : .NET 9.0.1 (9.0.124.61010), X64 RyuJIT AVX2
  Job-HBTJES : .NET Framework 4.8 (4.8.4775.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method                    | Runtime              | Mean         | Allocated |
|-------------------------- |--------------------- |-------------:|----------:|
| TypeMapperEmpty           | .NET 6.0             |    13.002 ns |      64 B |
| DirectAccessEmpty         | .NET 6.0             |     9.973 ns |      64 B |
| TypeMapperAddFireRemove   | .NET 6.0             |   124.606 ns |     224 B |
| DirectAccessAddFireRemove | .NET 6.0             |    86.848 ns |     192 B |
| TypeMapperSubscribed      | .NET 6.0             |    54.793 ns |      96 B |
| DirectAccessSubscribed    | .NET 6.0             |    11.611 ns |      64 B |
| TypeMapperAddRemove       | .NET 6.0             | 1,144.367 ns |     344 B |
| DirectAccessAddRemove     | .NET 6.0             |    73.516 ns |     152 B |
| TypeMapperEmpty           | .NET 8.0             |    12.507 ns |      64 B |
| DirectAccessEmpty         | .NET 8.0             |     9.777 ns |      64 B |
| TypeMapperAddFireRemove   | .NET 8.0             |    92.160 ns |     224 B |
| DirectAccessAddFireRemove | .NET 8.0             |    47.524 ns |     192 B |
| TypeMapperSubscribed      | .NET 8.0             |    39.921 ns |      96 B |
| DirectAccessSubscribed    | .NET 8.0             |     9.928 ns |      64 B |
| TypeMapperAddRemove       | .NET 8.0             | 1,020.496 ns |     344 B |
| DirectAccessAddRemove     | .NET 8.0             |    42.712 ns |     152 B |
| TypeMapperEmpty           | .NET 9.0             |    36.528 ns |      64 B |
| DirectAccessEmpty         | .NET 9.0             |     8.568 ns |      64 B |
| TypeMapperAddFireRemove   | .NET 9.0             |    78.481 ns |     224 B |
| DirectAccessAddFireRemove | .NET 9.0             |    49.320 ns |     192 B |
| TypeMapperSubscribed      | .NET 9.0             |    43.449 ns |      96 B |
| DirectAccessSubscribed    | .NET 9.0             |     9.676 ns |      64 B |
| TypeMapperAddRemove       | .NET 9.0             |   877.680 ns |     344 B |
| DirectAccessAddRemove     | .NET 9.0             |    42.449 ns |     152 B |
| TypeMapperEmpty           | .NET Framework 4.6.2 |    17.850 ns |      64 B |
| DirectAccessEmpty         | .NET Framework 4.6.2 |    15.008 ns |      64 B |
| TypeMapperAddFireRemove   | .NET Framework 4.6.2 |   147.676 ns |     225 B |
| DirectAccessAddFireRemove | .NET Framework 4.6.2 |    88.592 ns |     193 B |
| TypeMapperSubscribed      | .NET Framework 4.6.2 |    81.428 ns |      96 B |
| DirectAccessSubscribed    | .NET Framework 4.6.2 |    16.806 ns |      64 B |
| TypeMapperAddRemove       | .NET Framework 4.6.2 | 1,069.951 ns |     345 B |
| DirectAccessAddRemove     | .NET Framework 4.6.2 |    74.150 ns |     152 B |
