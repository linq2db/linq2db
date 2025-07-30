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
| Linq                  | .NET 8.0             | 22,894.05 ns |    8400 B |
| Compiled              | .NET 8.0             |  2,737.95 ns |    3104 B |
| FromSql_Interpolation | .NET 8.0             |  3,409.13 ns |    5008 B |
| FromSql_Formattable   | .NET 8.0             |  7,448.48 ns |    5504 B |
| Query                 | .NET 8.0             |    771.98 ns |     704 B |
| Execute               | .NET 8.0             |    870.35 ns |     576 B |
| RawAdoNet             | .NET 8.0             |     89.37 ns |     304 B |
| Linq                  | .NET 9.0             | 18,070.39 ns |    8416 B |
| Compiled              | .NET 9.0             |  2,935.87 ns |    3104 B |
| FromSql_Interpolation | .NET 9.0             |  6,595.68 ns |    5008 B |
| FromSql_Formattable   | .NET 9.0             |  7,784.17 ns |    5504 B |
| Query                 | .NET 9.0             |    803.54 ns |     704 B |
| Execute               | .NET 9.0             |    734.91 ns |     576 B |
| RawAdoNet             | .NET 9.0             |    153.10 ns |     304 B |
| Linq                  | .NET Framework 4.6.2 | 71,467.92 ns |   13544 B |
| Compiled              | .NET Framework 4.6.2 |  9,121.64 ns |    3225 B |
| FromSql_Interpolation | .NET Framework 4.6.2 | 24,387.46 ns |    6467 B |
| FromSql_Formattable   | .NET Framework 4.6.2 | 18,182.98 ns |    6980 B |
| Query                 | .NET Framework 4.6.2 |  2,490.61 ns |     738 B |
| Execute               | .NET Framework 4.6.2 |  2,134.63 ns |     610 B |
| RawAdoNet             | .NET Framework 4.6.2 |    447.40 ns |     393 B |
