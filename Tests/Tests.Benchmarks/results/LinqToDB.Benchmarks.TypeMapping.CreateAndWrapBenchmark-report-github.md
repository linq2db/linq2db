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
|                TypeMapperParameterless |    .NET 4.6.2 |   117,555.747 ns |  5,490.0353 ns |  3,631.3184 ns |  20,112.20 |   706.55 |  1.4648 | 0.4883 |     - |    6382 B |
|              DirectAccessParameterless |    .NET 4.6.2 |         5.847 ns |      0.2164 ns |      0.1431 ns |       1.00 |     0.00 |  0.0153 |      - |     - |      64 B |
|           TypeMapperOneParameterString |    .NET 4.6.2 |   364,714.142 ns | 36,625.6425 ns | 24,225.5948 ns |  62,399.80 | 4,320.10 |  4.3945 | 0.9766 |     - |   18704 B |
|         DirectAccessOneParameterString |    .NET 4.6.2 |        11.443 ns |      0.4103 ns |      0.2442 ns |       1.96 |     0.07 |  0.0153 |      - |     - |      64 B |
|   TypeMapperOneParameterTimeSpanUnwrap |    .NET 4.6.2 |   358,696.202 ns |  9,948.3369 ns |  6,580.2089 ns |  61,379.06 | 1,948.01 |  4.3945 | 0.9766 |     - |   18704 B |
| DirectAccessOneParameterTimeSpanUnwrap |    .NET 4.6.2 |        11.726 ns |      0.2339 ns |      0.1392 ns |       2.01 |     0.06 |  0.0153 |      - |     - |      64 B |
|       TypeMapperTwoParametersIntString |    .NET 4.6.2 |   519,880.960 ns |  7,177.7834 ns |  2,559.6667 ns |  88,614.84 | 2,478.19 |  6.8359 | 1.9531 |     - |   29905 B |
|     DirectAccessTwoParametersIntString |    .NET 4.6.2 |        11.571 ns |      0.4325 ns |      0.2574 ns |       1.98 |     0.07 |  0.0153 |      - |     - |      64 B |
|    TypeMapperTwoParametersStringString |    .NET 4.6.2 |   525,564.249 ns |  9,888.6441 ns |  5,884.5735 ns |  89,930.61 | 2,400.67 |  6.8359 | 1.9531 |     - |   29905 B |
|  DirectAccessTwoParametersStringString |    .NET 4.6.2 |        11.658 ns |      0.3548 ns |      0.2347 ns |       1.99 |     0.05 |  0.0153 |      - |     - |      64 B |
|     TypeMapperTwoParametersWrapperEnum |    .NET 4.6.2 |   769,680.717 ns | 10,302.3289 ns |  2,675.4821 ns | 130,849.06 | 4,088.32 |  7.8125 | 0.9766 |     - |   33801 B |
|   DirectAccessTwoParametersWrapperEnum |    .NET 4.6.2 |         6.368 ns |      0.1918 ns |      0.0684 ns |       1.09 |     0.03 |  0.0153 |      - |     - |      64 B |
|   TypeMapperTwoParametersWrapperString |    .NET 4.6.2 |   562,685.221 ns | 10,829.4936 ns |  2,812.3851 ns |  95,643.96 | 2,333.31 |  6.8359 | 0.9766 |     - |   30097 B |
| DirectAccessTwoParametersWrapperString |    .NET 4.6.2 |        11.546 ns |      0.6801 ns |      0.1766 ns |       1.96 |     0.06 |  0.0153 |      - |     - |      64 B |
|              TypeMapperThreeParameters |    .NET 4.6.2 |   778,419.665 ns | 15,382.4933 ns |  8,045.3414 ns | 133,103.55 | 3,465.28 |  7.8125 | 0.9766 |     - |   34441 B |
|            DirectAccessThreeParameters |    .NET 4.6.2 |         6.501 ns |      0.4388 ns |      0.2902 ns |       1.11 |     0.06 |  0.0153 |      - |     - |      64 B |
|                  TypeMapperTSTZFactory |    .NET 4.6.2 | 1,189,825.434 ns | 59,449.4968 ns | 39,322.1612 ns | 203,589.86 | 8,244.08 | 15.6250 | 1.9531 |     - |   69441 B |
|                DirectAccessTSTZFactory |    .NET 4.6.2 |       269.994 ns |      5.1756 ns |      1.8457 ns |      46.02 |     1.21 |  0.0153 |      - |     - |      64 B |
|                TypeMapperParameterless | .NET Core 2.1 |    92,735.874 ns |  1,366.8521 ns |    354.9671 ns |  15,764.08 |   431.25 |  1.0986 | 0.4883 |     - |    5017 B |
|              DirectAccessParameterless | .NET Core 2.1 |         7.064 ns |      1.2212 ns |      0.8077 ns |       1.21 |     0.15 |  0.0152 |      - |     - |      64 B |
|           TypeMapperOneParameterString | .NET Core 2.1 |   204,195.003 ns |  3,238.9412 ns |  1,155.0376 ns |  34,807.38 | 1,056.82 |  1.4648 | 0.2441 |     - |    6457 B |
|         DirectAccessOneParameterString | .NET Core 2.1 |         8.282 ns |      0.5037 ns |      0.3332 ns |       1.42 |     0.06 |  0.0152 |      - |     - |      64 B |
|   TypeMapperOneParameterTimeSpanUnwrap | .NET Core 2.1 |   213,529.224 ns |  7,355.8003 ns |  4,865.4065 ns |  36,538.01 | 1,249.72 |  1.4648 | 0.2441 |     - |    6457 B |
| DirectAccessOneParameterTimeSpanUnwrap | .NET Core 2.1 |         8.030 ns |      0.2030 ns |      0.1062 ns |       1.37 |     0.04 |  0.0152 |      - |     - |      64 B |
|       TypeMapperTwoParametersIntString | .NET Core 2.1 |   292,067.500 ns | 23,335.0709 ns | 15,434.7046 ns |  49,977.69 | 2,931.73 |  0.9766 |      - |     - |    7105 B |
|     DirectAccessTwoParametersIntString | .NET Core 2.1 |         8.239 ns |      0.2510 ns |      0.1115 ns |       1.40 |     0.04 |  0.0152 |      - |     - |      64 B |
|    TypeMapperTwoParametersStringString | .NET Core 2.1 |   284,954.312 ns |  5,785.5998 ns |  3,826.8160 ns |  48,761.12 | 1,459.08 |  0.9766 |      - |     - |    7105 B |
|  DirectAccessTwoParametersStringString | .NET Core 2.1 |         8.076 ns |      0.2669 ns |      0.1765 ns |       1.38 |     0.04 |  0.0152 |      - |     - |      64 B |
|     TypeMapperTwoParametersWrapperEnum | .NET Core 2.1 |   488,048.456 ns |  9,053.1885 ns |  4,019.6718 ns |  82,999.88 | 2,494.68 |  1.4648 | 0.4883 |     - |   10378 B |
|   DirectAccessTwoParametersWrapperEnum | .NET Core 2.1 |         6.816 ns |      0.2043 ns |      0.1216 ns |       1.17 |     0.03 |  0.0152 |      - |     - |      64 B |
|   TypeMapperTwoParametersWrapperString | .NET Core 2.1 |   317,245.230 ns |  6,023.1136 ns |  2,147.9003 ns |  54,073.30 | 1,457.82 |  0.9766 | 0.4883 |     - |    7289 B |
| DirectAccessTwoParametersWrapperString | .NET Core 2.1 |         8.391 ns |      0.4111 ns |      0.2719 ns |       1.44 |     0.06 |  0.0152 |      - |     - |      64 B |
|              TypeMapperThreeParameters | .NET Core 2.1 |   478,276.363 ns |  8,716.8694 ns |  4,559.0912 ns |  81,783.59 | 2,204.95 |  1.4648 |      - |     - |   10970 B |
|            DirectAccessThreeParameters | .NET Core 2.1 |         7.902 ns |      1.3211 ns |      0.8738 ns |       1.35 |     0.14 |  0.0152 |      - |     - |      64 B |
|                  TypeMapperTSTZFactory | .NET Core 2.1 |   539,216.716 ns |  7,818.5312 ns |  2,788.1636 ns |  91,900.99 | 2,116.04 |  1.9531 |      - |     - |   14538 B |
|                DirectAccessTSTZFactory | .NET Core 2.1 |       272.009 ns |      3.9408 ns |      1.0234 ns |      46.24 |     1.18 |  0.0148 |      - |     - |      64 B |
|                TypeMapperParameterless | .NET Core 3.1 |    92,269.999 ns |  2,893.1686 ns |  1,913.6519 ns |  15,790.46 |   577.84 |  1.0986 | 0.4883 |     - |    4903 B |
|              DirectAccessParameterless | .NET Core 3.1 |         6.758 ns |      0.2362 ns |      0.1562 ns |       1.16 |     0.03 |  0.0153 |      - |     - |      64 B |
|           TypeMapperOneParameterString | .NET Core 3.1 |   197,077.808 ns |    456.2464 ns |    118.4857 ns |  33,501.73 |   940.49 |  1.4648 | 0.4883 |     - |    6328 B |
|         DirectAccessOneParameterString | .NET Core 3.1 |         6.927 ns |      0.4051 ns |      0.2119 ns |       1.18 |     0.05 |  0.0153 |      - |     - |      64 B |
|   TypeMapperOneParameterTimeSpanUnwrap | .NET Core 3.1 |   202,405.431 ns |  6,850.7589 ns |  1,779.1203 ns |  34,402.49 |   778.34 |  1.4648 | 0.4883 |     - |    6328 B |
| DirectAccessOneParameterTimeSpanUnwrap | .NET Core 3.1 |         7.039 ns |      0.3413 ns |      0.1785 ns |       1.20 |     0.03 |  0.0153 |      - |     - |      64 B |
|       TypeMapperTwoParametersIntString | .NET Core 3.1 |   282,841.385 ns | 13,759.0896 ns |  9,100.7859 ns |  48,402.56 | 2,101.70 |  0.9766 |      - |     - |    6971 B |
|     DirectAccessTwoParametersIntString | .NET Core 3.1 |         6.870 ns |      0.2217 ns |      0.0576 ns |       1.17 |     0.04 |  0.0153 |      - |     - |      64 B |
|    TypeMapperTwoParametersStringString | .NET Core 3.1 |   282,951.513 ns | 24,251.2438 ns | 16,040.6962 ns |  48,435.97 | 3,323.16 |  0.9766 |      - |     - |    6971 B |
|  DirectAccessTwoParametersStringString | .NET Core 3.1 |         6.717 ns |      0.2135 ns |      0.0555 ns |       1.14 |     0.04 |  0.0153 |      - |     - |      64 B |
|     TypeMapperTwoParametersWrapperEnum | .NET Core 3.1 |   463,416.427 ns |  8,412.6651 ns |  2,184.7424 ns |  78,774.55 | 2,120.23 |  1.4648 | 0.4883 |     - |   10392 B |
|   DirectAccessTwoParametersWrapperEnum | .NET Core 3.1 |         7.226 ns |      0.2164 ns |      0.1288 ns |       1.24 |     0.03 |  0.0153 |      - |     - |      64 B |
|   TypeMapperTwoParametersWrapperString | .NET Core 3.1 |   308,195.966 ns |  8,862.3257 ns |  5,273.8279 ns |  52,737.61 | 1,613.93 |  0.9766 | 0.4883 |     - |    7158 B |
| DirectAccessTwoParametersWrapperString | .NET Core 3.1 |         6.852 ns |      0.4886 ns |      0.2908 ns |       1.17 |     0.06 |  0.0153 |      - |     - |      64 B |
|              TypeMapperThreeParameters | .NET Core 3.1 |   463,587.040 ns | 11,571.4827 ns |  7,653.8194 ns |  79,323.64 | 2,300.46 |  0.9766 |      - |     - |   10959 B |
|            DirectAccessThreeParameters | .NET Core 3.1 |         6.910 ns |      0.4178 ns |      0.2763 ns |       1.18 |     0.06 |  0.0153 |      - |     - |      64 B |
|                  TypeMapperTSTZFactory | .NET Core 3.1 |   546,746.658 ns |  8,737.3883 ns |  3,115.8368 ns |  93,186.30 | 2,251.90 |  1.9531 |      - |     - |   14511 B |
|                DirectAccessTSTZFactory | .NET Core 3.1 |       273.296 ns |      7.4389 ns |      3.8907 ns |      46.73 |     1.27 |  0.0153 |      - |     - |      64 B |
