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
| Method              | Runtime              | Mean       | Allocated |
|-------------------- |--------------------- |-----------:|----------:|
| TypeMapperString    | .NET 8.0             |  2.4094 ns |         - |
| DirectAccessString  | .NET 8.0             |  0.1891 ns |         - |
| TypeMapperInt       | .NET 8.0             |  4.3971 ns |         - |
| DirectAccessInt     | .NET 8.0             |  0.4522 ns |         - |
| TypeMapperLong      | .NET 8.0             |  4.2242 ns |         - |
| DirectAccessLong    | .NET 8.0             |  0.4762 ns |         - |
| TypeMapperBoolean   | .NET 8.0             |  3.1821 ns |         - |
| DirectAccessBoolean | .NET 8.0             |  0.4667 ns |         - |
| TypeMapperWrapper   | .NET 8.0             |  7.6297 ns |         - |
| DirectAccessWrapper | .NET 8.0             |  0.1830 ns |         - |
| TypeMapperEnum      | .NET 8.0             | 12.9682 ns |         - |
| DirectAccessEnum    | .NET 8.0             |  0.4023 ns |         - |
| TypeMapperVersion   | .NET 8.0             |  3.2820 ns |         - |
| DirectAccessVersion | .NET 8.0             |  0.2152 ns |         - |
| TypeMapperString    | .NET 9.0             |  2.1726 ns |         - |
| DirectAccessString  | .NET 9.0             |  0.4939 ns |         - |
| TypeMapperInt       | .NET 9.0             |  5.0302 ns |         - |
| DirectAccessInt     | .NET 9.0             |  0.5130 ns |         - |
| TypeMapperLong      | .NET 9.0             |  3.1693 ns |         - |
| DirectAccessLong    | .NET 9.0             |  0.7365 ns |         - |
| TypeMapperBoolean   | .NET 9.0             |  3.1643 ns |         - |
| DirectAccessBoolean | .NET 9.0             |  0.4194 ns |         - |
| TypeMapperWrapper   | .NET 9.0             |  7.4436 ns |         - |
| DirectAccessWrapper | .NET 9.0             |  0.3154 ns |         - |
| TypeMapperEnum      | .NET 9.0             | 13.2242 ns |         - |
| DirectAccessEnum    | .NET 9.0             |  0.4483 ns |         - |
| TypeMapperVersion   | .NET 9.0             |  0.0000 ns |         - |
| DirectAccessVersion | .NET 9.0             |  0.3865 ns |         - |
| TypeMapperString    | .NET Framework 4.6.2 | 21.8176 ns |         - |
| DirectAccessString  | .NET Framework 4.6.2 |  1.5563 ns |         - |
| TypeMapperInt       | .NET Framework 4.6.2 | 22.4397 ns |         - |
| DirectAccessInt     | .NET Framework 4.6.2 |  1.9634 ns |         - |
| TypeMapperLong      | .NET Framework 4.6.2 | 16.4598 ns |         - |
| DirectAccessLong    | .NET Framework 4.6.2 |  0.4187 ns |         - |
| TypeMapperBoolean   | .NET Framework 4.6.2 | 22.1400 ns |         - |
| DirectAccessBoolean | .NET Framework 4.6.2 |  0.0000 ns |         - |
| TypeMapperWrapper   | .NET Framework 4.6.2 | 31.3655 ns |         - |
| DirectAccessWrapper | .NET Framework 4.6.2 |  3.5904 ns |         - |
| TypeMapperEnum      | .NET Framework 4.6.2 | 66.4727 ns |      24 B |
| DirectAccessEnum    | .NET Framework 4.6.2 |  0.9075 ns |         - |
| TypeMapperVersion   | .NET Framework 4.6.2 | 23.3390 ns |         - |
| DirectAccessVersion | .NET Framework 4.6.2 |  1.2928 ns |         - |
