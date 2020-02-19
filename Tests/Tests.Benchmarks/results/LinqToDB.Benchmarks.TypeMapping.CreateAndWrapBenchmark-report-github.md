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
|                                 Method |       Runtime |             Mean |          Error |         StdDev |      Ratio |  RatioSD |   Gen 0 |  Gen 1 | Gen 2 | Allocated |
|--------------------------------------- |-------------- |-----------------:|---------------:|---------------:|-----------:|---------:|--------:|-------:|------:|----------:|
|                TypeMapperParameterless |    .NET 4.6.2 |   115,846.122 ns |  2,083.2186 ns |    742.8958 ns |  19,664.72 |   788.30 |  1.4648 | 0.4883 |     - |    6382 B |
|              DirectAccessParameterless |    .NET 4.6.2 |         5.853 ns |      0.3254 ns |      0.2153 ns |       1.00 |     0.00 |  0.0153 |      - |     - |      64 B |
|           TypeMapperOneParameterString |    .NET 4.6.2 |   383,138.757 ns | 33,958.2042 ns | 22,461.2495 ns |  65,485.71 | 3,605.40 |  4.3945 | 0.9766 |     - |   18704 B |
|         DirectAccessOneParameterString |    .NET 4.6.2 |        11.537 ns |      0.6338 ns |      0.4192 ns |       1.97 |     0.12 |  0.0153 |      - |     - |      64 B |
|   TypeMapperOneParameterTimeSpanUnwrap |    .NET 4.6.2 |   345,724.973 ns |  6,731.4660 ns |  1,748.1404 ns |  59,011.09 | 2,429.62 |  4.3945 | 0.9766 |     - |   18704 B |
| DirectAccessOneParameterTimeSpanUnwrap |    .NET 4.6.2 |        11.430 ns |      0.1913 ns |      0.0497 ns |       1.95 |     0.09 |  0.0153 |      - |     - |      64 B |
|       TypeMapperTwoParametersIntString |    .NET 4.6.2 |   506,048.399 ns |  9,137.4244 ns |  2,372.9601 ns |  86,381.33 | 3,687.96 |  6.8359 | 0.9766 |     - |   29905 B |
|     DirectAccessTwoParametersIntString |    .NET 4.6.2 |        11.202 ns |      0.1553 ns |      0.0403 ns |       1.91 |     0.08 |  0.0153 |      - |     - |      64 B |
|    TypeMapperTwoParametersStringString |    .NET 4.6.2 |   523,881.034 ns | 14,763.8224 ns |  9,765.3544 ns |  89,594.43 | 3,057.12 |  6.8359 | 1.9531 |     - |   29905 B |
|  DirectAccessTwoParametersStringString |    .NET 4.6.2 |        12.685 ns |      1.7303 ns |      1.1445 ns |       2.17 |     0.20 |  0.0153 |      - |     - |      64 B |
|     TypeMapperTwoParametersWrapperEnum |    .NET 4.6.2 |   770,341.048 ns | 19,022.1418 ns |  9,948.9480 ns | 130,893.83 | 5,487.05 |  7.8125 | 0.9766 |     - |   33809 B |
|   DirectAccessTwoParametersWrapperEnum |    .NET 4.6.2 |         6.424 ns |      0.1932 ns |      0.1010 ns |       1.09 |     0.05 |  0.0153 |      - |     - |      64 B |
|   TypeMapperTwoParametersWrapperString |    .NET 4.6.2 |   554,900.996 ns |  4,669.5113 ns |  1,212.6572 ns |  94,722.01 | 4,084.77 |  6.8359 | 0.9766 |     - |   30097 B |
| DirectAccessTwoParametersWrapperString |    .NET 4.6.2 |        11.487 ns |      0.2210 ns |      0.0981 ns |       1.95 |     0.07 |  0.0153 |      - |     - |      64 B |
|              TypeMapperThreeParameters |    .NET 4.6.2 |   772,894.509 ns | 21,788.2897 ns | 12,965.8617 ns | 131,400.48 | 4,874.50 |  7.8125 | 0.9766 |     - |   34441 B |
|            DirectAccessThreeParameters |    .NET 4.6.2 |         6.729 ns |      0.6365 ns |      0.4210 ns |       1.15 |     0.08 |  0.0153 |      - |     - |      64 B |
|                  TypeMapperTSTZFactory |    .NET 4.6.2 | 1,182,951.601 ns | 32,294.5763 ns |  8,386.7989 ns | 201,915.16 | 8,352.92 | 15.6250 | 1.9531 |     - |   69441 B |
|                DirectAccessTSTZFactory |    .NET 4.6.2 |       275.823 ns |      5.8735 ns |      3.8850 ns |      47.18 |     1.66 |  0.0153 |      - |     - |      64 B |
|                TypeMapperParameterless | .NET Core 2.1 |    92,019.200 ns |  1,338.2388 ns |    347.5364 ns |  15,706.43 |   638.77 |  1.0986 | 0.4883 |     - |    5551 B |
|              DirectAccessParameterless | .NET Core 2.1 |         6.252 ns |      0.4359 ns |      0.2883 ns |       1.07 |     0.07 |  0.0152 |      - |     - |      64 B |
|           TypeMapperOneParameterString | .NET Core 2.1 |   222,309.936 ns | 26,925.0599 ns | 17,809.2600 ns |  37,984.20 | 2,736.82 |  1.4648 | 0.2441 |     - |    6457 B |
|         DirectAccessOneParameterString | .NET Core 2.1 |         7.669 ns |      0.2193 ns |      0.0974 ns |       1.31 |     0.05 |  0.0152 |      - |     - |      64 B |
|   TypeMapperOneParameterTimeSpanUnwrap | .NET Core 2.1 |   206,088.889 ns |  3,846.9204 ns |    999.0330 ns |  35,186.09 | 1,704.75 |  1.4648 | 0.2441 |     - |    6457 B |
| DirectAccessOneParameterTimeSpanUnwrap | .NET Core 2.1 |         8.065 ns |      0.2409 ns |      0.1260 ns |       1.37 |     0.03 |  0.0152 |      - |     - |      64 B |
|       TypeMapperTwoParametersIntString | .NET Core 2.1 |   276,114.137 ns |  3,884.3091 ns |  1,008.7428 ns |  47,137.29 | 2,163.84 |  0.9766 |      - |     - |    7105 B |
|     DirectAccessTwoParametersIntString | .NET Core 2.1 |         9.247 ns |      1.5494 ns |      1.0248 ns |       1.58 |     0.14 |  0.0152 |      - |     - |      64 B |
|    TypeMapperTwoParametersStringString | .NET Core 2.1 |   297,453.887 ns |  4,298.2167 ns |  1,116.2332 ns |  50,776.80 | 2,225.89 |  0.9766 |      - |     - |    7105 B |
|  DirectAccessTwoParametersStringString | .NET Core 2.1 |         7.967 ns |      0.1891 ns |      0.0840 ns |       1.36 |     0.05 |  0.0152 |      - |     - |      64 B |
|     TypeMapperTwoParametersWrapperEnum | .NET Core 2.1 |   480,890.764 ns |  6,639.9181 ns |  1,724.3656 ns |  82,085.75 | 3,473.70 |  1.4648 | 0.4883 |     - |   10378 B |
|   DirectAccessTwoParametersWrapperEnum | .NET Core 2.1 |         6.736 ns |      0.1582 ns |      0.0564 ns |       1.14 |     0.05 |  0.0152 |      - |     - |      64 B |
|   TypeMapperTwoParametersWrapperString | .NET Core 2.1 |   319,381.433 ns | 11,908.2906 ns |  7,876.5969 ns |  54,602.26 | 1,413.65 |  0.9766 | 0.4883 |     - |    7289 B |
| DirectAccessTwoParametersWrapperString | .NET Core 2.1 |         8.059 ns |      0.8948 ns |      0.5918 ns |       1.38 |     0.11 |  0.0152 |      - |     - |      64 B |
|              TypeMapperThreeParameters | .NET Core 2.1 |   472,539.871 ns |  7,733.5158 ns |  2,757.8462 ns |  80,225.19 | 3,558.62 |  1.4648 |      - |     - |   10970 B |
|            DirectAccessThreeParameters | .NET Core 2.1 |         6.711 ns |      0.1834 ns |      0.0476 ns |       1.15 |     0.05 |  0.0152 |      - |     - |      64 B |
|                  TypeMapperTSTZFactory | .NET Core 2.1 |   529,421.893 ns |  8,573.2506 ns |  3,806.5764 ns |  90,109.14 | 3,428.17 |  1.9531 |      - |     - |   14538 B |
|                DirectAccessTSTZFactory | .NET Core 2.1 |       273.629 ns |      5.0478 ns |      2.2413 ns |      46.58 |     2.09 |  0.0148 |      - |     - |      64 B |
|                TypeMapperParameterless | .NET Core 3.1 |    90,046.366 ns |  3,290.2039 ns |  1,720.8402 ns |  15,300.44 |   682.43 |  1.0986 | 0.4883 |     - |    4904 B |
|              DirectAccessParameterless | .NET Core 3.1 |         7.265 ns |      0.6698 ns |      0.4431 ns |       1.24 |     0.08 |  0.0153 |      - |     - |      64 B |
|           TypeMapperOneParameterString | .NET Core 3.1 |   197,596.852 ns |  3,224.2257 ns |  1,149.7899 ns |  33,542.35 | 1,359.87 |  1.4648 | 0.4883 |     - |    6328 B |
|         DirectAccessOneParameterString | .NET Core 3.1 |         6.838 ns |      0.2226 ns |      0.0794 ns |       1.16 |     0.04 |  0.0153 |      - |     - |      64 B |
|   TypeMapperOneParameterTimeSpanUnwrap | .NET Core 3.1 |   199,066.643 ns |  1,561.5719 ns |    405.5353 ns |  33,980.37 | 1,450.65 |  1.4648 | 0.4883 |     - |    6328 B |
| DirectAccessOneParameterTimeSpanUnwrap | .NET Core 3.1 |         7.118 ns |      0.6063 ns |      0.4010 ns |       1.22 |     0.10 |  0.0153 |      - |     - |      64 B |
|       TypeMapperTwoParametersIntString | .NET Core 3.1 |   271,394.184 ns |  4,757.1989 ns |  2,488.1070 ns |  46,116.62 | 1,954.90 |  0.9766 | 0.4883 |     - |    6974 B |
|     DirectAccessTwoParametersIntString | .NET Core 3.1 |         6.846 ns |      0.1413 ns |      0.0367 ns |       1.17 |     0.05 |  0.0153 |      - |     - |      64 B |
|    TypeMapperTwoParametersStringString | .NET Core 3.1 |   267,318.861 ns |  5,116.1381 ns |  1,328.6448 ns |  45,625.74 | 1,797.77 |  0.9766 | 0.4883 |     - |    6979 B |
|  DirectAccessTwoParametersStringString | .NET Core 3.1 |         7.078 ns |      0.1756 ns |      0.0456 ns |       1.21 |     0.06 |  0.0153 |      - |     - |      64 B |
|     TypeMapperTwoParametersWrapperEnum | .NET Core 3.1 |   455,637.186 ns |  5,256.2224 ns |  1,365.0243 ns |  77,783.99 | 3,544.60 |  1.4648 | 0.4883 |     - |   10392 B |
|   DirectAccessTwoParametersWrapperEnum | .NET Core 3.1 |         6.768 ns |      0.2011 ns |      0.1052 ns |       1.15 |     0.05 |  0.0153 |      - |     - |      64 B |
|   TypeMapperTwoParametersWrapperString | .NET Core 3.1 |   310,129.102 ns | 12,537.7973 ns |  7,461.0420 ns |  52,734.47 | 2,407.11 |  0.9766 | 0.4883 |     - |    7158 B |
| DirectAccessTwoParametersWrapperString | .NET Core 3.1 |         6.953 ns |      0.1937 ns |      0.0503 ns |       1.19 |     0.05 |  0.0153 |      - |     - |      64 B |
|              TypeMapperThreeParameters | .NET Core 3.1 |   450,658.968 ns |  4,388.0526 ns |  1,139.5633 ns |  76,929.84 | 3,382.28 |  1.4648 |      - |     - |   10998 B |
|            DirectAccessThreeParameters | .NET Core 3.1 |         7.010 ns |      0.2776 ns |      0.1836 ns |       1.20 |     0.07 |  0.0153 |      - |     - |      64 B |
|                  TypeMapperTSTZFactory | .NET Core 3.1 |   542,568.919 ns |  4,053.3159 ns |  1,052.6333 ns |  92,619.10 | 4,055.42 |  1.9531 |      - |     - |   14511 B |
|                DirectAccessTSTZFactory | .NET Core 3.1 |       276.094 ns |     11.3853 ns |      7.5307 ns |      47.24 |     2.34 |  0.0153 |      - |     - |      64 B |
