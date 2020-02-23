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
|                                 Method |       Runtime |       Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------------------------------------- |-------------- |-----------:|----------:|----------:|------:|--------:|------:|------:|------:|----------:|
|        TypeMapperReadOracleTimeStampTZ |    .NET 4.6.2 | 127.516 ns | 1.9288 ns | 0.1057 ns |  2.08 |    0.04 |     - |     - |     - |         - |
|      DirectAccessReadOracleTimeStampTZ |    .NET 4.6.2 |  61.572 ns | 3.5123 ns | 0.9121 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|       TypeMapperReadOracleTimeStampLTZ |    .NET 4.6.2 | 133.739 ns | 2.4651 ns | 0.3815 ns |  2.18 |    0.03 |     - |     - |     - |         - |
|     DirectAccessReadOracleTimeStampLTZ |    .NET 4.6.2 |  59.304 ns | 3.5124 ns | 0.9122 ns |  0.96 |    0.02 |     - |     - |     - |         - |
|         TypeMapperReadOracleDecimalAdv |    .NET 4.6.2 |  41.266 ns | 5.4225 ns | 1.4082 ns |  0.67 |    0.03 |     - |     - |     - |         - |
|       DirectAccessReadOracleDecimalAdv |    .NET 4.6.2 |   6.834 ns | 0.1612 ns | 0.0088 ns |  0.11 |    0.00 |     - |     - |     - |         - |
|   TypeMapperReadOracleDecimalAsDecimal |    .NET 4.6.2 |  26.056 ns | 0.5403 ns | 0.0836 ns |  0.42 |    0.01 |     - |     - |     - |         - |
| DirectAccessReadOracleDecimalAsDecimal |    .NET 4.6.2 |   5.485 ns | 0.0943 ns | 0.0146 ns |  0.09 |    0.00 |     - |     - |     - |         - |
|       TypeMapperReadOracleDecimalAsInt |    .NET 4.6.2 |  29.319 ns | 1.6985 ns | 0.4411 ns |  0.48 |    0.01 |     - |     - |     - |         - |
|     DirectAccessReadOracleDecimalAsInt |    .NET 4.6.2 |   9.026 ns | 0.1007 ns | 0.0156 ns |  0.15 |    0.00 |     - |     - |     - |         - |
|      TypeMapperReadOracleDecimalAsLong |    .NET 4.6.2 |  28.935 ns | 0.4976 ns | 0.1292 ns |  0.47 |    0.01 |     - |     - |     - |         - |
|    DirectAccessReadOracleDecimalAsLong |    .NET 4.6.2 |   8.970 ns | 0.1967 ns | 0.0511 ns |  0.15 |    0.00 |     - |     - |     - |         - |
|        TypeMapperReadOracleTimeStampTZ | .NET Core 2.1 |  65.228 ns | 3.0981 ns | 0.8046 ns |  1.06 |    0.02 |     - |     - |     - |         - |
|      DirectAccessReadOracleTimeStampTZ | .NET Core 2.1 |  61.648 ns | 0.7986 ns | 0.1236 ns |  1.00 |    0.02 |     - |     - |     - |         - |
|       TypeMapperReadOracleTimeStampLTZ | .NET Core 2.1 |  65.367 ns | 1.0821 ns | 0.1675 ns |  1.06 |    0.02 |     - |     - |     - |         - |
|     DirectAccessReadOracleTimeStampLTZ | .NET Core 2.1 |  65.668 ns | 8.1241 ns | 2.1098 ns |  1.07 |    0.03 |     - |     - |     - |         - |
|         TypeMapperReadOracleDecimalAdv | .NET Core 2.1 |  21.163 ns | 0.2989 ns | 0.0776 ns |  0.34 |    0.00 |     - |     - |     - |         - |
|       DirectAccessReadOracleDecimalAdv | .NET Core 2.1 |   9.523 ns | 0.2168 ns | 0.0563 ns |  0.15 |    0.00 |     - |     - |     - |         - |
|   TypeMapperReadOracleDecimalAsDecimal | .NET Core 2.1 |  10.619 ns | 0.1712 ns | 0.0265 ns |  0.17 |    0.00 |     - |     - |     - |         - |
| DirectAccessReadOracleDecimalAsDecimal | .NET Core 2.1 |   8.460 ns | 0.1062 ns | 0.0164 ns |  0.14 |    0.00 |     - |     - |     - |         - |
|       TypeMapperReadOracleDecimalAsInt | .NET Core 2.1 |  12.432 ns | 0.2613 ns | 0.0679 ns |  0.20 |    0.00 |     - |     - |     - |         - |
|     DirectAccessReadOracleDecimalAsInt | .NET Core 2.1 |  10.856 ns | 0.1466 ns | 0.0381 ns |  0.18 |    0.00 |     - |     - |     - |         - |
|      TypeMapperReadOracleDecimalAsLong | .NET Core 2.1 |  12.833 ns | 1.4598 ns | 0.3791 ns |  0.21 |    0.01 |     - |     - |     - |         - |
|    DirectAccessReadOracleDecimalAsLong | .NET Core 2.1 |  11.079 ns | 0.5702 ns | 0.1481 ns |  0.18 |    0.00 |     - |     - |     - |         - |
|        TypeMapperReadOracleTimeStampTZ | .NET Core 3.1 |  66.111 ns | 1.2843 ns | 0.3335 ns |  1.07 |    0.02 |     - |     - |     - |         - |
|      DirectAccessReadOracleTimeStampTZ | .NET Core 3.1 |  62.240 ns | 1.0503 ns | 0.2728 ns |  1.01 |    0.02 |     - |     - |     - |         - |
|       TypeMapperReadOracleTimeStampLTZ | .NET Core 3.1 |  68.644 ns | 1.0092 ns | 0.2621 ns |  1.12 |    0.01 |     - |     - |     - |         - |
|     DirectAccessReadOracleTimeStampLTZ | .NET Core 3.1 |  65.093 ns | 1.1032 ns | 0.2865 ns |  1.06 |    0.02 |     - |     - |     - |         - |
|         TypeMapperReadOracleDecimalAdv | .NET Core 3.1 |  11.749 ns | 0.1716 ns | 0.0446 ns |  0.19 |    0.00 |     - |     - |     - |         - |
|       DirectAccessReadOracleDecimalAdv | .NET Core 3.1 |   7.355 ns | 0.1684 ns | 0.0261 ns |  0.12 |    0.00 |     - |     - |     - |         - |
|   TypeMapperReadOracleDecimalAsDecimal | .NET Core 3.1 |   7.092 ns | 0.1406 ns | 0.0218 ns |  0.12 |    0.00 |     - |     - |     - |         - |
| DirectAccessReadOracleDecimalAsDecimal | .NET Core 3.1 |   3.545 ns | 0.1270 ns | 0.0330 ns |  0.06 |    0.00 |     - |     - |     - |         - |
|       TypeMapperReadOracleDecimalAsInt | .NET Core 3.1 |   9.341 ns | 0.6585 ns | 0.1710 ns |  0.15 |    0.00 |     - |     - |     - |         - |
|     DirectAccessReadOracleDecimalAsInt | .NET Core 3.1 |   6.882 ns | 0.2634 ns | 0.0684 ns |  0.11 |    0.00 |     - |     - |     - |         - |
|      TypeMapperReadOracleDecimalAsLong | .NET Core 3.1 |   9.566 ns | 0.2513 ns | 0.0653 ns |  0.16 |    0.00 |     - |     - |     - |         - |
|    DirectAccessReadOracleDecimalAsLong | .NET Core 3.1 |   6.589 ns | 0.1245 ns | 0.0323 ns |  0.11 |    0.00 |     - |     - |     - |         - |
