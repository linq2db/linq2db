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
|                                 Method |              Runtime |       Mean |     Median | Ratio | Allocated | Alloc Ratio |
|--------------------------------------- |--------------------- |-----------:|-----------:|------:|----------:|------------:|
|        TypeMapperReadOracleTimeStampTZ |             .NET 6.0 |  47.160 ns |  47.466 ns |  0.71 |         - |          NA |
|      DirectAccessReadOracleTimeStampTZ |             .NET 6.0 |  43.177 ns |  43.474 ns |  0.65 |         - |          NA |
|       TypeMapperReadOracleTimeStampLTZ |             .NET 6.0 |  18.745 ns |  18.749 ns |  0.28 |         - |          NA |
|     DirectAccessReadOracleTimeStampLTZ |             .NET 6.0 |  33.950 ns |  43.204 ns |  0.65 |         - |          NA |
|         TypeMapperReadOracleDecimalAdv |             .NET 6.0 |  13.375 ns |  13.375 ns |  0.20 |         - |          NA |
|       DirectAccessReadOracleDecimalAdv |             .NET 6.0 |   9.636 ns |   9.635 ns |  0.15 |         - |          NA |
|   TypeMapperReadOracleDecimalAsDecimal |             .NET 6.0 |   7.350 ns |   7.349 ns |  0.11 |         - |          NA |
| DirectAccessReadOracleDecimalAsDecimal |             .NET 6.0 |   3.232 ns |   3.232 ns |  0.05 |         - |          NA |
|       TypeMapperReadOracleDecimalAsInt |             .NET 6.0 |  10.984 ns |  10.981 ns |  0.16 |         - |          NA |
|     DirectAccessReadOracleDecimalAsInt |             .NET 6.0 |   7.967 ns |   8.035 ns |  0.12 |         - |          NA |
|      TypeMapperReadOracleDecimalAsLong |             .NET 6.0 |   5.309 ns |   5.290 ns |  0.08 |         - |          NA |
|    DirectAccessReadOracleDecimalAsLong |             .NET 6.0 |   8.012 ns |   8.067 ns |  0.12 |         - |          NA |
|        TypeMapperReadOracleTimeStampTZ |             .NET 7.0 |  47.265 ns |  46.897 ns |  0.71 |         - |          NA |
|      DirectAccessReadOracleTimeStampTZ |             .NET 7.0 |  42.176 ns |  41.195 ns |  0.64 |         - |          NA |
|       TypeMapperReadOracleTimeStampLTZ |             .NET 7.0 |  49.995 ns |  50.092 ns |  0.75 |         - |          NA |
|     DirectAccessReadOracleTimeStampLTZ |             .NET 7.0 |  46.013 ns |  45.844 ns |  0.69 |         - |          NA |
|         TypeMapperReadOracleDecimalAdv |             .NET 7.0 |  10.621 ns |  13.345 ns |  0.18 |         - |          NA |
|       DirectAccessReadOracleDecimalAdv |             .NET 7.0 |   4.177 ns |   4.149 ns |  0.06 |         - |          NA |
|   TypeMapperReadOracleDecimalAsDecimal |             .NET 7.0 |   2.749 ns |   2.746 ns |  0.04 |         - |          NA |
| DirectAccessReadOracleDecimalAsDecimal |             .NET 7.0 |   3.226 ns |   3.226 ns |  0.05 |         - |          NA |
|       TypeMapperReadOracleDecimalAsInt |             .NET 7.0 |   9.834 ns |   9.898 ns |  0.15 |         - |          NA |
|     DirectAccessReadOracleDecimalAsInt |             .NET 7.0 |   7.942 ns |   8.012 ns |  0.12 |         - |          NA |
|      TypeMapperReadOracleDecimalAsLong |             .NET 7.0 |   9.807 ns |   9.872 ns |  0.15 |         - |          NA |
|    DirectAccessReadOracleDecimalAsLong |             .NET 7.0 |   6.568 ns |   6.613 ns |  0.10 |         - |          NA |
|        TypeMapperReadOracleTimeStampTZ |        .NET Core 3.1 |  62.083 ns |  70.738 ns |  0.83 |         - |          NA |
|      DirectAccessReadOracleTimeStampTZ |        .NET Core 3.1 |  67.553 ns |  68.347 ns |  1.02 |         - |          NA |
|       TypeMapperReadOracleTimeStampLTZ |        .NET Core 3.1 |  73.300 ns |  73.581 ns |  1.11 |         - |          NA |
|     DirectAccessReadOracleTimeStampLTZ |        .NET Core 3.1 |  70.051 ns |  70.458 ns |  1.06 |         - |          NA |
|         TypeMapperReadOracleDecimalAdv |        .NET Core 3.1 |  13.636 ns |  13.759 ns |  0.21 |         - |          NA |
|       DirectAccessReadOracleDecimalAdv |        .NET Core 3.1 |   9.847 ns |   9.922 ns |  0.15 |         - |          NA |
|   TypeMapperReadOracleDecimalAsDecimal |        .NET Core 3.1 |   7.532 ns |   7.505 ns |  0.11 |         - |          NA |
| DirectAccessReadOracleDecimalAsDecimal |        .NET Core 3.1 |   3.316 ns |   3.358 ns |  0.05 |         - |          NA |
|       TypeMapperReadOracleDecimalAsInt |        .NET Core 3.1 |  11.672 ns |  11.703 ns |  0.18 |         - |          NA |
|     DirectAccessReadOracleDecimalAsInt |        .NET Core 3.1 |   5.290 ns |   3.651 ns |  0.07 |         - |          NA |
|      TypeMapperReadOracleDecimalAsLong |        .NET Core 3.1 |  11.726 ns |  11.744 ns |  0.18 |         - |          NA |
|    DirectAccessReadOracleDecimalAsLong |        .NET Core 3.1 |   8.049 ns |   7.673 ns |  0.12 |         - |          NA |
|        TypeMapperReadOracleTimeStampTZ | .NET Framework 4.7.2 | 125.231 ns | 125.709 ns |  1.92 |         - |          NA |
|      DirectAccessReadOracleTimeStampTZ | .NET Framework 4.7.2 |  66.333 ns |  65.529 ns |  1.00 |         - |          NA |
|       TypeMapperReadOracleTimeStampLTZ | .NET Framework 4.7.2 | 146.293 ns | 147.402 ns |  2.21 |         - |          NA |
|     DirectAccessReadOracleTimeStampLTZ | .NET Framework 4.7.2 |  67.019 ns |  66.593 ns |  1.01 |         - |          NA |
|         TypeMapperReadOracleDecimalAdv | .NET Framework 4.7.2 |  48.832 ns |  51.478 ns |  0.48 |         - |          NA |
|       DirectAccessReadOracleDecimalAdv | .NET Framework 4.7.2 |   5.710 ns |   7.377 ns |  0.06 |         - |          NA |
|   TypeMapperReadOracleDecimalAsDecimal | .NET Framework 4.7.2 |  34.702 ns |  34.865 ns |  0.52 |         - |          NA |
| DirectAccessReadOracleDecimalAsDecimal | .NET Framework 4.7.2 |   5.977 ns |   5.977 ns |  0.09 |         - |          NA |
|       TypeMapperReadOracleDecimalAsInt | .NET Framework 4.7.2 |  36.094 ns |  36.091 ns |  0.54 |         - |          NA |
|     DirectAccessReadOracleDecimalAsInt | .NET Framework 4.7.2 |  10.123 ns |  10.123 ns |  0.15 |         - |          NA |
|      TypeMapperReadOracleDecimalAsLong | .NET Framework 4.7.2 |  36.574 ns |  36.572 ns |  0.55 |         - |          NA |
|    DirectAccessReadOracleDecimalAsLong | .NET Framework 4.7.2 |   8.820 ns |  10.523 ns |  0.15 |         - |          NA |
