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
| TypeMapperString    | .NET 8.0             |  3.7384 ns |         - |
| DirectAccessString  | .NET 8.0             |  0.4370 ns |         - |
| TypeMapperInt       | .NET 8.0             |  2.6698 ns |         - |
| DirectAccessInt     | .NET 8.0             |  0.4720 ns |         - |
| TypeMapperLong      | .NET 8.0             |  3.2684 ns |         - |
| DirectAccessLong    | .NET 8.0             |  0.4176 ns |         - |
| TypeMapperBoolean   | .NET 8.0             |  3.1776 ns |         - |
| DirectAccessBoolean | .NET 8.0             |  0.7969 ns |         - |
| TypeMapperWrapper   | .NET 8.0             |  5.6176 ns |         - |
| DirectAccessWrapper | .NET 8.0             |  0.0000 ns |         - |
| TypeMapperEnum      | .NET 8.0             | 12.7216 ns |         - |
| DirectAccessEnum    | .NET 8.0             |  0.2208 ns |         - |
| TypeMapperVersion   | .NET 8.0             |  3.1219 ns |         - |
| DirectAccessVersion | .NET 8.0             |  0.3247 ns |         - |
| TypeMapperString    | .NET 9.0             |  3.5012 ns |         - |
| DirectAccessString  | .NET 9.0             |  0.4417 ns |         - |
| TypeMapperInt       | .NET 9.0             |  3.5691 ns |         - |
| DirectAccessInt     | .NET 9.0             |  0.4956 ns |         - |
| TypeMapperLong      | .NET 9.0             |  1.8422 ns |         - |
| DirectAccessLong    | .NET 9.0             |  0.5855 ns |         - |
| TypeMapperBoolean   | .NET 9.0             |  3.9864 ns |         - |
| DirectAccessBoolean | .NET 9.0             |  0.5832 ns |         - |
| TypeMapperWrapper   | .NET 9.0             |  7.5353 ns |         - |
| DirectAccessWrapper | .NET 9.0             |  3.0309 ns |         - |
| TypeMapperEnum      | .NET 9.0             |  4.3611 ns |         - |
| DirectAccessEnum    | .NET 9.0             |  0.3175 ns |         - |
| TypeMapperVersion   | .NET 9.0             |  2.2813 ns |         - |
| DirectAccessVersion | .NET 9.0             |  0.5788 ns |         - |
| TypeMapperString    | .NET Framework 4.6.2 | 18.1594 ns |         - |
| DirectAccessString  | .NET Framework 4.6.2 |  0.7234 ns |         - |
| TypeMapperInt       | .NET Framework 4.6.2 | 15.0901 ns |         - |
| DirectAccessInt     | .NET Framework 4.6.2 |  2.1150 ns |         - |
| TypeMapperLong      | .NET Framework 4.6.2 | 23.9114 ns |         - |
| DirectAccessLong    | .NET Framework 4.6.2 |  0.9273 ns |         - |
| TypeMapperBoolean   | .NET Framework 4.6.2 | 21.7645 ns |         - |
| DirectAccessBoolean | .NET Framework 4.6.2 |  0.8587 ns |         - |
| TypeMapperWrapper   | .NET Framework 4.6.2 | 32.7461 ns |         - |
| DirectAccessWrapper | .NET Framework 4.6.2 |  1.0307 ns |         - |
| TypeMapperEnum      | .NET Framework 4.6.2 | 57.1753 ns |      24 B |
| DirectAccessEnum    | .NET Framework 4.6.2 |  2.7795 ns |         - |
| TypeMapperVersion   | .NET Framework 4.6.2 | 22.7768 ns |         - |
| DirectAccessVersion | .NET Framework 4.6.2 |  0.8325 ns |         - |
