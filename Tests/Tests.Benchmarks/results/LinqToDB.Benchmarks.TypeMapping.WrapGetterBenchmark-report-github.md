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
|              Method |       Runtime |          Mean |      Error |     StdDev |  Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------- |-------------- |--------------:|-----------:|-----------:|-------:|--------:|-------:|------:|------:|----------:|
|    TypeMapperString |    .NET 4.6.2 |    13.8750 ns |  0.6128 ns |  0.3647 ns |  11.85 |    0.69 |      - |     - |     - |         - |
|  DirectAccessString |    .NET 4.6.2 |     1.1816 ns |  0.1087 ns |  0.0719 ns |   1.00 |    0.00 |      - |     - |     - |         - |
|       TypeMapperInt |    .NET 4.6.2 |    14.8257 ns |  0.9200 ns |  0.6085 ns |  12.59 |    1.01 |      - |     - |     - |         - |
|     DirectAccessInt |    .NET 4.6.2 |     1.1851 ns |  0.1552 ns |  0.1026 ns |   1.01 |    0.11 |      - |     - |     - |         - |
|      TypeMapperLong |    .NET 4.6.2 |    14.1721 ns |  0.6861 ns |  0.4538 ns |  12.03 |    0.67 |      - |     - |     - |         - |
|    DirectAccessLong |    .NET 4.6.2 |     1.0932 ns |  0.0588 ns |  0.0210 ns |   0.95 |    0.06 |      - |     - |     - |         - |
|   TypeMapperBoolean |    .NET 4.6.2 |    14.3591 ns |  0.2664 ns |  0.1585 ns |  12.27 |    0.81 |      - |     - |     - |         - |
| DirectAccessBoolean |    .NET 4.6.2 |     1.1888 ns |  0.0556 ns |  0.0144 ns |   1.03 |    0.07 |      - |     - |     - |         - |
|   TypeMapperWrapper |    .NET 4.6.2 |    20.4755 ns |  0.4698 ns |  0.2086 ns |  17.82 |    1.05 |      - |     - |     - |         - |
| DirectAccessWrapper |    .NET 4.6.2 |     1.0291 ns |  0.1049 ns |  0.0624 ns |   0.88 |    0.06 |      - |     - |     - |         - |
|      TypeMapperEnum |    .NET 4.6.2 | 1,043.6252 ns | 20.6760 ns | 12.3040 ns | 892.18 |   59.47 | 0.0420 |     - |     - |     177 B |
|    DirectAccessEnum |    .NET 4.6.2 |     1.1197 ns |  0.0518 ns |  0.0135 ns |   0.97 |    0.06 |      - |     - |     - |         - |
|   TypeMapperVersion |    .NET 4.6.2 |    15.1972 ns |  1.0231 ns |  0.6767 ns |  12.89 |    0.64 |      - |     - |     - |         - |
| DirectAccessVersion |    .NET 4.6.2 |     1.0200 ns |  0.1062 ns |  0.0632 ns |   0.87 |    0.07 |      - |     - |     - |         - |
|    TypeMapperString | .NET Core 2.1 |     5.2015 ns |  0.0429 ns |  0.0111 ns |   4.52 |    0.31 |      - |     - |     - |         - |
|  DirectAccessString | .NET Core 2.1 |     1.0113 ns |  0.1116 ns |  0.0738 ns |   0.86 |    0.09 |      - |     - |     - |         - |
|       TypeMapperInt | .NET Core 2.1 |     5.6434 ns |  0.3933 ns |  0.2601 ns |   4.79 |    0.28 |      - |     - |     - |         - |
|     DirectAccessInt | .NET Core 2.1 |     1.0659 ns |  0.0460 ns |  0.0164 ns |   0.93 |    0.06 |      - |     - |     - |         - |
|      TypeMapperLong | .NET Core 2.1 |     5.3354 ns |  0.1551 ns |  0.1026 ns |   4.53 |    0.24 |      - |     - |     - |         - |
|    DirectAccessLong | .NET Core 2.1 |     1.0864 ns |  0.0452 ns |  0.0161 ns |   0.95 |    0.06 |      - |     - |     - |         - |
|   TypeMapperBoolean | .NET Core 2.1 |     5.3584 ns |  0.1166 ns |  0.0303 ns |   4.65 |    0.32 |      - |     - |     - |         - |
| DirectAccessBoolean | .NET Core 2.1 |     0.9231 ns |  0.0554 ns |  0.0246 ns |   0.80 |    0.04 |      - |     - |     - |         - |
|   TypeMapperWrapper | .NET Core 2.1 |    12.2121 ns |  0.3448 ns |  0.2052 ns |  10.43 |    0.52 |      - |     - |     - |         - |
| DirectAccessWrapper | .NET Core 2.1 |     1.1372 ns |  0.1367 ns |  0.0904 ns |   0.96 |    0.09 |      - |     - |     - |         - |
|      TypeMapperEnum | .NET Core 2.1 |   545.6388 ns | 15.4351 ns | 10.2093 ns | 463.35 |   29.64 | 0.0296 |     - |     - |     128 B |
|    DirectAccessEnum | .NET Core 2.1 |     1.5094 ns |  0.1579 ns |  0.1044 ns |   1.28 |    0.11 |      - |     - |     - |         - |
|   TypeMapperVersion | .NET Core 2.1 |     5.3717 ns |  0.1947 ns |  0.1288 ns |   4.57 |    0.37 |      - |     - |     - |         - |
| DirectAccessVersion | .NET Core 2.1 |     1.0200 ns |  0.1227 ns |  0.0811 ns |   0.87 |    0.10 |      - |     - |     - |         - |
|    TypeMapperString | .NET Core 3.1 |     5.5237 ns |  0.1709 ns |  0.0894 ns |   4.76 |    0.33 |      - |     - |     - |         - |
|  DirectAccessString | .NET Core 3.1 |     1.4679 ns |  0.3826 ns |  0.2530 ns |   1.25 |    0.27 |      - |     - |     - |         - |
|       TypeMapperInt | .NET Core 3.1 |     5.7885 ns |  0.8730 ns |  0.5774 ns |   4.94 |    0.76 |      - |     - |     - |         - |
|     DirectAccessInt | .NET Core 3.1 |     1.0521 ns |  0.0676 ns |  0.0175 ns |   0.91 |    0.07 |      - |     - |     - |         - |
|      TypeMapperLong | .NET Core 3.1 |     6.1002 ns |  0.2238 ns |  0.1480 ns |   5.18 |    0.32 |      - |     - |     - |         - |
|    DirectAccessLong | .NET Core 3.1 |     1.5780 ns |  0.2239 ns |  0.1481 ns |   1.35 |    0.20 |      - |     - |     - |         - |
|   TypeMapperBoolean | .NET Core 3.1 |     5.4221 ns |  0.1321 ns |  0.0691 ns |   4.67 |    0.25 |      - |     - |     - |         - |
| DirectAccessBoolean | .NET Core 3.1 |     1.0832 ns |  0.0357 ns |  0.0093 ns |   0.94 |    0.07 |      - |     - |     - |         - |
|   TypeMapperWrapper | .NET Core 3.1 |    11.6892 ns |  0.3445 ns |  0.2279 ns |   9.92 |    0.55 |      - |     - |     - |         - |
| DirectAccessWrapper | .NET Core 3.1 |     1.5818 ns |  0.4345 ns |  0.2874 ns |   1.34 |    0.22 |      - |     - |     - |         - |
|      TypeMapperEnum | .NET Core 3.1 |   191.9637 ns |  6.2252 ns |  4.1176 ns | 163.07 |   11.46 | 0.0114 |     - |     - |      48 B |
|    DirectAccessEnum | .NET Core 3.1 |     1.3535 ns |  0.0490 ns |  0.0175 ns |   1.18 |    0.07 |      - |     - |     - |         - |
|   TypeMapperVersion | .NET Core 3.1 |     5.8034 ns |  0.2504 ns |  0.1656 ns |   4.93 |    0.29 |      - |     - |     - |         - |
| DirectAccessVersion | .NET Core 3.1 |     1.4195 ns |  0.1013 ns |  0.0450 ns |   1.23 |    0.04 |      - |     - |     - |         - |
