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
| Method                  | Runtime              | Mean       | Allocated |
|------------------------ |--------------------- |-----------:|----------:|
| TypeMapperAsEnum        | .NET 8.0             | 10.2623 ns |         - |
| DirectAccessAsEnum      | .NET 8.0             |  0.4806 ns |         - |
| TypeMapperAsKnownEnum   | .NET 8.0             |  1.8244 ns |         - |
| DirectAccessAsKnownEnum | .NET 8.0             |  0.5138 ns |         - |
| TypeMapperAsString      | .NET 8.0             |  4.2334 ns |         - |
| DirectAccessAsString    | .NET 8.0             |  3.5053 ns |         - |
| TypeMapperAsEnum        | .NET 9.0             |  7.7131 ns |         - |
| DirectAccessAsEnum      | .NET 9.0             |  0.3594 ns |         - |
| TypeMapperAsKnownEnum   | .NET 9.0             |  1.5105 ns |         - |
| DirectAccessAsKnownEnum | .NET 9.0             |  1.1341 ns |         - |
| TypeMapperAsString      | .NET 9.0             |  5.0305 ns |         - |
| DirectAccessAsString    | .NET 9.0             |  3.6498 ns |         - |
| TypeMapperAsEnum        | .NET Framework 4.6.2 | 31.0884 ns |         - |
| DirectAccessAsEnum      | .NET Framework 4.6.2 |  2.2210 ns |         - |
| TypeMapperAsKnownEnum   | .NET Framework 4.6.2 |  6.8516 ns |         - |
| DirectAccessAsKnownEnum | .NET Framework 4.6.2 |  2.2642 ns |         - |
| TypeMapperAsString      | .NET Framework 4.6.2 | 12.5798 ns |         - |
| DirectAccessAsString    | .NET Framework 4.6.2 |  4.8759 ns |         - |
