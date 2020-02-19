``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417995 Hz, Resolution=292.5692 ns, Timer=TSC
  [Host]     : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-ZXOHUL : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-TAKBNN : .NET Core 2.1.15 (CoreCLR 4.6.28325.01, CoreFX 4.6.28327.02), X64 RyuJIT
  Job-WOIQBX : .NET Core 3.1.1 (CoreCLR 4.700.19.60701, CoreFX 4.700.19.60801), X64 RyuJIT

Jit=RyuJit  Platform=X64  MaxIterationCount=10  
MinIterationCount=5  WarmupCount=2  

```
|                                 Method |       Runtime |       Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------------------------------------- |-------------- |-----------:|----------:|----------:|------:|--------:|------:|------:|------:|----------:|
|        TypeMapperReadOracleTimeStampTZ |    .NET 4.6.2 | 132.213 ns | 8.3265 ns | 5.5074 ns |  2.18 |    0.10 |     - |     - |     - |         - |
|      DirectAccessReadOracleTimeStampTZ |    .NET 4.6.2 |  60.720 ns | 1.1356 ns | 0.7511 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|       TypeMapperReadOracleTimeStampLTZ |    .NET 4.6.2 | 141.672 ns | 5.4757 ns | 3.6218 ns |  2.33 |    0.06 |     - |     - |     - |         - |
|     DirectAccessReadOracleTimeStampLTZ |    .NET 4.6.2 |  61.512 ns | 2.3485 ns | 1.5534 ns |  1.01 |    0.03 |     - |     - |     - |         - |
|         TypeMapperReadOracleDecimalAdv |    .NET 4.6.2 |  41.376 ns | 1.0564 ns | 0.6987 ns |  0.68 |    0.01 |     - |     - |     - |         - |
|       DirectAccessReadOracleDecimalAdv |    .NET 4.6.2 |   6.814 ns | 0.3068 ns | 0.2029 ns |  0.11 |    0.00 |     - |     - |     - |         - |
|   TypeMapperReadOracleDecimalAsDecimal |    .NET 4.6.2 |  29.176 ns | 1.2963 ns | 0.8574 ns |  0.48 |    0.02 |     - |     - |     - |         - |
| DirectAccessReadOracleDecimalAsDecimal |    .NET 4.6.2 |   5.948 ns | 0.1364 ns | 0.0902 ns |  0.10 |    0.00 |     - |     - |     - |         - |
|       TypeMapperReadOracleDecimalAsInt |    .NET 4.6.2 |  29.581 ns | 0.6229 ns | 0.1618 ns |  0.48 |    0.00 |     - |     - |     - |         - |
|     DirectAccessReadOracleDecimalAsInt |    .NET 4.6.2 |   8.904 ns | 0.2097 ns | 0.0748 ns |  0.15 |    0.00 |     - |     - |     - |         - |
|      TypeMapperReadOracleDecimalAsLong |    .NET 4.6.2 |  29.537 ns | 0.6979 ns | 0.4616 ns |  0.49 |    0.01 |     - |     - |     - |         - |
|    DirectAccessReadOracleDecimalAsLong |    .NET 4.6.2 |   9.279 ns | 0.2801 ns | 0.1853 ns |  0.15 |    0.00 |     - |     - |     - |         - |
|        TypeMapperReadOracleTimeStampTZ | .NET Core 2.1 |  69.885 ns | 2.6035 ns | 1.7220 ns |  1.15 |    0.04 |     - |     - |     - |         - |
|      DirectAccessReadOracleTimeStampTZ | .NET Core 2.1 |  64.863 ns | 2.6816 ns | 1.7737 ns |  1.07 |    0.03 |     - |     - |     - |         - |
|       TypeMapperReadOracleTimeStampLTZ | .NET Core 2.1 |  66.703 ns | 1.1658 ns | 0.5176 ns |  1.10 |    0.02 |     - |     - |     - |         - |
|     DirectAccessReadOracleTimeStampLTZ | .NET Core 2.1 |  63.778 ns | 1.1023 ns | 0.4894 ns |  1.05 |    0.02 |     - |     - |     - |         - |
|         TypeMapperReadOracleDecimalAdv | .NET Core 2.1 |  21.872 ns | 0.3793 ns | 0.0985 ns |  0.36 |    0.00 |     - |     - |     - |         - |
|       DirectAccessReadOracleDecimalAdv | .NET Core 2.1 |   9.757 ns | 0.1912 ns | 0.1000 ns |  0.16 |    0.00 |     - |     - |     - |         - |
|   TypeMapperReadOracleDecimalAsDecimal | .NET Core 2.1 |  10.854 ns | 0.2699 ns | 0.1785 ns |  0.18 |    0.00 |     - |     - |     - |         - |
| DirectAccessReadOracleDecimalAsDecimal | .NET Core 2.1 |   9.099 ns | 0.5991 ns | 0.3963 ns |  0.15 |    0.01 |     - |     - |     - |         - |
|       TypeMapperReadOracleDecimalAsInt | .NET Core 2.1 |  12.643 ns | 0.2846 ns | 0.1882 ns |  0.21 |    0.00 |     - |     - |     - |         - |
|     DirectAccessReadOracleDecimalAsInt | .NET Core 2.1 |  11.037 ns | 0.2054 ns | 0.0912 ns |  0.18 |    0.00 |     - |     - |     - |         - |
|      TypeMapperReadOracleDecimalAsLong | .NET Core 2.1 |  12.371 ns | 0.1410 ns | 0.0366 ns |  0.20 |    0.00 |     - |     - |     - |         - |
|    DirectAccessReadOracleDecimalAsLong | .NET Core 2.1 |  11.393 ns | 0.3336 ns | 0.1985 ns |  0.19 |    0.00 |     - |     - |     - |         - |
|        TypeMapperReadOracleTimeStampTZ | .NET Core 3.1 |  74.463 ns | 6.3825 ns | 3.7981 ns |  1.22 |    0.06 |     - |     - |     - |         - |
|      DirectAccessReadOracleTimeStampTZ | .NET Core 3.1 |  62.866 ns | 1.1300 ns | 0.4030 ns |  1.03 |    0.02 |     - |     - |     - |         - |
|       TypeMapperReadOracleTimeStampLTZ | .NET Core 3.1 |  69.148 ns | 0.9303 ns | 0.2416 ns |  1.13 |    0.02 |     - |     - |     - |         - |
|     DirectAccessReadOracleTimeStampLTZ | .NET Core 3.1 |  66.235 ns | 0.6389 ns | 0.1659 ns |  1.08 |    0.01 |     - |     - |     - |         - |
|         TypeMapperReadOracleDecimalAdv | .NET Core 3.1 |  12.366 ns | 0.3526 ns | 0.2332 ns |  0.20 |    0.00 |     - |     - |     - |         - |
|       DirectAccessReadOracleDecimalAdv | .NET Core 3.1 |   7.460 ns | 0.1621 ns | 0.0421 ns |  0.12 |    0.00 |     - |     - |     - |         - |
|   TypeMapperReadOracleDecimalAsDecimal | .NET Core 3.1 |   7.227 ns | 0.1685 ns | 0.1115 ns |  0.12 |    0.00 |     - |     - |     - |         - |
| DirectAccessReadOracleDecimalAsDecimal | .NET Core 3.1 |   3.515 ns | 0.1376 ns | 0.0819 ns |  0.06 |    0.00 |     - |     - |     - |         - |
|       TypeMapperReadOracleDecimalAsInt | .NET Core 3.1 |   9.737 ns | 0.2060 ns | 0.1226 ns |  0.16 |    0.00 |     - |     - |     - |         - |
|     DirectAccessReadOracleDecimalAsInt | .NET Core 3.1 |   6.766 ns | 0.1424 ns | 0.0370 ns |  0.11 |    0.00 |     - |     - |     - |         - |
|      TypeMapperReadOracleDecimalAsLong | .NET Core 3.1 |   9.182 ns | 0.3012 ns | 0.1337 ns |  0.15 |    0.00 |     - |     - |     - |         - |
|    DirectAccessReadOracleDecimalAsLong | .NET Core 3.1 |   6.901 ns | 0.2362 ns | 0.1235 ns |  0.11 |    0.00 |     - |     - |     - |         - |
