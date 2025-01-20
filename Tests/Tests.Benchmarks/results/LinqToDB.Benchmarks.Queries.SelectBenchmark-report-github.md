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
| Method                | Runtime              | Mean        | Allocated |
|---------------------- |--------------------- |------------:|----------:|
| Linq                  | .NET 6.0             | 31,290.8 ns |    9456 B |
| Compiled              | .NET 6.0             |  5,360.5 ns |    3152 B |
| FromSql_Interpolation | .NET 6.0             | 41,042.6 ns |   14256 B |
| FromSql_Formattable   | .NET 6.0             | 38,612.0 ns |   14128 B |
| Query                 | .NET 6.0             |  1,576.6 ns |     736 B |
| Execute               | .NET 6.0             |  1,573.9 ns |     608 B |
| RawAdoNet             | .NET 6.0             |    218.4 ns |     304 B |
| Linq                  | .NET 8.0             | 14,303.7 ns |    6464 B |
| Compiled              | .NET 8.0             |  3,731.1 ns |    3152 B |
| FromSql_Interpolation | .NET 8.0             | 19,829.9 ns |   13088 B |
| FromSql_Formattable   | .NET 8.0             |  9,379.3 ns |   12960 B |
| Query                 | .NET 8.0             |    447.8 ns |     736 B |
| Execute               | .NET 8.0             |    887.9 ns |     608 B |
| RawAdoNet             | .NET 8.0             |    177.6 ns |     304 B |
| Linq                  | .NET 9.0             | 13,037.7 ns |    6480 B |
| Compiled              | .NET 9.0             |  2,887.8 ns |    3152 B |
| FromSql_Interpolation | .NET 9.0             | 16,900.7 ns |   13024 B |
| FromSql_Formattable   | .NET 9.0             | 18,061.6 ns |   12896 B |
| Query                 | .NET 9.0             |    921.0 ns |     736 B |
| Execute               | .NET 9.0             |    789.4 ns |     608 B |
| RawAdoNet             | .NET 9.0             |    138.2 ns |     304 B |
| Linq                  | .NET Framework 4.6.2 | 58,994.1 ns |   10928 B |
| Compiled              | .NET Framework 4.6.2 |  9,412.5 ns |    3226 B |
| FromSql_Interpolation | .NET Framework 4.6.2 | 66,221.4 ns |   15101 B |
| FromSql_Formattable   | .NET Framework 4.6.2 | 58,756.4 ns |   14973 B |
| Query                 | .NET Framework 4.6.2 |  2,922.7 ns |     770 B |
| Execute               | .NET Framework 4.6.2 |  2,793.7 ns |     642 B |
| RawAdoNet             | .NET Framework 4.6.2 |    607.5 ns |     393 B |
