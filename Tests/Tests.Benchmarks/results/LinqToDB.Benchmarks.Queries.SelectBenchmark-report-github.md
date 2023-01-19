``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-UZBSVL : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-AYZXIO : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-NXXYQT : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-HMCTKM : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                Method |              Runtime |        Mean |      Median |  Ratio |   Gen0 | Allocated | Alloc Ratio |
|---------------------- |--------------------- |------------:|------------:|-------:|-------:|----------:|------------:|
|                  Linq |             .NET 6.0 | 49,558.0 ns | 49,797.0 ns |  77.91 | 0.7935 |   13984 B |       35.58 |
|              Compiled |             .NET 6.0 |  4,973.0 ns |  5,084.0 ns |   7.55 | 0.1831 |    3088 B |        7.86 |
| FromSql_Interpolation |             .NET 6.0 | 17,012.3 ns | 16,999.9 ns |  26.86 | 0.3967 |    6864 B |       17.47 |
|   FromSql_Formattable |             .NET 6.0 | 18,202.0 ns | 18,206.4 ns |  28.64 | 0.4272 |    7168 B |       18.24 |
|                 Query |             .NET 6.0 |  1,550.5 ns |  1,544.1 ns |   2.45 | 0.0420 |     704 B |        1.79 |
|               Execute |             .NET 6.0 |  1,254.7 ns |  1,514.7 ns |   2.19 | 0.0343 |     576 B |        1.47 |
|             RawAdoNet |             .NET 6.0 |    216.1 ns |    216.4 ns |   0.34 | 0.0181 |     304 B |        0.77 |
|                  Linq |             .NET 7.0 | 30,016.4 ns | 30,023.8 ns |  47.14 | 0.4883 |    8944 B |       22.76 |
|              Compiled |             .NET 7.0 |  5,190.9 ns |  5,056.1 ns |   8.21 | 0.1831 |    3088 B |        7.86 |
| FromSql_Interpolation |             .NET 7.0 |  9,554.6 ns |  9,469.9 ns |  15.02 | 0.3052 |    5168 B |       13.15 |
|   FromSql_Formattable |             .NET 7.0 | 11,481.5 ns | 11,419.0 ns |  18.05 | 0.3204 |    5472 B |       13.92 |
|                 Query |             .NET 7.0 |  1,403.3 ns |  1,409.8 ns |   2.21 | 0.0420 |     704 B |        1.79 |
|               Execute |             .NET 7.0 |  1,384.8 ns |  1,372.0 ns |   2.18 | 0.0343 |     576 B |        1.47 |
|             RawAdoNet |             .NET 7.0 |    211.7 ns |    212.3 ns |   0.33 | 0.0181 |     304 B |        0.77 |
|                  Linq |        .NET Core 3.1 | 60,702.0 ns | 60,732.3 ns |  95.43 | 0.7324 |   13216 B |       33.63 |
|              Compiled |        .NET Core 3.1 |  6,772.7 ns |  6,769.9 ns |  10.69 | 0.1755 |    3056 B |        7.78 |
| FromSql_Interpolation |        .NET Core 3.1 | 20,532.1 ns | 20,695.8 ns |  32.28 | 0.3967 |    6816 B |       17.34 |
|   FromSql_Formattable |        .NET Core 3.1 | 21,192.5 ns | 21,331.5 ns |  33.32 | 0.3967 |    7120 B |       18.12 |
|                 Query |        .NET Core 3.1 |  2,231.5 ns |  2,226.6 ns |   3.50 | 0.0420 |     704 B |        1.79 |
|               Execute |        .NET Core 3.1 |  1,927.5 ns |  1,949.4 ns |   3.03 | 0.0343 |     576 B |        1.47 |
|             RawAdoNet |        .NET Core 3.1 |    506.3 ns |    510.6 ns |   0.80 | 0.0191 |     328 B |        0.83 |
|                  Linq | .NET Framework 4.7.2 | 85,631.8 ns | 95,475.9 ns | 122.71 | 2.3193 |   14991 B |       38.15 |
|              Compiled | .NET Framework 4.7.2 |  8,577.4 ns |  8,630.7 ns |  13.12 | 0.4883 |    3161 B |        8.04 |
| FromSql_Interpolation | .NET Framework 4.7.2 | 27,496.2 ns | 27,508.8 ns |  43.23 | 1.0376 |    6628 B |       16.87 |
|   FromSql_Formattable | .NET Framework 4.7.2 | 20,876.3 ns | 22,682.3 ns |  31.65 | 1.0986 |    6949 B |       17.68 |
|                 Query | .NET Framework 4.7.2 |  2,665.3 ns |  2,676.3 ns |   4.19 | 0.1163 |     738 B |        1.88 |
|               Execute | .NET Framework 4.7.2 |  2,558.9 ns |  2,553.0 ns |   4.03 | 0.0954 |     610 B |        1.55 |
|             RawAdoNet | .NET Framework 4.7.2 |    636.2 ns |    635.5 ns |   1.00 | 0.0625 |     393 B |        1.00 |
