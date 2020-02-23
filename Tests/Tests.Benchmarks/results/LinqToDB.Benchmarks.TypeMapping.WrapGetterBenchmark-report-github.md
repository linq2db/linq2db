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
|              Method |       Runtime |       Mean |      Error |    StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------- |-------------- |-----------:|-----------:|----------:|------:|--------:|-------:|------:|------:|----------:|
|    TypeMapperString |    .NET 4.6.2 | 15.3479 ns |  2.7478 ns | 0.7136 ns | 12.25 |    3.22 |      - |     - |     - |         - |
|  DirectAccessString |    .NET 4.6.2 |  1.3166 ns |  1.1818 ns | 0.3069 ns |  1.00 |    0.00 |      - |     - |     - |         - |
|       TypeMapperInt |    .NET 4.6.2 | 13.8341 ns |  0.5437 ns | 0.1412 ns | 11.02 |    2.74 |      - |     - |     - |         - |
|     DirectAccessInt |    .NET 4.6.2 |  1.3526 ns |  0.0388 ns | 0.0101 ns |  1.08 |    0.26 |      - |     - |     - |         - |
|      TypeMapperLong |    .NET 4.6.2 | 14.0896 ns |  0.8237 ns | 0.2139 ns | 11.21 |    2.76 |      - |     - |     - |         - |
|    DirectAccessLong |    .NET 4.6.2 |  1.4131 ns |  0.1988 ns | 0.0516 ns |  1.12 |    0.26 |      - |     - |     - |         - |
|   TypeMapperBoolean |    .NET 4.6.2 | 15.7012 ns |  5.2449 ns | 1.3621 ns | 12.64 |    3.99 |      - |     - |     - |         - |
| DirectAccessBoolean |    .NET 4.6.2 |  1.3107 ns |  0.7500 ns | 0.1948 ns |  1.06 |    0.38 |      - |     - |     - |         - |
|   TypeMapperWrapper |    .NET 4.6.2 | 23.4583 ns |  6.5086 ns | 1.6903 ns | 18.58 |    4.38 |      - |     - |     - |         - |
| DirectAccessWrapper |    .NET 4.6.2 |  1.1769 ns |  0.1940 ns | 0.0504 ns |  0.93 |    0.20 |      - |     - |     - |         - |
|      TypeMapperEnum |    .NET 4.6.2 | 48.2725 ns |  4.2875 ns | 1.1135 ns | 38.49 |    9.81 | 0.0057 |     - |     - |      24 B |
|    DirectAccessEnum |    .NET 4.6.2 |  1.1147 ns |  0.0962 ns | 0.0250 ns |  0.89 |    0.21 |      - |     - |     - |         - |
|   TypeMapperVersion |    .NET 4.6.2 | 13.2734 ns |  0.3918 ns | 0.1017 ns | 10.59 |    2.71 |      - |     - |     - |         - |
| DirectAccessVersion |    .NET 4.6.2 |  1.4912 ns |  0.1621 ns | 0.0421 ns |  1.18 |    0.27 |      - |     - |     - |         - |
|    TypeMapperString | .NET Core 2.1 |  5.5361 ns |  1.2538 ns | 0.3256 ns |  4.39 |    0.99 |      - |     - |     - |         - |
|  DirectAccessString | .NET Core 2.1 |  0.5012 ns |  0.6453 ns | 0.1676 ns |  0.38 |    0.11 |      - |     - |     - |         - |
|       TypeMapperInt | .NET Core 2.1 |  5.9537 ns |  1.7067 ns | 0.4432 ns |  4.71 |    1.04 |      - |     - |     - |         - |
|     DirectAccessInt | .NET Core 2.1 |  1.0811 ns |  0.5640 ns | 0.1465 ns |  0.85 |    0.16 |      - |     - |     - |         - |
|      TypeMapperLong | .NET Core 2.1 |  6.8012 ns |  2.7474 ns | 0.7135 ns |  5.53 |    1.98 |      - |     - |     - |         - |
|    DirectAccessLong | .NET Core 2.1 |  1.0612 ns |  0.1691 ns | 0.0439 ns |  0.84 |    0.21 |      - |     - |     - |         - |
|   TypeMapperBoolean | .NET Core 2.1 |  5.3466 ns |  1.3147 ns | 0.3414 ns |  4.23 |    0.92 |      - |     - |     - |         - |
| DirectAccessBoolean | .NET Core 2.1 |  1.0948 ns |  0.2774 ns | 0.0720 ns |  0.88 |    0.26 |      - |     - |     - |         - |
|   TypeMapperWrapper | .NET Core 2.1 | 11.9773 ns |  0.5043 ns | 0.1310 ns |  9.53 |    2.34 |      - |     - |     - |         - |
| DirectAccessWrapper | .NET Core 2.1 |  1.1047 ns |  0.2050 ns | 0.0532 ns |  0.88 |    0.22 |      - |     - |     - |         - |
|      TypeMapperEnum | .NET Core 2.1 | 38.6566 ns |  8.5132 ns | 2.2109 ns | 30.72 |    7.47 | 0.0057 |     - |     - |      24 B |
|    DirectAccessEnum | .NET Core 2.1 |  1.1315 ns |  0.0889 ns | 0.0231 ns |  0.90 |    0.23 |      - |     - |     - |         - |
|   TypeMapperVersion | .NET Core 2.1 |  6.6360 ns |  1.4445 ns | 0.3751 ns |  5.23 |    1.05 |      - |     - |     - |         - |
| DirectAccessVersion | .NET Core 2.1 |  0.7474 ns |  0.9915 ns | 0.2575 ns |  0.59 |    0.24 |      - |     - |     - |         - |
|    TypeMapperString | .NET Core 3.1 |  6.2129 ns |  2.2133 ns | 0.5748 ns |  4.93 |    1.18 |      - |     - |     - |         - |
|  DirectAccessString | .NET Core 3.1 |  1.1565 ns |  0.7506 ns | 0.1949 ns |  0.92 |    0.25 |      - |     - |     - |         - |
|       TypeMapperInt | .NET Core 3.1 |  6.5934 ns |  1.0197 ns | 0.2648 ns |  5.27 |    1.43 |      - |     - |     - |         - |
|     DirectAccessInt | .NET Core 3.1 |  1.4367 ns |  0.3659 ns | 0.0950 ns |  1.16 |    0.35 |      - |     - |     - |         - |
|      TypeMapperLong | .NET Core 3.1 |  5.6112 ns |  3.6916 ns | 0.9587 ns |  4.61 |    1.92 |      - |     - |     - |         - |
|    DirectAccessLong | .NET Core 3.1 |  1.3728 ns |  0.9424 ns | 0.2447 ns |  1.12 |    0.42 |      - |     - |     - |         - |
|   TypeMapperBoolean | .NET Core 3.1 |  5.9453 ns |  0.4840 ns | 0.1257 ns |  4.75 |    1.26 |      - |     - |     - |         - |
| DirectAccessBoolean | .NET Core 3.1 |  1.4241 ns |  0.4051 ns | 0.1052 ns |  1.13 |    0.27 |      - |     - |     - |         - |
|   TypeMapperWrapper | .NET Core 3.1 | 13.3349 ns |  4.2970 ns | 1.1159 ns | 10.79 |    3.54 |      - |     - |     - |         - |
| DirectAccessWrapper | .NET Core 3.1 |  1.2526 ns |  0.9796 ns | 0.2544 ns |  0.97 |    0.17 |      - |     - |     - |         - |
|      TypeMapperEnum | .NET Core 3.1 | 37.9145 ns | 19.2618 ns | 5.0022 ns | 29.75 |    6.04 | 0.0057 |     - |     - |      24 B |
|    DirectAccessEnum | .NET Core 3.1 |  1.5054 ns |  0.6556 ns | 0.1703 ns |  1.22 |    0.43 |      - |     - |     - |         - |
|   TypeMapperVersion | .NET Core 3.1 |  5.6706 ns |  1.5663 ns | 0.4068 ns |  4.51 |    1.12 |      - |     - |     - |         - |
| DirectAccessVersion | .NET Core 3.1 |  1.2854 ns |  0.8991 ns | 0.2335 ns |  1.01 |    0.25 |      - |     - |     - |         - |
