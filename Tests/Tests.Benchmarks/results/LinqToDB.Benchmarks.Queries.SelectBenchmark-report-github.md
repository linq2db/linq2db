```

BenchmarkDotNet v0.13.12, Windows 10 (10.0.17763.5696/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK 8.0.204
  [Host]     : .NET 8.0.4 (8.0.424.16909), X64 RyuJIT AVX2
  Job-VZLGGZ : .NET 6.0.29 (6.0.2924.17105), X64 RyuJIT AVX2
  Job-AZKKUX : .NET 8.0.4 (8.0.424.16909), X64 RyuJIT AVX2
  Job-TQCFWV : .NET Framework 4.8 (4.8.4645.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method                | Runtime              | Mean        | Allocated |
|---------------------- |--------------------- |------------:|----------:|
| Linq                  | .NET 6.0             | 31,611.6 ns |   10000 B |
| Compiled              | .NET 6.0             |  5,494.5 ns |    3072 B |
| FromSql_Interpolation | .NET 6.0             | 17,474.1 ns |    5824 B |
| FromSql_Formattable   | .NET 6.0             | 15,029.4 ns |    5696 B |
| Query                 | .NET 6.0             |  1,557.6 ns |     736 B |
| Execute               | .NET 6.0             |  1,384.4 ns |     608 B |
| RawAdoNet             | .NET 6.0             |    217.9 ns |     304 B |
| Linq                  | .NET 8.0             | 15,414.7 ns |    6656 B |
| Compiled              | .NET 8.0             |  3,737.8 ns |    3072 B |
| FromSql_Interpolation | .NET 8.0             |  7,719.6 ns |    4736 B |
| FromSql_Formattable   | .NET 8.0             |  8,265.1 ns |    4608 B |
| Query                 | .NET 8.0             |  1,029.7 ns |     736 B |
| Execute               | .NET 8.0             |    936.6 ns |     608 B |
| RawAdoNet             | .NET 8.0             |    166.8 ns |     304 B |
| Linq                  | .NET Framework 4.6.2 | 52,326.8 ns |   10608 B |
| Compiled              | .NET Framework 4.6.2 |  8,456.7 ns |    3145 B |
| FromSql_Interpolation | .NET Framework 4.6.2 | 24,505.1 ns |    5809 B |
| FromSql_Formattable   | .NET Framework 4.6.2 | 23,443.5 ns |    5681 B |
| Query                 | .NET Framework 4.6.2 |  2,898.8 ns |     770 B |
| Execute               | .NET Framework 4.6.2 |  2,505.5 ns |     642 B |
| RawAdoNet             | .NET Framework 4.6.2 |    676.6 ns |     393 B |
