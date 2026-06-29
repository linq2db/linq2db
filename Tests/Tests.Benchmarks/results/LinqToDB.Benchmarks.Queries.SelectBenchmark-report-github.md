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
| Method                | Runtime              | Mean         | Allocated |
|---------------------- |--------------------- |-------------:|----------:|
| Linq                  | .NET 8.0             | 15,677.81 ns |    6720 B |
| Compiled              | .NET 8.0             |  3,644.04 ns |    3168 B |
| FromSql_Interpolation | .NET 8.0             | 24,290.29 ns |   13920 B |
| FromSql_Formattable   | .NET 8.0             | 20,543.42 ns |   13792 B |
| Query                 | .NET 8.0             |  1,028.07 ns |     736 B |
| Execute               | .NET 8.0             |    950.47 ns |     608 B |
| RawAdoNet             | .NET 8.0             |    204.58 ns |     304 B |
| Linq                  | .NET 9.0             | 13,162.18 ns |    6496 B |
| Compiled              | .NET 9.0             |  2,724.89 ns |    3168 B |
| FromSql_Interpolation | .NET 9.0             | 24,035.31 ns |   13856 B |
| FromSql_Formattable   | .NET 9.0             | 23,651.55 ns |   13728 B |
| Query                 | .NET 9.0             |    915.78 ns |     736 B |
| Execute               | .NET 9.0             |    764.97 ns |     608 B |
| RawAdoNet             | .NET 9.0             |     73.38 ns |     304 B |
| Linq                  | .NET Framework 4.6.2 | 61,385.13 ns |   10432 B |
| Compiled              | .NET Framework 4.6.2 |  9,400.36 ns |    3241 B |
| FromSql_Interpolation | .NET Framework 4.6.2 | 84,160.39 ns |   17057 B |
| FromSql_Formattable   | .NET Framework 4.6.2 | 37,778.09 ns |   16930 B |
| Query                 | .NET Framework 4.6.2 |  1,266.57 ns |     770 B |
| Execute               | .NET Framework 4.6.2 |  2,653.56 ns |     642 B |
| RawAdoNet             | .NET Framework 4.6.2 |    280.80 ns |     393 B |
