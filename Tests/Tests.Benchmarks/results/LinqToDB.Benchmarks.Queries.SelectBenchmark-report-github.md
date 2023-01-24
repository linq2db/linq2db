``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-HCNGBR : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-XBFFOD : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-INBZNN : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-THZJXI : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                Method |              Runtime |        Mean | Allocated |
|---------------------- |--------------------- |------------:|----------:|
|                  Linq |             .NET 6.0 | 52,888.8 ns |   14752 B |
|              Compiled |             .NET 6.0 |  4,877.7 ns |    3088 B |
| FromSql_Interpolation |             .NET 6.0 | 16,666.8 ns |    6736 B |
|   FromSql_Formattable |             .NET 6.0 | 17,911.9 ns |    7040 B |
|                 Query |             .NET 6.0 |  1,258.3 ns |     704 B |
|               Execute |             .NET 6.0 |  1,325.5 ns |     576 B |
|             RawAdoNet |             .NET 6.0 |    229.5 ns |     304 B |
|                  Linq |             .NET 7.0 | 30,693.3 ns |    8304 B |
|              Compiled |             .NET 7.0 |  5,694.5 ns |    3088 B |
| FromSql_Interpolation |             .NET 7.0 | 10,349.4 ns |    5040 B |
|   FromSql_Formattable |             .NET 7.0 | 12,020.0 ns |    5344 B |
|                 Query |             .NET 7.0 |  1,295.5 ns |     704 B |
|               Execute |             .NET 7.0 |  1,377.9 ns |     576 B |
|             RawAdoNet |             .NET 7.0 |    217.3 ns |     304 B |
|                  Linq |        .NET Core 3.1 | 61,597.5 ns |   12576 B |
|              Compiled |        .NET Core 3.1 |  7,776.5 ns |    3056 B |
| FromSql_Interpolation |        .NET Core 3.1 | 19,783.9 ns |    6688 B |
|   FromSql_Formattable |        .NET Core 3.1 | 21,413.8 ns |    6992 B |
|                 Query |        .NET Core 3.1 |  1,998.7 ns |     704 B |
|               Execute |        .NET Core 3.1 |  1,953.0 ns |     576 B |
|             RawAdoNet |        .NET Core 3.1 |    505.3 ns |     328 B |
|                  Linq | .NET Framework 4.7.2 | 93,222.8 ns |   13964 B |
|              Compiled | .NET Framework 4.7.2 |  8,638.0 ns |    3161 B |
| FromSql_Interpolation | .NET Framework 4.7.2 | 25,983.0 ns |    6499 B |
|   FromSql_Formattable | .NET Framework 4.7.2 | 27,133.8 ns |    6820 B |
|                 Query | .NET Framework 4.7.2 |  2,341.1 ns |     738 B |
|               Execute | .NET Framework 4.7.2 |  2,291.6 ns |     610 B |
|             RawAdoNet | .NET Framework 4.7.2 |    625.0 ns |     393 B |
