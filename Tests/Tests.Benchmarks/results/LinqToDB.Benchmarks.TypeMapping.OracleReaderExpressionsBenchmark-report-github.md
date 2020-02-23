``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417995 Hz, Resolution=292.5692 ns, Timer=TSC
  [Host]     : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-FSMYUH : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-TSQXSD : .NET Core 2.1.15 (CoreCLR 4.6.28325.01, CoreFX 4.6.28327.02), X64 RyuJIT
  Job-OUTKHJ : .NET Core 3.1.1 (CoreCLR 4.700.19.60701, CoreFX 4.700.19.60801), X64 RyuJIT

Jit=RyuJit  Platform=X64  MaxIterationCount=5  
MinIterationCount=3  WarmupCount=2  

```
|                                 Method |       Runtime |       Mean |      Error |    StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------------------------------------- |-------------- |-----------:|-----------:|----------:|------:|--------:|------:|------:|------:|----------:|
|        TypeMapperReadOracleTimeStampTZ |    .NET 4.6.2 | 119.490 ns |  3.1471 ns | 0.8173 ns |  1.86 |    0.07 |     - |     - |     - |         - |
|      DirectAccessReadOracleTimeStampTZ |    .NET 4.6.2 |  64.319 ns |  9.1243 ns | 2.3696 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|       TypeMapperReadOracleTimeStampLTZ |    .NET 4.6.2 | 134.098 ns |  1.8368 ns | 0.4770 ns |  2.09 |    0.07 |     - |     - |     - |         - |
|     DirectAccessReadOracleTimeStampLTZ |    .NET 4.6.2 |  59.539 ns |  2.2682 ns | 0.5890 ns |  0.93 |    0.04 |     - |     - |     - |         - |
|         TypeMapperReadOracleDecimalAdv |    .NET 4.6.2 |  39.780 ns |  1.3080 ns | 0.3397 ns |  0.62 |    0.02 |     - |     - |     - |         - |
|       DirectAccessReadOracleDecimalAdv |    .NET 4.6.2 |   6.835 ns |  0.3663 ns | 0.0951 ns |  0.11 |    0.00 |     - |     - |     - |         - |
|   TypeMapperReadOracleDecimalAsDecimal |    .NET 4.6.2 |  26.887 ns |  4.7229 ns | 1.2265 ns |  0.42 |    0.02 |     - |     - |     - |         - |
| DirectAccessReadOracleDecimalAsDecimal |    .NET 4.6.2 |   5.464 ns |  0.0720 ns | 0.0111 ns |  0.08 |    0.00 |     - |     - |     - |         - |
|       TypeMapperReadOracleDecimalAsInt |    .NET 4.6.2 |  29.182 ns |  0.7537 ns | 0.1957 ns |  0.45 |    0.02 |     - |     - |     - |         - |
|     DirectAccessReadOracleDecimalAsInt |    .NET 4.6.2 |   9.408 ns |  0.4334 ns | 0.1126 ns |  0.15 |    0.01 |     - |     - |     - |         - |
|      TypeMapperReadOracleDecimalAsLong |    .NET 4.6.2 |  28.762 ns |  0.6924 ns | 0.1798 ns |  0.45 |    0.02 |     - |     - |     - |         - |
|    DirectAccessReadOracleDecimalAsLong |    .NET 4.6.2 |   8.926 ns |  0.3001 ns | 0.0779 ns |  0.14 |    0.00 |     - |     - |     - |         - |
|        TypeMapperReadOracleTimeStampTZ | .NET Core 2.1 |  63.723 ns |  0.8612 ns | 0.0472 ns |  0.98 |    0.04 |     - |     - |     - |         - |
|      DirectAccessReadOracleTimeStampTZ | .NET Core 2.1 |  62.082 ns |  0.5933 ns | 0.0325 ns |  0.95 |    0.04 |     - |     - |     - |         - |
|       TypeMapperReadOracleTimeStampLTZ | .NET Core 2.1 |  64.485 ns |  1.7744 ns | 0.4608 ns |  1.00 |    0.03 |     - |     - |     - |         - |
|     DirectAccessReadOracleTimeStampLTZ | .NET Core 2.1 |  66.371 ns |  5.0568 ns | 1.3132 ns |  1.03 |    0.05 |     - |     - |     - |         - |
|         TypeMapperReadOracleDecimalAdv | .NET Core 2.1 |  20.570 ns |  0.4290 ns | 0.1114 ns |  0.32 |    0.01 |     - |     - |     - |         - |
|       DirectAccessReadOracleDecimalAdv | .NET Core 2.1 |   9.416 ns |  0.1041 ns | 0.0161 ns |  0.15 |    0.01 |     - |     - |     - |         - |
|   TypeMapperReadOracleDecimalAsDecimal | .NET Core 2.1 |  10.793 ns |  0.8353 ns | 0.2169 ns |  0.17 |    0.01 |     - |     - |     - |         - |
| DirectAccessReadOracleDecimalAsDecimal | .NET Core 2.1 |   8.386 ns |  0.2178 ns | 0.0565 ns |  0.13 |    0.00 |     - |     - |     - |         - |
|       TypeMapperReadOracleDecimalAsInt | .NET Core 2.1 |  12.270 ns |  0.6447 ns | 0.1674 ns |  0.19 |    0.01 |     - |     - |     - |         - |
|     DirectAccessReadOracleDecimalAsInt | .NET Core 2.1 |  11.644 ns |  0.6613 ns | 0.1717 ns |  0.18 |    0.01 |     - |     - |     - |         - |
|      TypeMapperReadOracleDecimalAsLong | .NET Core 2.1 |  14.830 ns |  6.2833 ns | 1.6318 ns |  0.23 |    0.03 |     - |     - |     - |         - |
|    DirectAccessReadOracleDecimalAsLong | .NET Core 2.1 |  12.142 ns |  1.3059 ns | 0.3391 ns |  0.19 |    0.01 |     - |     - |     - |         - |
|        TypeMapperReadOracleTimeStampTZ | .NET Core 3.1 |  68.270 ns | 12.7491 ns | 3.3109 ns |  1.06 |    0.08 |     - |     - |     - |         - |
|      DirectAccessReadOracleTimeStampTZ | .NET Core 3.1 |  68.206 ns | 12.0346 ns | 3.1253 ns |  1.06 |    0.06 |     - |     - |     - |         - |
|       TypeMapperReadOracleTimeStampLTZ | .NET Core 3.1 |  63.736 ns |  4.2123 ns | 1.0939 ns |  0.99 |    0.03 |     - |     - |     - |         - |
|     DirectAccessReadOracleTimeStampLTZ | .NET Core 3.1 |  62.939 ns |  5.4138 ns | 1.4059 ns |  0.98 |    0.04 |     - |     - |     - |         - |
|         TypeMapperReadOracleDecimalAdv | .NET Core 3.1 |  12.337 ns |  0.7520 ns | 0.1953 ns |  0.19 |    0.01 |     - |     - |     - |         - |
|       DirectAccessReadOracleDecimalAdv | .NET Core 3.1 |   7.385 ns |  0.5064 ns | 0.1315 ns |  0.11 |    0.01 |     - |     - |     - |         - |
|   TypeMapperReadOracleDecimalAsDecimal | .NET Core 3.1 |   7.286 ns |  0.8619 ns | 0.2238 ns |  0.11 |    0.00 |     - |     - |     - |         - |
| DirectAccessReadOracleDecimalAsDecimal | .NET Core 3.1 |   3.043 ns |  0.0698 ns | 0.0108 ns |  0.05 |    0.00 |     - |     - |     - |         - |
|       TypeMapperReadOracleDecimalAsInt | .NET Core 3.1 |   9.824 ns |  0.9765 ns | 0.2536 ns |  0.15 |    0.01 |     - |     - |     - |         - |
|     DirectAccessReadOracleDecimalAsInt | .NET Core 3.1 |   7.183 ns |  0.7353 ns | 0.1909 ns |  0.11 |    0.00 |     - |     - |     - |         - |
|      TypeMapperReadOracleDecimalAsLong | .NET Core 3.1 |   8.928 ns |  0.1414 ns | 0.0219 ns |  0.14 |    0.01 |     - |     - |     - |         - |
|    DirectAccessReadOracleDecimalAsLong | .NET Core 3.1 |   6.769 ns |  0.1054 ns | 0.0274 ns |  0.11 |    0.00 |     - |     - |     - |         - |
