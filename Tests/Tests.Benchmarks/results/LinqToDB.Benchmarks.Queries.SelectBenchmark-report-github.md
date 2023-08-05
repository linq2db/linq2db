``` ini

BenchmarkDotNet=v0.13.5, OS=Windows 10 (10.0.17763.4010/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.201
  [Host]     : .NET 7.0.3 (7.0.323.6910), X64 RyuJIT AVX2
  Job-ZOLDKB : .NET 6.0.14 (6.0.1423.7309), X64 RyuJIT AVX2
  Job-EHWHZK : .NET 7.0.3 (7.0.323.6910), X64 RyuJIT AVX2
  Job-LWJRKG : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-AGOWOF : .NET Framework 4.8 (4.8.4614.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                Method |              Runtime |        Mean | Allocated |
|---------------------- |--------------------- |------------:|----------:|
|                  Linq |             .NET 6.0 | 38,189.5 ns |   11984 B |
|              Compiled |             .NET 6.0 |  5,240.0 ns |    3088 B |
| FromSql_Interpolation |             .NET 6.0 | 16,805.6 ns |    6736 B |
|   FromSql_Formattable |             .NET 6.0 | 17,862.0 ns |    7040 B |
|                 Query |             .NET 6.0 |  1,392.1 ns |     704 B |
|               Execute |             .NET 6.0 |  1,204.9 ns |     576 B |
|             RawAdoNet |             .NET 6.0 |    230.7 ns |     304 B |
|                  Linq |             .NET 7.0 | 30,433.8 ns |    8304 B |
|              Compiled |             .NET 7.0 |  5,649.8 ns |    3088 B |
| FromSql_Interpolation |             .NET 7.0 |  9,804.8 ns |    5040 B |
|   FromSql_Formattable |             .NET 7.0 | 11,679.1 ns |    5344 B |
|                 Query |             .NET 7.0 |  1,269.2 ns |     704 B |
|               Execute |             .NET 7.0 |  1,327.0 ns |     576 B |
|             RawAdoNet |             .NET 7.0 |    180.0 ns |     304 B |
|                  Linq |        .NET Core 3.1 | 27,787.8 ns |   12577 B |
|              Compiled |        .NET Core 3.1 |  6,854.9 ns |    3056 B |
| FromSql_Interpolation |        .NET Core 3.1 | 19,877.1 ns |    6688 B |
|   FromSql_Formattable |        .NET Core 3.1 | 20,926.1 ns |    6992 B |
|                 Query |        .NET Core 3.1 |  2,049.2 ns |     704 B |
|               Execute |        .NET Core 3.1 |  1,962.9 ns |     576 B |
|             RawAdoNet |        .NET Core 3.1 |    521.8 ns |     328 B |
|                  Linq | .NET Framework 4.7.2 | 75,960.0 ns |   13964 B |
|              Compiled | .NET Framework 4.7.2 |  9,231.9 ns |    3161 B |
| FromSql_Interpolation | .NET Framework 4.7.2 | 25,716.1 ns |    6499 B |
|   FromSql_Formattable | .NET Framework 4.7.2 | 27,851.5 ns |    6820 B |
|                 Query | .NET Framework 4.7.2 |  2,304.8 ns |     738 B |
|               Execute | .NET Framework 4.7.2 |  2,440.3 ns |     610 B |
|             RawAdoNet | .NET Framework 4.7.2 |    624.2 ns |     393 B |
