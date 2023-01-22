``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-TEPEZT : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-ISYUTK : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-SMHCKK : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-DHDWVI : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                Method |              Runtime |        Mean |      Median |  Ratio |   Gen0 | Allocated | Alloc Ratio |
|---------------------- |--------------------- |------------:|------------:|-------:|-------:|----------:|------------:|
|                  Linq |             .NET 6.0 | 41,371.6 ns | 48,426.3 ns |  70.29 | 0.7324 |   12640 B |       32.16 |
|              Compiled |             .NET 6.0 |  4,636.4 ns |  4,553.4 ns |   7.42 | 0.1831 |    3088 B |        7.86 |
| FromSql_Interpolation |             .NET 6.0 | 15,641.5 ns | 15,633.8 ns |  24.69 | 0.3967 |    6736 B |       17.14 |
|   FromSql_Formattable |             .NET 6.0 | 18,103.6 ns | 18,112.7 ns |  28.60 | 0.3967 |    7040 B |       17.91 |
|                 Query |             .NET 6.0 |  1,572.2 ns |  1,584.4 ns |   2.48 | 0.0420 |     704 B |        1.79 |
|               Execute |             .NET 6.0 |  1,188.3 ns |  1,427.7 ns |   1.92 | 0.0343 |     576 B |        1.47 |
|             RawAdoNet |             .NET 6.0 |    232.1 ns |    233.1 ns |   0.37 | 0.0181 |     304 B |        0.77 |
|                  Linq |             .NET 7.0 | 31,846.0 ns | 31,846.3 ns |  50.21 | 0.4883 |    8304 B |       21.13 |
|              Compiled |             .NET 7.0 |  5,694.9 ns |  5,672.6 ns |   8.98 | 0.1831 |    3088 B |        7.86 |
| FromSql_Interpolation |             .NET 7.0 | 10,315.2 ns | 10,352.3 ns |  16.28 | 0.2899 |    5040 B |       12.82 |
|   FromSql_Formattable |             .NET 7.0 | 11,889.8 ns | 11,876.9 ns |  18.77 | 0.3052 |    5344 B |       13.60 |
|                 Query |             .NET 7.0 |  1,611.1 ns |  1,616.7 ns |   2.54 | 0.0420 |     704 B |        1.79 |
|               Execute |             .NET 7.0 |  1,302.5 ns |  1,308.6 ns |   2.05 | 0.0343 |     576 B |        1.47 |
|             RawAdoNet |             .NET 7.0 |    174.5 ns |    203.8 ns |   0.24 | 0.0181 |     304 B |        0.77 |
|                  Linq |        .NET Core 3.1 | 65,024.9 ns | 64,838.3 ns | 102.45 | 0.7324 |   12928 B |       32.90 |
|              Compiled |        .NET Core 3.1 |  7,425.3 ns |  7,442.5 ns |  11.71 | 0.1755 |    3056 B |        7.78 |
| FromSql_Interpolation |        .NET Core 3.1 | 20,323.9 ns | 20,446.9 ns |  32.05 | 0.3967 |    6688 B |       17.02 |
|   FromSql_Formattable |        .NET Core 3.1 | 20,342.3 ns | 19,959.1 ns |  31.50 | 0.3967 |    6992 B |       17.79 |
|                 Query |        .NET Core 3.1 |  2,190.9 ns |  2,186.0 ns |   3.45 | 0.0420 |     704 B |        1.79 |
|               Execute |        .NET Core 3.1 |  1,602.7 ns |  1,906.8 ns |   2.04 | 0.0343 |     576 B |        1.47 |
|             RawAdoNet |        .NET Core 3.1 |    504.5 ns |    505.7 ns |   0.80 | 0.0191 |     328 B |        0.83 |
|                  Linq | .NET Framework 4.7.2 | 89,473.8 ns | 92,925.4 ns | 128.11 | 2.1973 |   13963 B |       35.53 |
|              Compiled | .NET Framework 4.7.2 |  7,658.6 ns |  9,262.9 ns |  13.51 | 0.4883 |    3161 B |        8.04 |
| FromSql_Interpolation | .NET Framework 4.7.2 | 25,380.9 ns | 25,424.0 ns |  40.02 | 1.0071 |    6499 B |       16.54 |
|   FromSql_Formattable | .NET Framework 4.7.2 | 26,374.4 ns | 27,689.9 ns |  39.87 | 1.0681 |    6820 B |       17.35 |
|                 Query | .NET Framework 4.7.2 |  2,382.4 ns |  2,457.4 ns |   4.02 | 0.1144 |     738 B |        1.88 |
|               Execute | .NET Framework 4.7.2 |  2,461.0 ns |  2,482.4 ns |   3.86 | 0.0954 |     610 B |        1.55 |
|             RawAdoNet | .NET Framework 4.7.2 |    634.1 ns |    635.6 ns |   1.00 | 0.0620 |     393 B |        1.00 |
