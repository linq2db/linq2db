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
|              Method |       Runtime |          Mean |     Error |    StdDev |  Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------- |-------------- |--------------:|----------:|----------:|-------:|--------:|-------:|------:|------:|----------:|
|    TypeMapperString |    .NET 4.6.2 |    13.8224 ns | 0.3409 ns | 0.2255 ns |  13.27 |    0.37 |      - |     - |     - |         - |
|  DirectAccessString |    .NET 4.6.2 |     1.0442 ns | 0.0932 ns | 0.0414 ns |   1.00 |    0.00 |      - |     - |     - |         - |
|       TypeMapperInt |    .NET 4.6.2 |    14.3010 ns | 0.5072 ns | 0.3355 ns |  13.74 |    0.55 |      - |     - |     - |         - |
|     DirectAccessInt |    .NET 4.6.2 |     1.2577 ns | 0.1738 ns | 0.1150 ns |   1.16 |    0.11 |      - |     - |     - |         - |
|      TypeMapperLong |    .NET 4.6.2 |    14.6610 ns | 0.9504 ns | 0.6286 ns |  14.30 |    0.62 |      - |     - |     - |         - |
|    DirectAccessLong |    .NET 4.6.2 |     1.0777 ns | 0.0610 ns | 0.0404 ns |   1.03 |    0.06 |      - |     - |     - |         - |
|   TypeMapperBoolean |    .NET 4.6.2 |    14.1574 ns | 0.5757 ns | 0.3808 ns |  13.59 |    0.73 |      - |     - |     - |         - |
| DirectAccessBoolean |    .NET 4.6.2 |     1.0784 ns | 0.0159 ns | 0.0041 ns |   1.03 |    0.04 |      - |     - |     - |         - |
|   TypeMapperWrapper |    .NET 4.6.2 |    22.2686 ns | 2.0246 ns | 1.3392 ns |  21.44 |    0.87 |      - |     - |     - |         - |
| DirectAccessWrapper |    .NET 4.6.2 |     1.0639 ns | 0.1077 ns | 0.0563 ns |   1.01 |    0.05 |      - |     - |     - |         - |
|      TypeMapperEnum |    .NET 4.6.2 | 1,027.8619 ns | 9.2174 ns | 2.3937 ns | 984.93 |   41.91 | 0.0420 |     - |     - |     177 B |
|    DirectAccessEnum |    .NET 4.6.2 |     1.3551 ns | 0.0521 ns | 0.0135 ns |   1.30 |    0.05 |      - |     - |     - |         - |
|   TypeMapperVersion |    .NET 4.6.2 |    14.1643 ns | 0.4856 ns | 0.2890 ns |  13.51 |    0.54 |      - |     - |     - |         - |
| DirectAccessVersion |    .NET 4.6.2 |     0.8705 ns | 0.0821 ns | 0.0213 ns |   0.83 |    0.04 |      - |     - |     - |         - |
|    TypeMapperString | .NET Core 2.1 |     5.3554 ns | 0.2729 ns | 0.1805 ns |   5.10 |    0.08 |      - |     - |     - |         - |
|  DirectAccessString | .NET Core 2.1 |     1.3819 ns | 0.0407 ns | 0.0106 ns |   1.32 |    0.06 |      - |     - |     - |         - |
|       TypeMapperInt | .NET Core 2.1 |     5.3844 ns | 0.0569 ns | 0.0148 ns |   5.16 |    0.21 |      - |     - |     - |         - |
|     DirectAccessInt | .NET Core 2.1 |     1.3403 ns | 0.0613 ns | 0.0405 ns |   1.29 |    0.08 |      - |     - |     - |         - |
|      TypeMapperLong | .NET Core 2.1 |     5.6063 ns | 0.2219 ns | 0.1468 ns |   5.41 |    0.31 |      - |     - |     - |         - |
|    DirectAccessLong | .NET Core 2.1 |     1.3951 ns | 0.0416 ns | 0.0108 ns |   1.34 |    0.07 |      - |     - |     - |         - |
|   TypeMapperBoolean | .NET Core 2.1 |     5.4640 ns | 0.1428 ns | 0.0371 ns |   5.24 |    0.22 |      - |     - |     - |         - |
| DirectAccessBoolean | .NET Core 2.1 |     1.4621 ns | 0.0663 ns | 0.0347 ns |   1.40 |    0.05 |      - |     - |     - |         - |
|   TypeMapperWrapper | .NET Core 2.1 |    12.1789 ns | 0.2924 ns | 0.1740 ns |  11.71 |    0.59 |      - |     - |     - |         - |
| DirectAccessWrapper | .NET Core 2.1 |     1.3699 ns | 0.1019 ns | 0.0452 ns |   1.31 |    0.05 |      - |     - |     - |         - |
|      TypeMapperEnum | .NET Core 2.1 |   478.4662 ns | 8.6847 ns | 2.2554 ns | 458.41 |   17.42 | 0.0296 |     - |     - |     128 B |
|    DirectAccessEnum | .NET Core 2.1 |     1.3378 ns | 0.0608 ns | 0.0158 ns |   1.28 |    0.06 |      - |     - |     - |         - |
|   TypeMapperVersion | .NET Core 2.1 |     5.3713 ns | 0.6145 ns | 0.4064 ns |   5.28 |    0.33 |      - |     - |     - |         - |
| DirectAccessVersion | .NET Core 2.1 |     1.0287 ns | 0.1296 ns | 0.0337 ns |   0.99 |    0.05 |      - |     - |     - |         - |
|    TypeMapperString | .NET Core 3.1 |     5.2166 ns | 0.2285 ns | 0.1512 ns |   4.97 |    0.20 |      - |     - |     - |         - |
|  DirectAccessString | .NET Core 3.1 |     0.9935 ns | 0.1010 ns | 0.0528 ns |   0.96 |    0.03 |      - |     - |     - |         - |
|       TypeMapperInt | .NET Core 3.1 |     5.7110 ns | 0.4925 ns | 0.3258 ns |   5.63 |    0.43 |      - |     - |     - |         - |
|     DirectAccessInt | .NET Core 3.1 |     1.0145 ns | 0.0505 ns | 0.0224 ns |   0.97 |    0.03 |      - |     - |     - |         - |
|      TypeMapperLong | .NET Core 3.1 |     5.9914 ns | 0.2721 ns | 0.1800 ns |   5.73 |    0.18 |      - |     - |     - |         - |
|    DirectAccessLong | .NET Core 3.1 |     1.3926 ns | 0.0447 ns | 0.0116 ns |   1.33 |    0.05 |      - |     - |     - |         - |
|   TypeMapperBoolean | .NET Core 3.1 |     5.4006 ns | 0.3280 ns | 0.2170 ns |   5.10 |    0.31 |      - |     - |     - |         - |
| DirectAccessBoolean | .NET Core 3.1 |     1.1671 ns | 0.1298 ns | 0.0859 ns |   1.14 |    0.12 |      - |     - |     - |         - |
|   TypeMapperWrapper | .NET Core 3.1 |    11.3460 ns | 0.2775 ns | 0.1651 ns |  10.92 |    0.42 |      - |     - |     - |         - |
| DirectAccessWrapper | .NET Core 3.1 |     1.3841 ns | 0.0528 ns | 0.0137 ns |   1.33 |    0.06 |      - |     - |     - |         - |
|      TypeMapperEnum | .NET Core 3.1 |   190.2484 ns | 2.8136 ns | 1.0034 ns | 183.52 |    8.26 | 0.0114 |     - |     - |      48 B |
|    DirectAccessEnum | .NET Core 3.1 |     1.2081 ns | 0.0834 ns | 0.0551 ns |   1.18 |    0.05 |      - |     - |     - |         - |
|   TypeMapperVersion | .NET Core 3.1 |     5.7323 ns | 0.1312 ns | 0.0341 ns |   5.49 |    0.20 |      - |     - |     - |         - |
| DirectAccessVersion | .NET Core 3.1 |     1.3703 ns | 0.1003 ns | 0.0261 ns |   1.31 |    0.06 |      - |     - |     - |         - |
