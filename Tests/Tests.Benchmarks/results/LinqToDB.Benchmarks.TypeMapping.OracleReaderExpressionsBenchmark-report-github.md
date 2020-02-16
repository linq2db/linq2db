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
|                                 Method |       Runtime |       Mean |      Error |     StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------------------------------------- |-------------- |-----------:|-----------:|-----------:|------:|--------:|------:|------:|------:|----------:|
|        TypeMapperReadOracleTimeStampTZ |    .NET 4.6.2 | 227.449 ns | 20.0620 ns | 13.2698 ns |  2.15 |    0.13 |     - |     - |     - |         - |
|      DirectAccessReadOracleTimeStampTZ |    .NET 4.6.2 | 105.929 ns |  5.3414 ns |  3.5330 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|       TypeMapperReadOracleTimeStampLTZ |    .NET 4.6.2 | 217.643 ns | 19.3823 ns | 12.8202 ns |  2.06 |    0.13 |     - |     - |     - |         - |
|     DirectAccessReadOracleTimeStampLTZ |    .NET 4.6.2 | 101.981 ns |  6.7005 ns |  4.4319 ns |  0.96 |    0.06 |     - |     - |     - |         - |
|         TypeMapperReadOracleDecimalAdv |    .NET 4.6.2 |  67.761 ns |  8.3602 ns |  5.5297 ns |  0.64 |    0.07 |     - |     - |     - |         - |
|       DirectAccessReadOracleDecimalAdv |    .NET 4.6.2 |  11.063 ns |  0.7426 ns |  0.4912 ns |  0.10 |    0.01 |     - |     - |     - |         - |
|   TypeMapperReadOracleDecimalAsDecimal |    .NET 4.6.2 |  43.698 ns |  2.3568 ns |  1.4025 ns |  0.41 |    0.02 |     - |     - |     - |         - |
| DirectAccessReadOracleDecimalAsDecimal |    .NET 4.6.2 |   8.229 ns |  0.2077 ns |  0.0540 ns |  0.08 |    0.00 |     - |     - |     - |         - |
|       TypeMapperReadOracleDecimalAsInt |    .NET 4.6.2 |  45.034 ns |  3.1813 ns |  2.1042 ns |  0.43 |    0.03 |     - |     - |     - |         - |
|     DirectAccessReadOracleDecimalAsInt |    .NET 4.6.2 |  14.502 ns |  1.1716 ns |  0.7749 ns |  0.14 |    0.01 |     - |     - |     - |         - |
|      TypeMapperReadOracleDecimalAsLong |    .NET 4.6.2 |  49.105 ns |  3.4387 ns |  2.2745 ns |  0.46 |    0.03 |     - |     - |     - |         - |
|    DirectAccessReadOracleDecimalAsLong |    .NET 4.6.2 |  13.595 ns |  1.1151 ns |  0.7376 ns |  0.13 |    0.01 |     - |     - |     - |         - |
|        TypeMapperReadOracleTimeStampTZ | .NET Core 2.1 |  86.692 ns |  3.6337 ns |  2.4034 ns |  0.82 |    0.04 |     - |     - |     - |         - |
|      DirectAccessReadOracleTimeStampTZ | .NET Core 2.1 |  80.856 ns |  6.4401 ns |  3.8324 ns |  0.76 |    0.06 |     - |     - |     - |         - |
|       TypeMapperReadOracleTimeStampLTZ | .NET Core 2.1 |  87.636 ns | 11.5180 ns |  7.6184 ns |  0.83 |    0.09 |     - |     - |     - |         - |
|     DirectAccessReadOracleTimeStampLTZ | .NET Core 2.1 |  81.998 ns |  5.4330 ns |  3.5936 ns |  0.77 |    0.02 |     - |     - |     - |         - |
|         TypeMapperReadOracleDecimalAdv | .NET Core 2.1 |  27.916 ns |  1.1851 ns |  0.7839 ns |  0.26 |    0.02 |     - |     - |     - |         - |
|       DirectAccessReadOracleDecimalAdv | .NET Core 2.1 |  12.134 ns |  0.6403 ns |  0.4235 ns |  0.11 |    0.01 |     - |     - |     - |         - |
|   TypeMapperReadOracleDecimalAsDecimal | .NET Core 2.1 |  13.072 ns |  0.6594 ns |  0.4362 ns |  0.12 |    0.01 |     - |     - |     - |         - |
| DirectAccessReadOracleDecimalAsDecimal | .NET Core 2.1 |  10.245 ns |  0.5128 ns |  0.3392 ns |  0.10 |    0.01 |     - |     - |     - |         - |
|       TypeMapperReadOracleDecimalAsInt | .NET Core 2.1 |  15.569 ns |  0.8671 ns |  0.5735 ns |  0.15 |    0.01 |     - |     - |     - |         - |
|     DirectAccessReadOracleDecimalAsInt | .NET Core 2.1 |  14.544 ns |  1.0714 ns |  0.7087 ns |  0.14 |    0.01 |     - |     - |     - |         - |
|      TypeMapperReadOracleDecimalAsLong | .NET Core 2.1 |  15.198 ns |  1.0264 ns |  0.6789 ns |  0.14 |    0.01 |     - |     - |     - |         - |
|    DirectAccessReadOracleDecimalAsLong | .NET Core 2.1 |  13.814 ns |  0.4533 ns |  0.2698 ns |  0.13 |    0.00 |     - |     - |     - |         - |
|        TypeMapperReadOracleTimeStampTZ | .NET Core 3.1 |  90.381 ns | 11.2703 ns |  7.4546 ns |  0.85 |    0.08 |     - |     - |     - |         - |
|      DirectAccessReadOracleTimeStampTZ | .NET Core 3.1 |  81.777 ns |  3.8340 ns |  2.5360 ns |  0.77 |    0.02 |     - |     - |     - |         - |
|       TypeMapperReadOracleTimeStampLTZ | .NET Core 3.1 |  87.457 ns |  4.7244 ns |  2.4709 ns |  0.83 |    0.05 |     - |     - |     - |         - |
|     DirectAccessReadOracleTimeStampLTZ | .NET Core 3.1 |  83.846 ns |  5.6891 ns |  3.7630 ns |  0.79 |    0.05 |     - |     - |     - |         - |
|         TypeMapperReadOracleDecimalAdv | .NET Core 3.1 |  15.157 ns |  0.8687 ns |  0.5746 ns |  0.14 |    0.01 |     - |     - |     - |         - |
|       DirectAccessReadOracleDecimalAdv | .NET Core 3.1 |   9.401 ns |  0.5955 ns |  0.3939 ns |  0.09 |    0.00 |     - |     - |     - |         - |
|   TypeMapperReadOracleDecimalAsDecimal | .NET Core 3.1 |  10.639 ns |  0.6459 ns |  0.4272 ns |  0.10 |    0.00 |     - |     - |     - |         - |
| DirectAccessReadOracleDecimalAsDecimal | .NET Core 3.1 |   4.603 ns |  0.3031 ns |  0.2005 ns |  0.04 |    0.00 |     - |     - |     - |         - |
|       TypeMapperReadOracleDecimalAsInt | .NET Core 3.1 |  10.216 ns |  0.2060 ns |  0.0915 ns |  0.10 |    0.00 |     - |     - |     - |         - |
|     DirectAccessReadOracleDecimalAsInt | .NET Core 3.1 |   8.077 ns |  0.3439 ns |  0.2046 ns |  0.08 |    0.00 |     - |     - |     - |         - |
|      TypeMapperReadOracleDecimalAsLong | .NET Core 3.1 |  11.788 ns |  1.2965 ns |  0.8576 ns |  0.11 |    0.01 |     - |     - |     - |         - |
|    DirectAccessReadOracleDecimalAsLong | .NET Core 3.1 |   7.586 ns |  0.2214 ns |  0.1464 ns |  0.07 |    0.00 |     - |     - |     - |         - |
