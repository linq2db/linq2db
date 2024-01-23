```

BenchmarkDotNet v0.13.9+228a464e8be6c580ad9408e98f18813f6407fb5a, Windows 10 (10.0.17763.4644/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK 7.0.401
  [Host]     : .NET 7.0.11 (7.0.1123.42427), X64 RyuJIT AVX2
  Job-DAXXNM : .NET 6.0.22 (6.0.2223.42425), X64 RyuJIT AVX2
  Job-SLTPYD : .NET 7.0.11 (7.0.1123.42427), X64 RyuJIT AVX2
  Job-YOWJJJ : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-OZLLFF : .NET Framework 4.8 (4.8.4645.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method                | Runtime              | Mean        | Allocated |
|---------------------- |--------------------- |------------:|----------:|
| Linq                  | .NET 6.0             | 45,964.6 ns |   11920 B |
| Compiled              | .NET 6.0             |  5,425.2 ns |    3072 B |
| FromSql_Interpolation | .NET 6.0             | 16,513.7 ns |    6672 B |
| FromSql_Formattable   | .NET 6.0             | 17,158.6 ns |    7168 B |
| Query                 | .NET 6.0             |  1,412.1 ns |     704 B |
| Execute               | .NET 6.0             |  1,070.0 ns |     576 B |
| RawAdoNet             | .NET 6.0             |    224.9 ns |     304 B |
| Linq                  | .NET 7.0             | 30,227.9 ns |    8240 B |
| Compiled              | .NET 7.0             |  5,269.8 ns |    3072 B |
| FromSql_Interpolation | .NET 7.0             | 10,297.2 ns |    4976 B |
| FromSql_Formattable   | .NET 7.0             |  6,401.0 ns |    5472 B |
| Query                 | .NET 7.0             |  1,469.1 ns |     704 B |
| Execute               | .NET 7.0             |  1,398.2 ns |     576 B |
| RawAdoNet             | .NET 7.0             |    209.5 ns |     304 B |
| Linq                  | .NET Core 3.1        | 60,402.7 ns |   12512 B |
| Compiled              | .NET Core 3.1        |  7,397.7 ns |    3040 B |
| FromSql_Interpolation | .NET Core 3.1        | 19,262.4 ns |    6624 B |
| FromSql_Formattable   | .NET Core 3.1        | 20,374.8 ns |    7120 B |
| Query                 | .NET Core 3.1        |  2,126.7 ns |     704 B |
| Execute               | .NET Core 3.1        |  1,765.3 ns |     576 B |
| RawAdoNet             | .NET Core 3.1        |    490.4 ns |     328 B |
| Linq                  | .NET Framework 4.7.2 | 90,047.4 ns |   13899 B |
| Compiled              | .NET Framework 4.7.2 |  3,964.9 ns |    3145 B |
| FromSql_Interpolation | .NET Framework 4.7.2 | 25,052.7 ns |    6435 B |
| FromSql_Formattable   | .NET Framework 4.7.2 | 26,931.8 ns |    6949 B |
| Query                 | .NET Framework 4.7.2 |  2,670.7 ns |     738 B |
| Execute               | .NET Framework 4.7.2 |  2,103.3 ns |     610 B |
| RawAdoNet             | .NET Framework 4.7.2 |    647.7 ns |     393 B |
