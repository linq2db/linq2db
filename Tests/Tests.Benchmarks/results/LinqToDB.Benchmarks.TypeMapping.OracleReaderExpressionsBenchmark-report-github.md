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
|                                 Method |              Runtime |       Mean |     Median | Ratio | Allocated | Alloc Ratio |
|--------------------------------------- |--------------------- |-----------:|-----------:|------:|----------:|------------:|
|        TypeMapperReadOracleTimeStampTZ |             .NET 6.0 |  43.487 ns |  43.908 ns |  0.64 |         - |          NA |
|      DirectAccessReadOracleTimeStampTZ |             .NET 6.0 |  43.243 ns |  43.324 ns |  0.64 |         - |          NA |
|       TypeMapperReadOracleTimeStampLTZ |             .NET 6.0 |  45.923 ns |  45.975 ns |  0.68 |         - |          NA |
|     DirectAccessReadOracleTimeStampLTZ |             .NET 6.0 |  45.883 ns |  46.185 ns |  0.68 |         - |          NA |
|         TypeMapperReadOracleDecimalAdv |             .NET 6.0 |  13.751 ns |  13.856 ns |  0.20 |         - |          NA |
|       DirectAccessReadOracleDecimalAdv |             .NET 6.0 |   9.848 ns |   9.822 ns |  0.15 |         - |          NA |
|   TypeMapperReadOracleDecimalAsDecimal |             .NET 6.0 |   7.516 ns |   7.566 ns |  0.11 |         - |          NA |
| DirectAccessReadOracleDecimalAsDecimal |             .NET 6.0 |   3.905 ns |   4.275 ns |  0.04 |         - |          NA |
|       TypeMapperReadOracleDecimalAsInt |             .NET 6.0 |  11.204 ns |  11.225 ns |  0.17 |         - |          NA |
|     DirectAccessReadOracleDecimalAsInt |             .NET 6.0 |   8.032 ns |   8.036 ns |  0.12 |         - |          NA |
|      TypeMapperReadOracleDecimalAsLong |             .NET 6.0 |  10.411 ns |  11.702 ns |  0.14 |         - |          NA |
|    DirectAccessReadOracleDecimalAsLong |             .NET 6.0 |   8.525 ns |   8.527 ns |  0.13 |         - |          NA |
|        TypeMapperReadOracleTimeStampTZ |             .NET 7.0 |  48.012 ns |  48.028 ns |  0.71 |         - |          NA |
|      DirectAccessReadOracleTimeStampTZ |             .NET 7.0 |  47.719 ns |  47.814 ns |  0.71 |         - |          NA |
|       TypeMapperReadOracleTimeStampLTZ |             .NET 7.0 |  46.462 ns |  46.927 ns |  0.69 |         - |          NA |
|     DirectAccessReadOracleTimeStampLTZ |             .NET 7.0 |  46.481 ns |  46.640 ns |  0.69 |         - |          NA |
|         TypeMapperReadOracleDecimalAdv |             .NET 7.0 |  13.121 ns |  13.135 ns |  0.20 |         - |          NA |
|       DirectAccessReadOracleDecimalAdv |             .NET 7.0 |   4.287 ns |   4.283 ns |  0.06 |         - |          NA |
|   TypeMapperReadOracleDecimalAsDecimal |             .NET 7.0 |   5.390 ns |   5.490 ns |  0.08 |         - |          NA |
| DirectAccessReadOracleDecimalAsDecimal |             .NET 7.0 |   3.325 ns |   3.355 ns |  0.05 |         - |          NA |
|       TypeMapperReadOracleDecimalAsInt |             .NET 7.0 |   9.816 ns |   9.844 ns |  0.15 |         - |          NA |
|     DirectAccessReadOracleDecimalAsInt |             .NET 7.0 |   7.969 ns |   8.026 ns |  0.12 |         - |          NA |
|      TypeMapperReadOracleDecimalAsLong |             .NET 7.0 |   8.842 ns |   8.700 ns |  0.13 |         - |          NA |
|    DirectAccessReadOracleDecimalAsLong |             .NET 7.0 |   6.569 ns |   6.633 ns |  0.10 |         - |          NA |
|        TypeMapperReadOracleTimeStampTZ |        .NET Core 3.1 |  70.183 ns |  70.910 ns |  1.04 |         - |          NA |
|      DirectAccessReadOracleTimeStampTZ |        .NET Core 3.1 |  69.039 ns |  69.189 ns |  1.02 |         - |          NA |
|       TypeMapperReadOracleTimeStampLTZ |        .NET Core 3.1 |  31.634 ns |  31.664 ns |  0.47 |         - |          NA |
|     DirectAccessReadOracleTimeStampLTZ |        .NET Core 3.1 |  68.394 ns |  67.883 ns |  1.01 |         - |          NA |
|         TypeMapperReadOracleDecimalAdv |        .NET Core 3.1 |  13.666 ns |  13.748 ns |  0.20 |         - |          NA |
|       DirectAccessReadOracleDecimalAdv |        .NET Core 3.1 |   8.800 ns |   8.504 ns |  0.13 |         - |          NA |
|   TypeMapperReadOracleDecimalAsDecimal |        .NET Core 3.1 |   8.062 ns |   8.085 ns |  0.12 |         - |          NA |
| DirectAccessReadOracleDecimalAsDecimal |        .NET Core 3.1 |   3.110 ns |   4.257 ns |  0.05 |         - |          NA |
|       TypeMapperReadOracleDecimalAsInt |        .NET Core 3.1 |  10.866 ns |  11.669 ns |  0.13 |         - |          NA |
|     DirectAccessReadOracleDecimalAsInt |        .NET Core 3.1 |   8.396 ns |   8.497 ns |  0.12 |         - |          NA |
|      TypeMapperReadOracleDecimalAsLong |        .NET Core 3.1 |  12.211 ns |  12.206 ns |  0.18 |         - |          NA |
|    DirectAccessReadOracleDecimalAsLong |        .NET Core 3.1 |   7.740 ns |   8.105 ns |  0.09 |         - |          NA |
|        TypeMapperReadOracleTimeStampTZ | .NET Framework 4.7.2 | 138.640 ns | 139.187 ns |  2.06 |         - |          NA |
|      DirectAccessReadOracleTimeStampTZ | .NET Framework 4.7.2 |  67.426 ns |  67.469 ns |  1.00 |         - |          NA |
|       TypeMapperReadOracleTimeStampLTZ | .NET Framework 4.7.2 | 130.793 ns | 145.440 ns |  1.92 |         - |          NA |
|     DirectAccessReadOracleTimeStampLTZ | .NET Framework 4.7.2 |  66.906 ns |  67.021 ns |  0.99 |         - |          NA |
|         TypeMapperReadOracleDecimalAdv | .NET Framework 4.7.2 |  52.437 ns |  52.606 ns |  0.78 |         - |          NA |
|       DirectAccessReadOracleDecimalAdv | .NET Framework 4.7.2 |   7.700 ns |   7.676 ns |  0.11 |         - |          NA |
|   TypeMapperReadOracleDecimalAsDecimal | .NET Framework 4.7.2 |  34.815 ns |  35.132 ns |  0.52 |         - |          NA |
| DirectAccessReadOracleDecimalAsDecimal | .NET Framework 4.7.2 |   6.143 ns |   6.174 ns |  0.09 |         - |          NA |
|       TypeMapperReadOracleDecimalAsInt | .NET Framework 4.7.2 |  36.944 ns |  37.103 ns |  0.55 |         - |          NA |
|     DirectAccessReadOracleDecimalAsInt | .NET Framework 4.7.2 |  10.414 ns |  10.463 ns |  0.15 |         - |          NA |
|      TypeMapperReadOracleDecimalAsLong | .NET Framework 4.7.2 |  37.235 ns |  37.562 ns |  0.55 |         - |          NA |
|    DirectAccessReadOracleDecimalAsLong | .NET Framework 4.7.2 |  10.742 ns |  10.791 ns |  0.16 |         - |          NA |
