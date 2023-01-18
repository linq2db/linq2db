``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-XCPGVR : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-RHOQGE : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-WEVYVV : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-ORXRGX : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                Method |              Runtime |         Mean |       Median |  Ratio |   Gen0 | Allocated | Alloc Ratio |
|---------------------- |--------------------- |-------------:|-------------:|-------:|-------:|----------:|------------:|
|                  Linq |             .NET 6.0 |  61,213.9 ns |  61,308.5 ns |  91.56 | 0.7935 |   13632 B |       34.69 |
|              Compiled |             .NET 6.0 |   4,940.6 ns |   4,940.1 ns |   7.40 | 0.1831 |    3088 B |        7.86 |
| FromSql_Interpolation |             .NET 6.0 |  19,320.7 ns |  19,191.0 ns |  28.90 | 0.3967 |    6720 B |       17.10 |
|   FromSql_Formattable |             .NET 6.0 |  16,254.9 ns |  20,014.3 ns |  28.65 | 0.4120 |    7024 B |       17.87 |
|                 Query |             .NET 6.0 |   1,553.9 ns |   1,554.5 ns |   2.32 | 0.0420 |     704 B |        1.79 |
|               Execute |             .NET 6.0 |   1,359.8 ns |   1,335.6 ns |   2.01 | 0.0343 |     576 B |        1.47 |
|             RawAdoNet |             .NET 6.0 |     220.4 ns |     220.8 ns |   0.33 | 0.0181 |     304 B |        0.77 |
|                  Linq |             .NET 7.0 |  41,658.5 ns |  41,912.5 ns |  62.31 | 0.5493 |    9984 B |       25.40 |
|              Compiled |             .NET 7.0 |   5,482.0 ns |   5,439.3 ns |   8.20 | 0.1831 |    3088 B |        7.86 |
| FromSql_Interpolation |             .NET 7.0 |  13,336.1 ns |  13,330.3 ns |  19.97 | 0.3204 |    5520 B |       14.05 |
|   FromSql_Formattable |             .NET 7.0 |  14,938.3 ns |  14,905.7 ns |  22.34 | 0.3433 |    5824 B |       14.82 |
|                 Query |             .NET 7.0 |   1,402.3 ns |   1,402.2 ns |   2.10 | 0.0420 |     704 B |        1.79 |
|               Execute |             .NET 7.0 |   1,112.6 ns |   1,307.5 ns |   1.41 | 0.0343 |     576 B |        1.47 |
|             RawAdoNet |             .NET 7.0 |     210.5 ns |     210.7 ns |   0.31 | 0.0181 |     304 B |        0.77 |
|                  Linq |        .NET Core 3.1 |  72,877.8 ns |  73,495.8 ns | 108.52 | 0.7324 |   13168 B |       33.51 |
|              Compiled |        .NET Core 3.1 |   7,170.3 ns |   7,192.4 ns |  10.72 | 0.1755 |    3056 B |        7.78 |
| FromSql_Interpolation |        .NET Core 3.1 |  23,532.9 ns |  23,522.2 ns |  35.20 | 0.3967 |    6672 B |       16.98 |
|   FromSql_Formattable |        .NET Core 3.1 |  24,626.6 ns |  24,626.3 ns |  36.88 | 0.3967 |    6976 B |       17.75 |
|                 Query |        .NET Core 3.1 |   2,152.5 ns |   2,154.5 ns |   3.23 | 0.0420 |     704 B |        1.79 |
|               Execute |        .NET Core 3.1 |   2,129.9 ns |   2,145.5 ns |   3.19 | 0.0343 |     576 B |        1.47 |
|             RawAdoNet |        .NET Core 3.1 |     498.9 ns |     499.7 ns |   0.75 | 0.0191 |     328 B |        0.83 |
|                  Linq | .NET Framework 4.7.2 | 123,425.0 ns | 124,747.2 ns | 190.92 | 2.4414 |   16340 B |       41.58 |
|              Compiled | .NET Framework 4.7.2 |   8,332.3 ns |   8,230.5 ns |  12.42 | 0.4883 |    3161 B |        8.04 |
| FromSql_Interpolation | .NET Framework 4.7.2 |  34,484.4 ns |  34,485.3 ns |  51.64 | 1.0986 |    7190 B |       18.30 |
|   FromSql_Formattable | .NET Framework 4.7.2 |  36,108.5 ns |  36,649.0 ns |  50.30 | 1.1902 |    7510 B |       19.11 |
|                 Query | .NET Framework 4.7.2 |   2,808.4 ns |   2,825.9 ns |   4.20 | 0.1144 |     738 B |        1.88 |
|               Execute | .NET Framework 4.7.2 |   2,242.2 ns |   2,237.7 ns |   3.32 | 0.0954 |     610 B |        1.55 |
|             RawAdoNet | .NET Framework 4.7.2 |     668.7 ns |     662.0 ns |   1.00 | 0.0620 |     393 B |        1.00 |
