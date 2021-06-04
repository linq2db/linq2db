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
|        TypeMapperReadOracleTimeStampTZ |             .NET 5.0 |  46.379 ns |  45.799 ns |  0.77 |         - |
|      DirectAccessReadOracleTimeStampTZ |             .NET 5.0 |  45.945 ns |  45.989 ns |  0.76 |         - |
|       TypeMapperReadOracleTimeStampLTZ |             .NET 5.0 |  48.541 ns |  48.494 ns |  0.80 |         - |
|     DirectAccessReadOracleTimeStampLTZ |             .NET 5.0 |  44.030 ns |  44.025 ns |  0.73 |         - |
|         TypeMapperReadOracleDecimalAdv |             .NET 5.0 |  12.304 ns |  12.294 ns |  0.20 |         - |
|       DirectAccessReadOracleDecimalAdv |             .NET 5.0 |   7.885 ns |   7.744 ns |  0.13 |         - |
|   TypeMapperReadOracleDecimalAsDecimal |             .NET 5.0 |   6.676 ns |   6.679 ns |  0.11 |         - |
| DirectAccessReadOracleDecimalAsDecimal |             .NET 5.0 |   3.281 ns |   3.288 ns |  0.05 |         - |
|       TypeMapperReadOracleDecimalAsInt |             .NET 5.0 |   9.328 ns |   9.280 ns |  0.15 |         - |
|     DirectAccessReadOracleDecimalAsInt |             .NET 5.0 |   6.403 ns |   6.393 ns |  0.11 |         - |
|      TypeMapperReadOracleDecimalAsLong |             .NET 5.0 |   8.570 ns |   8.526 ns |  0.14 |         - |
|    DirectAccessReadOracleDecimalAsLong |             .NET 5.0 |   6.745 ns |   6.561 ns |  0.12 |         - |
|        TypeMapperReadOracleTimeStampTZ |        .NET Core 3.1 |  64.422 ns |  64.520 ns |  1.06 |         - |
|      DirectAccessReadOracleTimeStampTZ |        .NET Core 3.1 |  60.763 ns |  60.677 ns |  1.00 |         - |
|       TypeMapperReadOracleTimeStampLTZ |        .NET Core 3.1 |  66.764 ns |  66.683 ns |  1.10 |         - |
|     DirectAccessReadOracleTimeStampLTZ |        .NET Core 3.1 |  65.419 ns |  65.911 ns |  1.08 |         - |
|         TypeMapperReadOracleDecimalAdv |        .NET Core 3.1 |  11.983 ns |  12.002 ns |  0.20 |         - |
|       DirectAccessReadOracleDecimalAdv |        .NET Core 3.1 |   7.598 ns |   7.593 ns |  0.13 |         - |
|   TypeMapperReadOracleDecimalAsDecimal |        .NET Core 3.1 |   7.123 ns |   7.059 ns |  0.12 |         - |
| DirectAccessReadOracleDecimalAsDecimal |        .NET Core 3.1 |   2.971 ns |   2.974 ns |  0.05 |         - |
|       TypeMapperReadOracleDecimalAsInt |        .NET Core 3.1 |   8.862 ns |   8.885 ns |  0.15 |         - |
|     DirectAccessReadOracleDecimalAsInt |        .NET Core 3.1 |   6.745 ns |   6.749 ns |  0.11 |         - |
|      TypeMapperReadOracleDecimalAsLong |        .NET Core 3.1 |   9.427 ns |   9.439 ns |  0.16 |         - |
|    DirectAccessReadOracleDecimalAsLong |        .NET Core 3.1 |   6.370 ns |   6.379 ns |  0.10 |         - |
|        TypeMapperReadOracleTimeStampTZ | .NET Framework 4.7.2 | 126.153 ns | 125.535 ns |  2.13 |         - |
|      DirectAccessReadOracleTimeStampTZ | .NET Framework 4.7.2 |  60.713 ns |  60.683 ns |  1.00 |         - |
|       TypeMapperReadOracleTimeStampLTZ | .NET Framework 4.7.2 | 129.382 ns | 128.992 ns |  2.13 |         - |
|     DirectAccessReadOracleTimeStampLTZ | .NET Framework 4.7.2 |  57.828 ns |  57.374 ns |  0.96 |         - |
|         TypeMapperReadOracleDecimalAdv | .NET Framework 4.7.2 |  39.244 ns |  38.870 ns |  0.65 |         - |
|       DirectAccessReadOracleDecimalAdv | .NET Framework 4.7.2 |   7.052 ns |   6.942 ns |  0.11 |         - |
|   TypeMapperReadOracleDecimalAsDecimal | .NET Framework 4.7.2 |  25.447 ns |  25.444 ns |  0.42 |         - |
| DirectAccessReadOracleDecimalAsDecimal | .NET Framework 4.7.2 |   5.472 ns |   5.393 ns |  0.09 |         - |
|       TypeMapperReadOracleDecimalAsInt | .NET Framework 4.7.2 |  27.926 ns |  27.935 ns |  0.46 |         - |
|     DirectAccessReadOracleDecimalAsInt | .NET Framework 4.7.2 |   9.147 ns |   9.136 ns |  0.15 |         - |
|      TypeMapperReadOracleDecimalAsLong | .NET Framework 4.7.2 |  28.855 ns |  28.418 ns |  0.48 |         - |
|    DirectAccessReadOracleDecimalAsLong | .NET Framework 4.7.2 |   8.841 ns |   8.847 ns |  0.15 |         - |
