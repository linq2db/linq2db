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
| TypeMapperAsEnum        | .NET 8.0             |  9.8490 ns |         - |
| DirectAccessAsEnum      | .NET 8.0             |  0.3317 ns |         - |
| TypeMapperAsKnownEnum   | .NET 8.0             |  1.8249 ns |         - |
| DirectAccessAsKnownEnum | .NET 8.0             |  0.3631 ns |         - |
| TypeMapperAsString      | .NET 8.0             |  5.1196 ns |         - |
| DirectAccessAsString    | .NET 8.0             |  2.2958 ns |         - |
| TypeMapperAsEnum        | .NET 9.0             |  7.8861 ns |         - |
| DirectAccessAsEnum      | .NET 9.0             |  0.4235 ns |         - |
| TypeMapperAsKnownEnum   | .NET 9.0             |  2.0822 ns |         - |
| DirectAccessAsKnownEnum | .NET 9.0             |  0.4696 ns |         - |
| TypeMapperAsString      | .NET 9.0             |  4.7928 ns |         - |
| DirectAccessAsString    | .NET 9.0             |  2.4301 ns |         - |
| TypeMapperAsEnum        | .NET Framework 4.6.2 | 32.1450 ns |         - |
| DirectAccessAsEnum      | .NET Framework 4.6.2 |  1.3772 ns |         - |
| TypeMapperAsKnownEnum   | .NET Framework 4.6.2 |  3.1828 ns |         - |
| DirectAccessAsKnownEnum | .NET Framework 4.6.2 |  1.4397 ns |         - |
| TypeMapperAsString      | .NET Framework 4.6.2 | 12.6154 ns |         - |
| DirectAccessAsString    | .NET Framework 4.6.2 |  4.1398 ns |         - |
