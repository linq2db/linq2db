``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-HCNGBR : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-XBFFOD : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-INBZNN : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-THZJXI : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                                 Method |              Runtime |       Mean | Allocated |
|--------------------------------------- |--------------------- |-----------:|----------:|
|        TypeMapperReadOracleTimeStampTZ |             .NET 6.0 |  43.598 ns |         - |
|      DirectAccessReadOracleTimeStampTZ |             .NET 6.0 |  43.215 ns |         - |
|       TypeMapperReadOracleTimeStampLTZ |             .NET 6.0 |  45.503 ns |         - |
|     DirectAccessReadOracleTimeStampLTZ |             .NET 6.0 |  29.129 ns |         - |
|         TypeMapperReadOracleDecimalAdv |             .NET 6.0 |  13.653 ns |         - |
|       DirectAccessReadOracleDecimalAdv |             .NET 6.0 |   7.055 ns |         - |
|   TypeMapperReadOracleDecimalAsDecimal |             .NET 6.0 |   6.326 ns |         - |
| DirectAccessReadOracleDecimalAsDecimal |             .NET 6.0 |   4.237 ns |         - |
|       TypeMapperReadOracleDecimalAsInt |             .NET 6.0 |  11.314 ns |         - |
|     DirectAccessReadOracleDecimalAsInt |             .NET 6.0 |   8.001 ns |         - |
|      TypeMapperReadOracleDecimalAsLong |             .NET 6.0 |  11.888 ns |         - |
|    DirectAccessReadOracleDecimalAsLong |             .NET 6.0 |   8.618 ns |         - |
|        TypeMapperReadOracleTimeStampTZ |             .NET 7.0 |  45.988 ns |         - |
|      DirectAccessReadOracleTimeStampTZ |             .NET 7.0 |  42.317 ns |         - |
|       TypeMapperReadOracleTimeStampLTZ |             .NET 7.0 |  42.511 ns |         - |
|     DirectAccessReadOracleTimeStampLTZ |             .NET 7.0 |  43.590 ns |         - |
|         TypeMapperReadOracleDecimalAdv |             .NET 7.0 |  12.598 ns |         - |
|       DirectAccessReadOracleDecimalAdv |             .NET 7.0 |   3.512 ns |         - |
|   TypeMapperReadOracleDecimalAsDecimal |             .NET 7.0 |   5.536 ns |         - |
| DirectAccessReadOracleDecimalAsDecimal |             .NET 7.0 |   3.354 ns |         - |
|       TypeMapperReadOracleDecimalAsInt |             .NET 7.0 |   9.561 ns |         - |
|     DirectAccessReadOracleDecimalAsInt |             .NET 7.0 |   7.623 ns |         - |
|      TypeMapperReadOracleDecimalAsLong |             .NET 7.0 |   8.085 ns |         - |
|    DirectAccessReadOracleDecimalAsLong |             .NET 7.0 |   6.000 ns |         - |
|        TypeMapperReadOracleTimeStampTZ |        .NET Core 3.1 |  63.736 ns |         - |
|      DirectAccessReadOracleTimeStampTZ |        .NET Core 3.1 |  68.913 ns |         - |
|       TypeMapperReadOracleTimeStampLTZ |        .NET Core 3.1 |  75.763 ns |         - |
|     DirectAccessReadOracleTimeStampLTZ |        .NET Core 3.1 |  70.156 ns |         - |
|         TypeMapperReadOracleDecimalAdv |        .NET Core 3.1 |  12.029 ns |         - |
|       DirectAccessReadOracleDecimalAdv |        .NET Core 3.1 |   9.610 ns |         - |
|   TypeMapperReadOracleDecimalAsDecimal |        .NET Core 3.1 |   8.076 ns |         - |
| DirectAccessReadOracleDecimalAsDecimal |        .NET Core 3.1 |   3.305 ns |         - |
|       TypeMapperReadOracleDecimalAsInt |        .NET Core 3.1 |  11.885 ns |         - |
|     DirectAccessReadOracleDecimalAsInt |        .NET Core 3.1 |   7.857 ns |         - |
|      TypeMapperReadOracleDecimalAsLong |        .NET Core 3.1 |  13.451 ns |         - |
|    DirectAccessReadOracleDecimalAsLong |        .NET Core 3.1 |   8.088 ns |         - |
|        TypeMapperReadOracleTimeStampTZ | .NET Framework 4.7.2 | 138.186 ns |         - |
|      DirectAccessReadOracleTimeStampTZ | .NET Framework 4.7.2 |  65.889 ns |         - |
|       TypeMapperReadOracleTimeStampLTZ | .NET Framework 4.7.2 | 147.381 ns |         - |
|     DirectAccessReadOracleTimeStampLTZ | .NET Framework 4.7.2 |  66.851 ns |         - |
|         TypeMapperReadOracleDecimalAdv | .NET Framework 4.7.2 |  50.260 ns |         - |
|       DirectAccessReadOracleDecimalAdv | .NET Framework 4.7.2 |   7.688 ns |         - |
|   TypeMapperReadOracleDecimalAsDecimal | .NET Framework 4.7.2 |  34.604 ns |         - |
| DirectAccessReadOracleDecimalAsDecimal | .NET Framework 4.7.2 |   5.823 ns |         - |
|       TypeMapperReadOracleDecimalAsInt | .NET Framework 4.7.2 |  36.328 ns |         - |
|     DirectAccessReadOracleDecimalAsInt | .NET Framework 4.7.2 |   9.854 ns |         - |
|      TypeMapperReadOracleDecimalAsLong | .NET Framework 4.7.2 |  36.254 ns |         - |
|    DirectAccessReadOracleDecimalAsLong | .NET Framework 4.7.2 |  10.860 ns |         - |
