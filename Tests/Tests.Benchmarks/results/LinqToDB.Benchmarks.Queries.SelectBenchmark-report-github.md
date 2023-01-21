``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-RNZPMW : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-XCCWXF : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-WSMVMG : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-FMTKFQ : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                Method |              Runtime |        Mean |      Median |  Ratio |   Gen0 | Allocated | Alloc Ratio |
|---------------------- |--------------------- |------------:|------------:|-------:|-------:|----------:|------------:|
|                  Linq |             .NET 6.0 | 69,041.6 ns | 71,558.0 ns | 108.86 | 0.7324 |   13344 B |       33.95 |
|              Compiled |             .NET 6.0 |  6,797.0 ns |  6,965.8 ns |  10.79 | 0.1831 |    3088 B |        7.86 |
| FromSql_Interpolation |             .NET 6.0 | 21,608.5 ns | 22,319.1 ns |  34.26 | 0.3967 |    6736 B |       17.14 |
|   FromSql_Formattable |             .NET 6.0 | 26,840.2 ns | 28,249.9 ns |  42.52 | 0.3967 |    7040 B |       17.91 |
|                 Query |             .NET 6.0 |  2,065.5 ns |  2,259.5 ns |   3.26 | 0.0420 |     704 B |        1.79 |
|               Execute |             .NET 6.0 |  2,103.6 ns |  2,288.6 ns |   3.33 | 0.0343 |     576 B |        1.47 |
|             RawAdoNet |             .NET 6.0 |    365.8 ns |    392.8 ns |   0.58 | 0.0181 |     304 B |        0.77 |
|                  Linq |             .NET 7.0 | 44,747.2 ns | 46,236.2 ns |  70.63 | 0.4883 |    8304 B |       21.13 |
|              Compiled |             .NET 7.0 |  6,676.9 ns |  6,722.1 ns |  10.51 | 0.1831 |    3088 B |        7.86 |
| FromSql_Interpolation |             .NET 7.0 | 12,640.1 ns | 13,319.5 ns |  19.95 | 0.2899 |    5040 B |       12.82 |
|   FromSql_Formattable |             .NET 7.0 | 14,529.3 ns | 15,001.1 ns |  23.01 | 0.3052 |    5344 B |       13.60 |
|                 Query |             .NET 7.0 |  1,728.0 ns |  1,813.8 ns |   2.72 | 0.0420 |     704 B |        1.79 |
|               Execute |             .NET 7.0 |  1,621.9 ns |  1,694.6 ns |   2.58 | 0.0343 |     576 B |        1.47 |
|             RawAdoNet |             .NET 7.0 |    295.7 ns |    304.6 ns |   0.47 | 0.0181 |     304 B |        0.77 |
|                  Linq |        .NET Core 3.1 | 79,661.5 ns | 82,340.7 ns | 125.91 | 0.7324 |   12928 B |       32.90 |
|              Compiled |        .NET Core 3.1 | 10,090.6 ns | 10,935.3 ns |  15.79 | 0.1678 |    3056 B |        7.78 |
| FromSql_Interpolation |        .NET Core 3.1 | 28,536.2 ns | 29,354.0 ns |  45.22 | 0.3967 |    6688 B |       17.02 |
|   FromSql_Formattable |        .NET Core 3.1 | 27,449.6 ns | 30,217.5 ns |  43.59 | 0.3967 |    6992 B |       17.79 |
|                 Query |        .NET Core 3.1 |  2,418.5 ns |  2,489.6 ns |   3.85 | 0.0420 |     704 B |        1.79 |
|               Execute |        .NET Core 3.1 |  2,300.8 ns |  2,421.6 ns |   3.65 | 0.0343 |     576 B |        1.47 |
|             RawAdoNet |        .NET Core 3.1 |    586.8 ns |    594.5 ns |   0.93 | 0.0191 |     328 B |        0.83 |
|                  Linq | .NET Framework 4.7.2 | 91,990.2 ns | 94,471.5 ns | 145.92 | 2.1973 |   13963 B |       35.53 |
|              Compiled | .NET Framework 4.7.2 |  9,164.3 ns |  9,325.8 ns |  14.50 | 0.4883 |    3161 B |        8.04 |
| FromSql_Interpolation | .NET Framework 4.7.2 | 27,135.0 ns | 28,769.1 ns |  42.67 | 1.0071 |    6499 B |       16.54 |
|   FromSql_Formattable | .NET Framework 4.7.2 | 29,981.1 ns | 30,930.5 ns |  47.49 | 1.0376 |    6821 B |       17.36 |
|                 Query | .NET Framework 4.7.2 |  2,918.6 ns |  3,117.0 ns |   4.61 | 0.1163 |     738 B |        1.88 |
|               Execute | .NET Framework 4.7.2 |  2,760.8 ns |  2,847.7 ns |   4.37 | 0.0954 |     610 B |        1.55 |
|             RawAdoNet | .NET Framework 4.7.2 |    660.9 ns |    655.3 ns |   1.00 | 0.0620 |     393 B |        1.00 |
