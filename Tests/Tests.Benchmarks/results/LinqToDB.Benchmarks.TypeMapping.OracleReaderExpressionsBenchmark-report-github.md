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
|        TypeMapperReadOracleTimeStampTZ |    .NET 4.6.2 | 128.541 ns | 4.4063 ns | 2.9145 ns |  1.93 |    0.13 |     - |     - |     - |         - |
|      DirectAccessReadOracleTimeStampTZ |    .NET 4.6.2 |  66.792 ns | 7.2237 ns | 4.7781 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|       TypeMapperReadOracleTimeStampLTZ |    .NET 4.6.2 | 144.688 ns | 9.4044 ns | 6.2204 ns |  2.18 |    0.23 |     - |     - |     - |         - |
|     DirectAccessReadOracleTimeStampLTZ |    .NET 4.6.2 |  62.228 ns | 1.5967 ns | 1.0561 ns |  0.94 |    0.07 |     - |     - |     - |         - |
|         TypeMapperReadOracleDecimalAdv |    .NET 4.6.2 |  43.094 ns | 1.7340 ns | 1.1469 ns |  0.65 |    0.05 |     - |     - |     - |         - |
|       DirectAccessReadOracleDecimalAdv |    .NET 4.6.2 |   7.002 ns | 0.1679 ns | 0.1111 ns |  0.11 |    0.01 |     - |     - |     - |         - |
|   TypeMapperReadOracleDecimalAsDecimal |    .NET 4.6.2 |  27.102 ns | 1.2163 ns | 0.8045 ns |  0.41 |    0.03 |     - |     - |     - |         - |
| DirectAccessReadOracleDecimalAsDecimal |    .NET 4.6.2 |   6.539 ns | 0.5159 ns | 0.3412 ns |  0.10 |    0.01 |     - |     - |     - |         - |
|       TypeMapperReadOracleDecimalAsInt |    .NET 4.6.2 |  29.765 ns | 0.5886 ns | 0.3078 ns |  0.44 |    0.03 |     - |     - |     - |         - |
|     DirectAccessReadOracleDecimalAsInt |    .NET 4.6.2 |   8.900 ns | 0.2817 ns | 0.1863 ns |  0.13 |    0.01 |     - |     - |     - |         - |
|      TypeMapperReadOracleDecimalAsLong |    .NET 4.6.2 |  30.682 ns | 1.7581 ns | 1.1629 ns |  0.46 |    0.05 |     - |     - |     - |         - |
|    DirectAccessReadOracleDecimalAsLong |    .NET 4.6.2 |   9.572 ns | 0.2774 ns | 0.1835 ns |  0.14 |    0.01 |     - |     - |     - |         - |
|        TypeMapperReadOracleTimeStampTZ | .NET Core 2.1 |  67.485 ns | 1.9889 ns | 1.3155 ns |  1.01 |    0.07 |     - |     - |     - |         - |
|      DirectAccessReadOracleTimeStampTZ | .NET Core 2.1 |  69.668 ns | 8.5245 ns | 5.6385 ns |  1.05 |    0.13 |     - |     - |     - |         - |
|       TypeMapperReadOracleTimeStampLTZ | .NET Core 2.1 |  69.384 ns | 1.2103 ns | 0.5374 ns |  1.01 |    0.05 |     - |     - |     - |         - |
|     DirectAccessReadOracleTimeStampLTZ | .NET Core 2.1 |  65.001 ns | 2.4656 ns | 1.2896 ns |  0.95 |    0.06 |     - |     - |     - |         - |
|         TypeMapperReadOracleDecimalAdv | .NET Core 2.1 |  22.144 ns | 0.8263 ns | 0.5466 ns |  0.33 |    0.03 |     - |     - |     - |         - |
|       DirectAccessReadOracleDecimalAdv | .NET Core 2.1 |   9.935 ns | 0.4529 ns | 0.2996 ns |  0.15 |    0.01 |     - |     - |     - |         - |
|   TypeMapperReadOracleDecimalAsDecimal | .NET Core 2.1 |  10.724 ns | 0.2953 ns | 0.1757 ns |  0.16 |    0.01 |     - |     - |     - |         - |
| DirectAccessReadOracleDecimalAsDecimal | .NET Core 2.1 |   8.945 ns | 0.6195 ns | 0.4098 ns |  0.13 |    0.01 |     - |     - |     - |         - |
|       TypeMapperReadOracleDecimalAsInt | .NET Core 2.1 |  12.726 ns | 0.3317 ns | 0.2194 ns |  0.19 |    0.02 |     - |     - |     - |         - |
|     DirectAccessReadOracleDecimalAsInt | .NET Core 2.1 |  11.396 ns | 0.6076 ns | 0.4019 ns |  0.17 |    0.01 |     - |     - |     - |         - |
|      TypeMapperReadOracleDecimalAsLong | .NET Core 2.1 |  12.795 ns | 0.5170 ns | 0.3420 ns |  0.19 |    0.01 |     - |     - |     - |         - |
|    DirectAccessReadOracleDecimalAsLong | .NET Core 2.1 |  11.904 ns | 1.1594 ns | 0.7669 ns |  0.18 |    0.01 |     - |     - |     - |         - |
|        TypeMapperReadOracleTimeStampTZ | .NET Core 3.1 |  66.850 ns | 1.2254 ns | 0.8105 ns |  1.01 |    0.08 |     - |     - |     - |         - |
|      DirectAccessReadOracleTimeStampTZ | .NET Core 3.1 |  66.402 ns | 1.4941 ns | 0.9883 ns |  1.00 |    0.08 |     - |     - |     - |         - |
|       TypeMapperReadOracleTimeStampLTZ | .NET Core 3.1 |  70.249 ns | 1.3031 ns | 0.5786 ns |  1.02 |    0.06 |     - |     - |     - |         - |
|     DirectAccessReadOracleTimeStampLTZ | .NET Core 3.1 |  66.712 ns | 1.9771 ns | 1.1765 ns |  0.99 |    0.06 |     - |     - |     - |         - |
|         TypeMapperReadOracleDecimalAdv | .NET Core 3.1 |  12.123 ns | 0.2201 ns | 0.0977 ns |  0.18 |    0.01 |     - |     - |     - |         - |
|       DirectAccessReadOracleDecimalAdv | .NET Core 3.1 |   8.430 ns | 0.6027 ns | 0.3987 ns |  0.13 |    0.01 |     - |     - |     - |         - |
|   TypeMapperReadOracleDecimalAsDecimal | .NET Core 3.1 |   7.292 ns | 0.2392 ns | 0.1582 ns |  0.11 |    0.01 |     - |     - |     - |         - |
| DirectAccessReadOracleDecimalAsDecimal | .NET Core 3.1 |   3.672 ns | 0.1366 ns | 0.0904 ns |  0.06 |    0.00 |     - |     - |     - |         - |
|       TypeMapperReadOracleDecimalAsInt | .NET Core 3.1 |  10.065 ns | 0.3044 ns | 0.2013 ns |  0.15 |    0.01 |     - |     - |     - |         - |
|     DirectAccessReadOracleDecimalAsInt | .NET Core 3.1 |   7.005 ns | 0.3492 ns | 0.2310 ns |  0.11 |    0.01 |     - |     - |     - |         - |
|      TypeMapperReadOracleDecimalAsLong | .NET Core 3.1 |  10.238 ns | 1.0636 ns | 0.7035 ns |  0.15 |    0.01 |     - |     - |     - |         - |
|    DirectAccessReadOracleDecimalAsLong | .NET Core 3.1 |   6.981 ns | 0.2385 ns | 0.1578 ns |  0.11 |    0.01 |     - |     - |     - |         - |
