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
|              Method |       Runtime |          Mean |         Error |        StdDev |     Ratio |  RatioSD |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|-------------------- |-------------- |--------------:|--------------:|--------------:|----------:|---------:|-------:|-------:|------:|----------:|
|    TypeMapperString |    .NET 4.6.2 | 21,360.607 ns | 1,085.1027 ns |   717.7282 ns | 14,067.09 | 1,117.98 | 0.4578 | 0.1221 |     - |    2865 B |
|  DirectAccessString |    .NET 4.6.2 |      1.530 ns |     0.2423 ns |     0.1603 ns |      1.00 |     0.00 |      - |      - |     - |         - |
|       TypeMapperInt |    .NET 4.6.2 | 21,529.481 ns | 2,159.8248 ns | 1,428.5904 ns | 14,275.53 | 2,247.05 | 0.4578 | 0.1221 |     - |    2889 B |
|     DirectAccessInt |    .NET 4.6.2 |      1.528 ns |     0.2962 ns |     0.1959 ns |      1.01 |     0.17 |      - |      - |     - |         - |
|      TypeMapperLong |    .NET 4.6.2 | 18,389.222 ns | 1,379.7528 ns |   912.6210 ns | 12,146.68 | 1,466.57 | 0.4578 | 0.1221 |     - |    2889 B |
|    DirectAccessLong |    .NET 4.6.2 |      1.411 ns |     0.1014 ns |     0.0603 ns |      0.91 |     0.11 |      - |      - |     - |         - |
|   TypeMapperBoolean |    .NET 4.6.2 | 17,586.953 ns | 1,602.8545 ns | 1,060.1890 ns | 11,623.46 | 1,515.70 | 0.4578 | 0.1221 |     - |    2889 B |
| DirectAccessBoolean |    .NET 4.6.2 |      1.627 ns |     0.0972 ns |     0.0643 ns |      1.07 |     0.11 |      - |      - |     - |         - |
|   TypeMapperWrapper |    .NET 4.6.2 | 15,786.221 ns |   765.1114 ns |   400.1681 ns | 10,200.66 | 1,035.05 | 0.4272 | 0.1221 |     - |    2729 B |
| DirectAccessWrapper |    .NET 4.6.2 |      1.516 ns |     0.3004 ns |     0.1987 ns |      1.00 |     0.14 |      - |      - |     - |         - |
|      TypeMapperEnum |    .NET 4.6.2 | 16,809.118 ns |   803.1637 ns |   477.9498 ns | 10,882.43 | 1,210.29 | 0.4272 | 0.1221 |     - |    2753 B |
|    DirectAccessEnum |    .NET 4.6.2 |      1.190 ns |     0.1161 ns |     0.0768 ns |      0.78 |     0.07 |      - |      - |     - |         - |
|   TypeMapperVersion |    .NET 4.6.2 | 16,655.051 ns | 1,315.4528 ns |   870.0906 ns | 10,986.93 | 1,229.34 | 0.4578 | 0.1221 |     - |    2865 B |
| DirectAccessVersion |    .NET 4.6.2 |      1.308 ns |     0.2270 ns |     0.1501 ns |      0.87 |     0.16 |      - |      - |     - |         - |
|    TypeMapperString | .NET Core 2.1 | 12,822.095 ns |   814.9450 ns |   539.0356 ns |  8,455.38 |   837.40 | 0.2899 | 0.1068 |     - |    1848 B |
|  DirectAccessString | .NET Core 2.1 |      2.154 ns |     0.3674 ns |     0.2430 ns |      1.41 |     0.15 |      - |      - |     - |         - |
|       TypeMapperInt | .NET Core 2.1 | 16,983.749 ns | 4,032.0435 ns | 2,666.9472 ns | 11,123.40 | 1,513.32 | 0.2899 | 0.1068 |     - |    1872 B |
|     DirectAccessInt | .NET Core 2.1 |      1.363 ns |     0.0771 ns |     0.0510 ns |      0.90 |     0.12 |      - |      - |     - |         - |
|      TypeMapperLong | .NET Core 2.1 | 17,194.864 ns |   941.5272 ns |   560.2877 ns | 11,119.77 | 1,127.09 | 0.3052 | 0.0916 |     - |    1872 B |
|    DirectAccessLong | .NET Core 2.1 |      1.380 ns |     0.1947 ns |     0.1288 ns |      0.91 |     0.10 |      - |      - |     - |         - |
|   TypeMapperBoolean | .NET Core 2.1 | 16,545.462 ns | 1,762.0650 ns | 1,165.4969 ns | 10,924.94 | 1,384.76 | 0.3052 | 0.0916 |     - |    1872 B |
| DirectAccessBoolean | .NET Core 2.1 |      1.411 ns |     0.1136 ns |     0.0676 ns |      0.91 |     0.11 |      - |      - |     - |         - |
|   TypeMapperWrapper | .NET Core 2.1 | 13,999.786 ns | 2,147.3504 ns | 1,277.8538 ns |  9,075.98 | 1,369.63 | 0.2747 | 0.0916 |     - |    1848 B |
| DirectAccessWrapper | .NET Core 2.1 |      1.341 ns |     0.2255 ns |     0.1342 ns |      0.87 |     0.14 |      - |      - |     - |         - |
|      TypeMapperEnum | .NET Core 2.1 | 15,915.483 ns | 2,651.7348 ns | 1,753.9584 ns | 10,483.66 | 1,383.96 | 0.3052 | 0.0916 |     - |    1872 B |
|    DirectAccessEnum | .NET Core 2.1 |      1.609 ns |     0.1575 ns |     0.0937 ns |      1.04 |     0.12 |      - |      - |     - |         - |
|   TypeMapperVersion | .NET Core 2.1 | 12,497.666 ns | 1,521.4424 ns |   905.3860 ns |  8,071.09 |   826.81 | 0.2747 | 0.0916 |     - |    1848 B |
| DirectAccessVersion | .NET Core 2.1 |      1.368 ns |     0.2680 ns |     0.1773 ns |      0.90 |     0.13 |      - |      - |     - |         - |
|    TypeMapperString | .NET Core 3.1 |  8,707.281 ns |   683.3310 ns |   451.9812 ns |  5,759.90 |   780.94 | 0.2899 | 0.0763 |     - |    1816 B |
|  DirectAccessString | .NET Core 3.1 |      1.525 ns |     0.2070 ns |     0.1369 ns |      1.01 |     0.12 |      - |      - |     - |         - |
|       TypeMapperInt | .NET Core 3.1 |  8,785.617 ns |   857.1338 ns |   566.9409 ns |  5,801.68 |   728.72 | 0.2899 | 0.0763 |     - |    1840 B |
|     DirectAccessInt | .NET Core 3.1 |      1.281 ns |     0.1375 ns |     0.0910 ns |      0.85 |     0.13 |      - |      - |     - |         - |
|      TypeMapperLong | .NET Core 3.1 |  9,020.747 ns |   537.2682 ns |   355.3697 ns |  5,952.32 |   623.39 | 0.2899 | 0.0763 |     - |    1840 B |
|    DirectAccessLong | .NET Core 3.1 |      1.574 ns |     0.1208 ns |     0.0799 ns |      1.04 |     0.13 |      - |      - |     - |         - |
|   TypeMapperBoolean | .NET Core 3.1 |  9,691.427 ns | 2,413.2640 ns | 1,596.2247 ns |  6,402.21 | 1,340.67 | 0.2899 | 0.0763 |     - |    1840 B |
| DirectAccessBoolean | .NET Core 3.1 |      1.117 ns |     0.2046 ns |     0.1217 ns |      0.73 |     0.13 |      - |      - |     - |         - |
|   TypeMapperWrapper | .NET Core 3.1 | 10,759.606 ns |   700.9624 ns |   463.6432 ns |  7,096.33 |   728.16 | 0.2899 | 0.0763 |     - |    1816 B |
| DirectAccessWrapper | .NET Core 3.1 |      1.364 ns |     0.1014 ns |     0.0263 ns |      0.93 |     0.07 |      - |      - |     - |         - |
|      TypeMapperEnum | .NET Core 3.1 | 12,134.551 ns | 1,309.6620 ns |   866.2603 ns |  8,001.86 |   917.10 | 0.2899 | 0.0763 |     - |    1840 B |
|    DirectAccessEnum | .NET Core 3.1 |      1.744 ns |     0.1758 ns |     0.1163 ns |      1.15 |     0.15 |      - |      - |     - |         - |
|   TypeMapperVersion | .NET Core 3.1 | 10,023.494 ns | 2,028.5768 ns | 1,341.7780 ns |  6,639.14 | 1,230.20 | 0.2899 | 0.0763 |     - |    1816 B |
| DirectAccessVersion | .NET Core 3.1 |      1.494 ns |     0.2342 ns |     0.1549 ns |      0.99 |     0.17 |      - |      - |     - |         - |
