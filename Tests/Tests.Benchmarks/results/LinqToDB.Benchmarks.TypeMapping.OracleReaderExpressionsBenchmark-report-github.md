``` ini

BenchmarkDotNet=v0.12.1.1533-nightly, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417995 Hz, Resolution=292.5692 ns, Timer=TSC
  [Host]     : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-GUCTZK : .NET 5.0.5 (5.0.521.16609), X64 RyuJIT
  Job-IOHEYN : .NET Core 3.1.14 (CoreCLR 4.700.21.16201, CoreFX 4.700.21.16208), X64 RyuJIT
  Job-FWTWYQ : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT

Jit=RyuJit  Platform=X64  

```
|                                 Method |              Runtime |       Mean |     Median | Ratio | Allocated |
|--------------------------------------- |--------------------- |-----------:|-----------:|------:|----------:|
|        TypeMapperReadOracleTimeStampTZ |             .NET 5.0 |  48.358 ns |  47.897 ns |  0.75 |         - |
|      DirectAccessReadOracleTimeStampTZ |             .NET 5.0 |  45.154 ns |  45.233 ns |  0.70 |         - |
|       TypeMapperReadOracleTimeStampLTZ |             .NET 5.0 |  47.590 ns |  47.693 ns |  0.73 |         - |
|     DirectAccessReadOracleTimeStampLTZ |             .NET 5.0 |  44.566 ns |  44.573 ns |  0.70 |         - |
|         TypeMapperReadOracleDecimalAdv |             .NET 5.0 |  12.788 ns |  12.635 ns |  0.20 |         - |
|       DirectAccessReadOracleDecimalAdv |             .NET 5.0 |   7.863 ns |   7.870 ns |  0.12 |         - |
|   TypeMapperReadOracleDecimalAsDecimal |             .NET 5.0 |   7.240 ns |   7.223 ns |  0.11 |         - |
| DirectAccessReadOracleDecimalAsDecimal |             .NET 5.0 |   3.319 ns |   3.314 ns |  0.05 |         - |
|       TypeMapperReadOracleDecimalAsInt |             .NET 5.0 |   9.202 ns |   9.144 ns |  0.14 |         - |
|     DirectAccessReadOracleDecimalAsInt |             .NET 5.0 |   6.722 ns |   6.653 ns |  0.10 |         - |
|      TypeMapperReadOracleDecimalAsLong |             .NET 5.0 |   9.803 ns |   9.750 ns |  0.15 |         - |
|    DirectAccessReadOracleDecimalAsLong |             .NET 5.0 |   6.421 ns |   6.410 ns |  0.10 |         - |
|        TypeMapperReadOracleTimeStampTZ |        .NET Core 3.1 |  62.466 ns |  61.585 ns |  0.97 |         - |
|      DirectAccessReadOracleTimeStampTZ |        .NET Core 3.1 |  60.017 ns |  59.393 ns |  0.93 |         - |
|       TypeMapperReadOracleTimeStampLTZ |        .NET Core 3.1 |  61.093 ns |  61.088 ns |  0.96 |         - |
|     DirectAccessReadOracleTimeStampLTZ |        .NET Core 3.1 |  63.388 ns |  62.022 ns |  0.99 |         - |
|         TypeMapperReadOracleDecimalAdv |        .NET Core 3.1 |  11.553 ns |  11.511 ns |  0.18 |         - |
|       DirectAccessReadOracleDecimalAdv |        .NET Core 3.1 |   7.634 ns |   7.551 ns |  0.12 |         - |
|   TypeMapperReadOracleDecimalAsDecimal |        .NET Core 3.1 |   7.257 ns |   7.262 ns |  0.11 |         - |
| DirectAccessReadOracleDecimalAsDecimal |        .NET Core 3.1 |   3.679 ns |   3.585 ns |  0.06 |         - |
|       TypeMapperReadOracleDecimalAsInt |        .NET Core 3.1 |   9.249 ns |   9.144 ns |  0.14 |         - |
|     DirectAccessReadOracleDecimalAsInt |        .NET Core 3.1 |   6.848 ns |   6.813 ns |  0.11 |         - |
|      TypeMapperReadOracleDecimalAsLong |        .NET Core 3.1 |   9.923 ns |   9.774 ns |  0.15 |         - |
|    DirectAccessReadOracleDecimalAsLong |        .NET Core 3.1 |   6.872 ns |   6.824 ns |  0.11 |         - |
|        TypeMapperReadOracleTimeStampTZ | .NET Framework 4.7.2 | 135.873 ns | 135.227 ns |  2.10 |         - |
|      DirectAccessReadOracleTimeStampTZ | .NET Framework 4.7.2 |  64.412 ns |  63.491 ns |  1.00 |         - |
|       TypeMapperReadOracleTimeStampLTZ | .NET Framework 4.7.2 | 129.401 ns | 129.199 ns |  2.03 |         - |
|     DirectAccessReadOracleTimeStampLTZ | .NET Framework 4.7.2 |  59.963 ns |  59.895 ns |  0.94 |         - |
|         TypeMapperReadOracleDecimalAdv | .NET Framework 4.7.2 |  40.873 ns |  40.089 ns |  0.64 |         - |
|       DirectAccessReadOracleDecimalAdv | .NET Framework 4.7.2 |   6.303 ns |   6.287 ns |  0.10 |         - |
|   TypeMapperReadOracleDecimalAsDecimal | .NET Framework 4.7.2 |  27.390 ns |  27.409 ns |  0.42 |         - |
| DirectAccessReadOracleDecimalAsDecimal | .NET Framework 4.7.2 |   5.801 ns |   5.803 ns |  0.09 |         - |
|       TypeMapperReadOracleDecimalAsInt | .NET Framework 4.7.2 |  28.745 ns |  28.727 ns |  0.45 |         - |
|     DirectAccessReadOracleDecimalAsInt | .NET Framework 4.7.2 |   8.442 ns |   8.434 ns |  0.13 |         - |
|      TypeMapperReadOracleDecimalAsLong | .NET Framework 4.7.2 |  29.937 ns |  30.022 ns |  0.47 |         - |
|    DirectAccessReadOracleDecimalAsLong | .NET Framework 4.7.2 |   9.023 ns |   9.061 ns |  0.14 |         - |
