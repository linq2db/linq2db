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
|                                 Method |              Runtime |       Mean |     Median | Ratio | Allocated | Alloc Ratio |
|--------------------------------------- |--------------------- |-----------:|-----------:|------:|----------:|------------:|
|        TypeMapperReadOracleTimeStampTZ |             .NET 6.0 |  48.182 ns |  49.503 ns |  0.76 |         - |          NA |
|      DirectAccessReadOracleTimeStampTZ |             .NET 6.0 |  37.948 ns |  42.158 ns |  0.59 |         - |          NA |
|       TypeMapperReadOracleTimeStampLTZ |             .NET 6.0 |  43.740 ns |  46.715 ns |  0.68 |         - |          NA |
|     DirectAccessReadOracleTimeStampLTZ |             .NET 6.0 |  43.069 ns |  45.494 ns |  0.68 |         - |          NA |
|         TypeMapperReadOracleDecimalAdv |             .NET 6.0 |  16.020 ns |  17.049 ns |  0.25 |         - |          NA |
|       DirectAccessReadOracleDecimalAdv |             .NET 6.0 |   9.478 ns |  10.695 ns |  0.15 |         - |          NA |
|   TypeMapperReadOracleDecimalAsDecimal |             .NET 6.0 |   7.398 ns |   8.056 ns |  0.11 |         - |          NA |
| DirectAccessReadOracleDecimalAsDecimal |             .NET 6.0 |   3.733 ns |   3.946 ns |  0.06 |         - |          NA |
|       TypeMapperReadOracleDecimalAsInt |             .NET 6.0 |  11.566 ns |  13.002 ns |  0.18 |         - |          NA |
|     DirectAccessReadOracleDecimalAsInt |             .NET 6.0 |   6.958 ns |   7.513 ns |  0.11 |         - |          NA |
|      TypeMapperReadOracleDecimalAsLong |             .NET 6.0 |  11.735 ns |  12.282 ns |  0.19 |         - |          NA |
|    DirectAccessReadOracleDecimalAsLong |             .NET 6.0 |   7.464 ns |   8.012 ns |  0.12 |         - |          NA |
|        TypeMapperReadOracleTimeStampTZ |             .NET 7.0 |  48.750 ns |  52.488 ns |  0.76 |         - |          NA |
|      DirectAccessReadOracleTimeStampTZ |             .NET 7.0 |  45.410 ns |  48.405 ns |  0.71 |         - |          NA |
|       TypeMapperReadOracleTimeStampLTZ |             .NET 7.0 |  45.039 ns |  49.202 ns |  0.71 |         - |          NA |
|     DirectAccessReadOracleTimeStampLTZ |             .NET 7.0 |  48.042 ns |  52.134 ns |  0.76 |         - |          NA |
|         TypeMapperReadOracleDecimalAdv |             .NET 7.0 |  13.278 ns |  13.901 ns |  0.21 |         - |          NA |
|       DirectAccessReadOracleDecimalAdv |             .NET 7.0 |   4.756 ns |   4.993 ns |  0.07 |         - |          NA |
|   TypeMapperReadOracleDecimalAsDecimal |             .NET 7.0 |   6.791 ns |   7.180 ns |  0.11 |         - |          NA |
| DirectAccessReadOracleDecimalAsDecimal |             .NET 7.0 |   3.175 ns |   3.414 ns |  0.05 |         - |          NA |
|       TypeMapperReadOracleDecimalAsInt |             .NET 7.0 |   9.614 ns |   9.870 ns |  0.15 |         - |          NA |
|     DirectAccessReadOracleDecimalAsInt |             .NET 7.0 |   7.214 ns |   7.690 ns |  0.11 |         - |          NA |
|      TypeMapperReadOracleDecimalAsLong |             .NET 7.0 |  10.326 ns |  11.066 ns |  0.16 |         - |          NA |
|    DirectAccessReadOracleDecimalAsLong |             .NET 7.0 |   6.590 ns |   7.333 ns |  0.10 |         - |          NA |
|        TypeMapperReadOracleTimeStampTZ |        .NET Core 3.1 |  70.572 ns |  76.396 ns |  1.10 |         - |          NA |
|      DirectAccessReadOracleTimeStampTZ |        .NET Core 3.1 |  75.189 ns |  80.815 ns |  1.17 |         - |          NA |
|       TypeMapperReadOracleTimeStampLTZ |        .NET Core 3.1 |  76.517 ns |  80.406 ns |  1.20 |         - |          NA |
|     DirectAccessReadOracleTimeStampLTZ |        .NET Core 3.1 |  65.779 ns |  65.396 ns |  1.02 |         - |          NA |
|         TypeMapperReadOracleDecimalAdv |        .NET Core 3.1 |  14.500 ns |  15.425 ns |  0.23 |         - |          NA |
|       DirectAccessReadOracleDecimalAdv |        .NET Core 3.1 |   8.858 ns |   9.981 ns |  0.14 |         - |          NA |
|   TypeMapperReadOracleDecimalAsDecimal |        .NET Core 3.1 |   8.734 ns |   9.582 ns |  0.14 |         - |          NA |
| DirectAccessReadOracleDecimalAsDecimal |        .NET Core 3.1 |   2.882 ns |   3.352 ns |  0.05 |         - |          NA |
|       TypeMapperReadOracleDecimalAsInt |        .NET Core 3.1 |  11.124 ns |  11.777 ns |  0.17 |         - |          NA |
|     DirectAccessReadOracleDecimalAsInt |        .NET Core 3.1 |   8.570 ns |   8.968 ns |  0.13 |         - |          NA |
|      TypeMapperReadOracleDecimalAsLong |        .NET Core 3.1 |  13.018 ns |  13.897 ns |  0.20 |         - |          NA |
|    DirectAccessReadOracleDecimalAsLong |        .NET Core 3.1 |   7.793 ns |   8.322 ns |  0.12 |         - |          NA |
|        TypeMapperReadOracleTimeStampTZ | .NET Framework 4.7.2 | 137.625 ns | 140.803 ns |  2.14 |         - |          NA |
|      DirectAccessReadOracleTimeStampTZ | .NET Framework 4.7.2 |  67.631 ns |  72.445 ns |  1.00 |         - |          NA |
|       TypeMapperReadOracleTimeStampLTZ | .NET Framework 4.7.2 | 153.043 ns | 158.914 ns |  2.39 |         - |          NA |
|     DirectAccessReadOracleTimeStampLTZ | .NET Framework 4.7.2 |  67.125 ns |  70.069 ns |  1.06 |         - |          NA |
|         TypeMapperReadOracleDecimalAdv | .NET Framework 4.7.2 |  56.203 ns |  58.093 ns |  0.88 |         - |          NA |
|       DirectAccessReadOracleDecimalAdv | .NET Framework 4.7.2 |   7.578 ns |   8.008 ns |  0.12 |         - |          NA |
|   TypeMapperReadOracleDecimalAsDecimal | .NET Framework 4.7.2 |  37.664 ns |  39.619 ns |  0.60 |         - |          NA |
| DirectAccessReadOracleDecimalAsDecimal | .NET Framework 4.7.2 |   6.241 ns |   6.700 ns |  0.10 |         - |          NA |
|       TypeMapperReadOracleDecimalAsInt | .NET Framework 4.7.2 |  36.812 ns |  39.338 ns |  0.58 |         - |          NA |
|     DirectAccessReadOracleDecimalAsInt | .NET Framework 4.7.2 |  12.467 ns |  13.259 ns |  0.19 |         - |          NA |
|      TypeMapperReadOracleDecimalAsLong | .NET Framework 4.7.2 |  40.332 ns |  42.606 ns |  0.64 |         - |          NA |
|    DirectAccessReadOracleDecimalAsLong | .NET Framework 4.7.2 |  10.802 ns |  12.056 ns |  0.17 |         - |          NA |
