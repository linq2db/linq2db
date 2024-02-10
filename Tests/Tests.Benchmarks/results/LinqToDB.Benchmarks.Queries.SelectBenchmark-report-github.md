```

BenchmarkDotNet v0.13.9+228a464e8be6c580ad9408e98f18813f6407fb5a, Windows 10 (10.0.17763.5328/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK 8.0.101
  [Host]     : .NET 7.0.15 (7.0.1523.57226), X64 RyuJIT AVX2
  Job-KJWIMT : .NET 6.0.26 (6.0.2623.60508), X64 RyuJIT AVX2
  Job-GULBRG : .NET 7.0.15 (7.0.1523.57226), X64 RyuJIT AVX2
  Job-LRGNRQ : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-SJROSW : .NET Framework 4.8 (4.8.4645.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method                | Runtime              | Mean        | Allocated |
|---------------------- |--------------------- |------------:|----------:|
| Linq                  | .NET 6.0             | 46,900.9 ns |   11952 B |
| Compiled              | .NET 6.0             |  5,812.2 ns |    3104 B |
| FromSql_Interpolation | .NET 6.0             | 17,443.7 ns |    6704 B |
| FromSql_Formattable   | .NET 6.0             | 18,629.0 ns |    7200 B |
| Query                 | .NET 6.0             |    988.7 ns |     704 B |
| Execute               | .NET 6.0             |  1,382.5 ns |     576 B |
| RawAdoNet             | .NET 6.0             |    216.1 ns |     304 B |
| Linq                  | .NET 7.0             | 29,852.3 ns |    8272 B |
| Compiled              | .NET 7.0             |  5,425.2 ns |    3104 B |
| FromSql_Interpolation | .NET 7.0             | 10,307.1 ns |    5008 B |
| FromSql_Formattable   | .NET 7.0             | 11,952.9 ns |    5504 B |
| Query                 | .NET 7.0             |    646.7 ns |     704 B |
| Execute               | .NET 7.0             |  1,385.0 ns |     576 B |
| RawAdoNet             | .NET 7.0             |    211.4 ns |     304 B |
| Linq                  | .NET Core 3.1        | 58,396.8 ns |   12896 B |
| Compiled              | .NET Core 3.1        |  7,433.8 ns |    3072 B |
| FromSql_Interpolation | .NET Core 3.1        | 20,181.8 ns |    6656 B |
| FromSql_Formattable   | .NET Core 3.1        | 21,090.9 ns |    7152 B |
| Query                 | .NET Core 3.1        |  1,694.9 ns |     704 B |
| Execute               | .NET Core 3.1        |  1,745.3 ns |     576 B |
| RawAdoNet             | .NET Core 3.1        |    488.4 ns |     328 B |
| Linq                  | .NET Framework 4.7.2 | 91,331.5 ns |   13931 B |
| Compiled              | .NET Framework 4.7.2 |  9,215.6 ns |    3177 B |
| FromSql_Interpolation | .NET Framework 4.7.2 | 25,258.1 ns |    6467 B |
| FromSql_Formattable   | .NET Framework 4.7.2 | 23,571.4 ns |    6981 B |
| Query                 | .NET Framework 4.7.2 |  2,633.7 ns |     738 B |
| Execute               | .NET Framework 4.7.2 |  2,454.2 ns |     610 B |
| RawAdoNet             | .NET Framework 4.7.2 |    569.1 ns |     393 B |
