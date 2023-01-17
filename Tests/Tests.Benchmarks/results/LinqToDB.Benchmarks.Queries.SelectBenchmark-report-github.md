``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-WUZRIO : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-EMBONI : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-HZWTXS : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-VIGHHX : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                Method |              Runtime |         Mean |       Median |  Ratio |   Gen0 | Allocated | Alloc Ratio |
|---------------------- |--------------------- |-------------:|-------------:|-------:|-------:|----------:|------------:|
|                  Linq |             .NET 6.0 |  61,777.3 ns |  61,873.7 ns | 101.35 | 0.7935 |   13632 B |       34.69 |
|              Compiled |             .NET 6.0 |   4,938.4 ns |   5,010.4 ns |   8.19 | 0.1831 |    3088 B |        7.86 |
| FromSql_Interpolation |             .NET 6.0 |  18,562.4 ns |  19,817.7 ns |  18.45 | 0.3967 |    6720 B |       17.10 |
|   FromSql_Formattable |             .NET 6.0 |  18,269.7 ns |  20,120.3 ns |  29.17 | 0.3967 |    7024 B |       17.87 |
|                 Query |             .NET 6.0 |   1,518.9 ns |   1,514.2 ns |   2.49 | 0.0420 |     704 B |        1.79 |
|               Execute |             .NET 6.0 |   1,108.1 ns |   1,385.3 ns |   1.87 | 0.0343 |     576 B |        1.47 |
|             RawAdoNet |             .NET 6.0 |     223.4 ns |     223.4 ns |   0.37 | 0.0181 |     304 B |        0.77 |
|                  Linq |             .NET 7.0 |  43,025.1 ns |  43,076.0 ns |  70.64 | 0.5493 |    9984 B |       25.40 |
|              Compiled |             .NET 7.0 |   5,547.4 ns |   5,593.8 ns |   9.11 | 0.1831 |    3088 B |        7.86 |
| FromSql_Interpolation |             .NET 7.0 |  12,033.4 ns |  13,426.9 ns |  20.58 | 0.3281 |    5520 B |       14.05 |
|   FromSql_Formattable |             .NET 7.0 |  14,844.5 ns |  14,840.2 ns |  24.35 | 0.3357 |    5824 B |       14.82 |
|                 Query |             .NET 7.0 |   1,416.1 ns |   1,431.1 ns |   2.19 | 0.0420 |     704 B |        1.79 |
|               Execute |             .NET 7.0 |   1,374.0 ns |   1,374.7 ns |   2.25 | 0.0343 |     576 B |        1.47 |
|             RawAdoNet |             .NET 7.0 |     200.0 ns |     208.5 ns |   0.33 | 0.0181 |     304 B |        0.77 |
|                  Linq |        .NET Core 3.1 |  75,272.2 ns |  75,283.2 ns | 123.52 | 0.7324 |   13169 B |       33.51 |
|              Compiled |        .NET Core 3.1 |   7,096.2 ns |   7,097.7 ns |  11.64 | 0.1755 |    3056 B |        7.78 |
| FromSql_Interpolation |        .NET Core 3.1 |  23,040.6 ns |  22,839.9 ns |  37.83 | 0.3967 |    6672 B |       16.98 |
|   FromSql_Formattable |        .NET Core 3.1 |  25,407.1 ns |  25,497.8 ns |  41.67 | 0.3967 |    6976 B |       17.75 |
|                 Query |        .NET Core 3.1 |   2,137.2 ns |   2,153.0 ns |   3.44 | 0.0420 |     704 B |        1.79 |
|               Execute |        .NET Core 3.1 |   1,855.0 ns |   1,858.4 ns |   3.04 | 0.0343 |     576 B |        1.47 |
|             RawAdoNet |        .NET Core 3.1 |     502.2 ns |     500.5 ns |   0.83 | 0.0191 |     328 B |        0.83 |
|                  Linq | .NET Framework 4.7.2 | 139,035.4 ns | 139,017.3 ns | 228.10 | 2.4414 |   16340 B |       41.58 |
|              Compiled | .NET Framework 4.7.2 |   8,830.6 ns |   8,767.8 ns |  14.52 | 0.4883 |    3161 B |        8.04 |
| FromSql_Interpolation | .NET Framework 4.7.2 |  34,947.2 ns |  35,009.3 ns |  57.08 | 1.1292 |    7189 B |       18.29 |
|   FromSql_Formattable | .NET Framework 4.7.2 |  36,588.2 ns |  36,626.9 ns |  59.96 | 1.1597 |    7511 B |       19.11 |
|                 Query | .NET Framework 4.7.2 |   2,637.1 ns |   2,653.1 ns |   4.34 | 0.1144 |     738 B |        1.88 |
|               Execute | .NET Framework 4.7.2 |   2,434.8 ns |   2,417.0 ns |   3.99 | 0.0954 |     610 B |        1.55 |
|             RawAdoNet | .NET Framework 4.7.2 |     609.6 ns |     609.6 ns |   1.00 | 0.0620 |     393 B |        1.00 |
