```

BenchmarkDotNet v0.13.12, Windows 10 (10.0.17763.5458/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK 8.0.200
  [Host]     : .NET 8.0.2 (8.0.224.6711), X64 RyuJIT AVX2
  Job-GXDOCB : .NET 6.0.27 (6.0.2724.6912), X64 RyuJIT AVX2
  Job-YDFVLV : .NET 8.0.2 (8.0.224.6711), X64 RyuJIT AVX2
  Job-SBTNYY : .NET Framework 4.8 (4.8.4645.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method                | Runtime              | Mean        | Allocated |
|---------------------- |--------------------- |------------:|----------:|
| Linq                  | .NET 6.0             | 27,623.4 ns |    9712 B |
| Compiled              | .NET 6.0             |  5,618.5 ns |    3072 B |
| FromSql_Interpolation | .NET 6.0             | 16,425.8 ns |    5824 B |
| FromSql_Formattable   | .NET 6.0             | 16,051.8 ns |    5696 B |
| Query                 | .NET 6.0             |  1,447.0 ns |     704 B |
| Execute               | .NET 6.0             |  1,348.7 ns |     576 B |
| RawAdoNet             | .NET 6.0             |    229.8 ns |     304 B |
| Linq                  | .NET 8.0             | 14,822.9 ns |    6720 B |
| Compiled              | .NET 8.0             |  3,475.3 ns |    3072 B |
| FromSql_Interpolation | .NET 8.0             |  8,358.9 ns |    4736 B |
| FromSql_Formattable   | .NET 8.0             |  8,167.4 ns |    4608 B |
| Query                 | .NET 8.0             |    428.7 ns |     704 B |
| Execute               | .NET 8.0             |    850.5 ns |     576 B |
| RawAdoNet             | .NET 8.0             |    163.8 ns |     304 B |
| Linq                  | .NET Framework 4.6.2 | 58,294.4 ns |   11057 B |
| Compiled              | .NET Framework 4.6.2 |  8,861.2 ns |    3145 B |
| FromSql_Interpolation | .NET Framework 4.6.2 | 23,900.1 ns |    5809 B |
| FromSql_Formattable   | .NET Framework 4.6.2 | 22,933.0 ns |    5681 B |
| Query                 | .NET Framework 4.6.2 |  2,496.8 ns |     738 B |
| Execute               | .NET Framework 4.6.2 |  2,344.2 ns |     610 B |
| RawAdoNet             | .NET Framework 4.6.2 |    640.5 ns |     393 B |
