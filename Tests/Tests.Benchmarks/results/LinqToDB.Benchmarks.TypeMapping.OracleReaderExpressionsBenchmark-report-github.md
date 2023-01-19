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
|                                 Method |              Runtime |       Mean |     Median | Ratio | Allocated | Alloc Ratio |
|--------------------------------------- |--------------------- |-----------:|-----------:|------:|----------:|------------:|
|        TypeMapperReadOracleTimeStampTZ |             .NET 6.0 |  39.453 ns |  43.307 ns |  0.61 |         - |          NA |
|      DirectAccessReadOracleTimeStampTZ |             .NET 6.0 |  43.310 ns |  43.754 ns |  0.64 |         - |          NA |
|       TypeMapperReadOracleTimeStampLTZ |             .NET 6.0 |  44.299 ns |  44.298 ns |  0.66 |         - |          NA |
|     DirectAccessReadOracleTimeStampLTZ |             .NET 6.0 |  34.812 ns |  42.690 ns |  0.60 |         - |          NA |
|         TypeMapperReadOracleDecimalAdv |             .NET 6.0 |   9.873 ns |  13.314 ns |  0.15 |         - |          NA |
|       DirectAccessReadOracleDecimalAdv |             .NET 6.0 |   9.618 ns |   9.618 ns |  0.14 |         - |          NA |
|   TypeMapperReadOracleDecimalAsDecimal |             .NET 6.0 |   6.879 ns |   6.879 ns |  0.10 |         - |          NA |
| DirectAccessReadOracleDecimalAsDecimal |             .NET 6.0 |   2.562 ns |   3.225 ns |  0.03 |         - |          NA |
|       TypeMapperReadOracleDecimalAsInt |             .NET 6.0 |   9.991 ns |  10.957 ns |  0.14 |         - |          NA |
|     DirectAccessReadOracleDecimalAsInt |             .NET 6.0 |   8.414 ns |   8.477 ns |  0.12 |         - |          NA |
|      TypeMapperReadOracleDecimalAsLong |             .NET 6.0 |  10.378 ns |  10.315 ns |  0.15 |         - |          NA |
|    DirectAccessReadOracleDecimalAsLong |             .NET 6.0 |   7.407 ns |   7.756 ns |  0.11 |         - |          NA |
|        TypeMapperReadOracleTimeStampTZ |             .NET 7.0 |  45.271 ns |  46.833 ns |  0.66 |         - |          NA |
|      DirectAccessReadOracleTimeStampTZ |             .NET 7.0 |  45.604 ns |  45.325 ns |  0.68 |         - |          NA |
|       TypeMapperReadOracleTimeStampLTZ |             .NET 7.0 |  45.922 ns |  45.919 ns |  0.68 |         - |          NA |
|     DirectAccessReadOracleTimeStampLTZ |             .NET 7.0 |  43.321 ns |  45.532 ns |  0.51 |         - |          NA |
|         TypeMapperReadOracleDecimalAdv |             .NET 7.0 |  13.403 ns |  13.271 ns |  0.20 |         - |          NA |
|       DirectAccessReadOracleDecimalAdv |             .NET 7.0 |   4.784 ns |   5.449 ns |  0.05 |         - |          NA |
|   TypeMapperReadOracleDecimalAsDecimal |             .NET 7.0 |   6.081 ns |   6.051 ns |  0.09 |         - |          NA |
| DirectAccessReadOracleDecimalAsDecimal |             .NET 7.0 |   3.224 ns |   3.224 ns |  0.05 |         - |          NA |
|       TypeMapperReadOracleDecimalAsInt |             .NET 7.0 |   8.020 ns |  10.623 ns |  0.10 |         - |          NA |
|     DirectAccessReadOracleDecimalAsInt |             .NET 7.0 |   7.914 ns |   7.921 ns |  0.12 |         - |          NA |
|      TypeMapperReadOracleDecimalAsLong |             .NET 7.0 |   9.616 ns |   9.615 ns |  0.14 |         - |          NA |
|    DirectAccessReadOracleDecimalAsLong |             .NET 7.0 |   6.398 ns |   6.398 ns |  0.09 |         - |          NA |
|        TypeMapperReadOracleTimeStampTZ |        .NET Core 3.1 |  69.166 ns |  69.164 ns |  1.02 |         - |          NA |
|      DirectAccessReadOracleTimeStampTZ |        .NET Core 3.1 |  68.103 ns |  68.101 ns |  1.01 |         - |          NA |
|       TypeMapperReadOracleTimeStampLTZ |        .NET Core 3.1 |  72.180 ns |  72.182 ns |  1.07 |         - |          NA |
|     DirectAccessReadOracleTimeStampLTZ |        .NET Core 3.1 |  29.275 ns |  29.117 ns |  0.43 |         - |          NA |
|         TypeMapperReadOracleDecimalAdv |        .NET Core 3.1 |  13.567 ns |  13.564 ns |  0.20 |         - |          NA |
|       DirectAccessReadOracleDecimalAdv |        .NET Core 3.1 |  10.748 ns |  10.789 ns |  0.16 |         - |          NA |
|   TypeMapperReadOracleDecimalAsDecimal |        .NET Core 3.1 |   7.457 ns |   7.478 ns |  0.11 |         - |          NA |
| DirectAccessReadOracleDecimalAsDecimal |        .NET Core 3.1 |   3.585 ns |   4.600 ns |  0.07 |         - |          NA |
|       TypeMapperReadOracleDecimalAsInt |        .NET Core 3.1 |  10.958 ns |  10.958 ns |  0.16 |         - |          NA |
|     DirectAccessReadOracleDecimalAsInt |        .NET Core 3.1 |   7.557 ns |   7.494 ns |  0.12 |         - |          NA |
|      TypeMapperReadOracleDecimalAsLong |        .NET Core 3.1 |  11.438 ns |  11.423 ns |  0.17 |         - |          NA |
|    DirectAccessReadOracleDecimalAsLong |        .NET Core 3.1 |   8.624 ns |   8.621 ns |  0.13 |         - |          NA |
|        TypeMapperReadOracleTimeStampTZ | .NET Framework 4.7.2 | 134.997 ns | 134.995 ns |  2.00 |         - |          NA |
|      DirectAccessReadOracleTimeStampTZ | .NET Framework 4.7.2 |  67.511 ns |  67.434 ns |  1.00 |         - |          NA |
|       TypeMapperReadOracleTimeStampLTZ | .NET Framework 4.7.2 | 133.598 ns | 133.329 ns |  1.99 |         - |          NA |
|     DirectAccessReadOracleTimeStampLTZ | .NET Framework 4.7.2 |  66.457 ns |  67.099 ns |  0.98 |         - |          NA |
|         TypeMapperReadOracleDecimalAdv | .NET Framework 4.7.2 |  52.089 ns |  52.225 ns |  0.77 |         - |          NA |
|       DirectAccessReadOracleDecimalAdv | .NET Framework 4.7.2 |   6.365 ns |   7.515 ns |  0.09 |         - |          NA |
|   TypeMapperReadOracleDecimalAsDecimal | .NET Framework 4.7.2 |  38.443 ns |  41.568 ns |  0.59 |         - |          NA |
| DirectAccessReadOracleDecimalAsDecimal | .NET Framework 4.7.2 |   5.965 ns |   5.965 ns |  0.09 |         - |          NA |
|       TypeMapperReadOracleDecimalAsInt | .NET Framework 4.7.2 |  36.526 ns |  36.527 ns |  0.54 |         - |          NA |
|     DirectAccessReadOracleDecimalAsInt | .NET Framework 4.7.2 |   9.667 ns |  11.080 ns |  0.12 |         - |          NA |
|      TypeMapperReadOracleDecimalAsLong | .NET Framework 4.7.2 |  36.595 ns |  36.595 ns |  0.54 |         - |          NA |
|    DirectAccessReadOracleDecimalAsLong | .NET Framework 4.7.2 |  10.530 ns |  10.530 ns |  0.16 |         - |          NA |
