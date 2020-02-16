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
|                                 Method |       Runtime |             Mean |             Error |          StdDev |      Ratio |   RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------------------------------------- |-------------- |-----------------:|------------------:|----------------:|-----------:|----------:|-------:|------:|------:|----------:|
|                TypeMapperParameterless |    .NET 4.6.2 | 2,432,160.114 ns |   807,644.6139 ns | 480,616.3550 ns | 231,520.83 | 49,348.90 |      - |     - |     - |   73728 B |
|              DirectAccessParameterless |    .NET 4.6.2 |        10.465 ns |         1.0597 ns |       0.7009 ns |       1.00 |      0.00 | 0.0172 |     - |     - |      72 B |
|           TypeMapperOneParameterString |    .NET 4.6.2 | 3,212,204.816 ns | 1,497,296.0040 ns | 990,368.5999 ns | 306,907.82 | 94,540.59 |      - |     - |     - |   90112 B |
|         DirectAccessOneParameterString |    .NET 4.6.2 |        17.931 ns |         1.4522 ns |       0.9606 ns |       1.72 |      0.19 | 0.0172 |     - |     - |      72 B |
|   TypeMapperOneParameterTimeSpanUnwrap |    .NET 4.6.2 | 2,957,113.747 ns | 1,070,382.9102 ns | 707,992.0212 ns | 285,781.24 | 80,274.04 |      - |     - |     - |   90112 B |
| DirectAccessOneParameterTimeSpanUnwrap |    .NET 4.6.2 |        17.797 ns |         0.9943 ns |       0.6577 ns |       1.71 |      0.13 | 0.0172 |     - |     - |      72 B |
|       TypeMapperTwoParametersIntString |    .NET 4.6.2 | 2,682,713.110 ns |   538,751.9590 ns | 281,777.6918 ns | 255,211.22 | 37,200.65 |      - |     - |     - |   98304 B |
|     DirectAccessTwoParametersIntString |    .NET 4.6.2 |        18.155 ns |         0.8446 ns |       0.5586 ns |       1.74 |      0.10 | 0.0172 |     - |     - |      72 B |
|    TypeMapperTwoParametersStringString |    .NET 4.6.2 | 3,898,425.832 ns |   992,272.7780 ns | 656,327.0050 ns | 374,416.90 | 68,752.84 |      - |     - |     - |   98304 B |
|  DirectAccessTwoParametersStringString |    .NET 4.6.2 |        17.801 ns |         0.7069 ns |       0.4676 ns |       1.71 |      0.11 | 0.0172 |     - |     - |      72 B |
|     TypeMapperTwoParametersWrapperEnum |    .NET 4.6.2 | 3,699,069.192 ns | 1,267,234.9889 ns | 838,197.4829 ns | 356,640.53 | 90,946.35 |      - |     - |     - |  106496 B |
|   DirectAccessTwoParametersWrapperEnum |    .NET 4.6.2 |        10.929 ns |         0.8475 ns |       0.5606 ns |       1.05 |      0.09 | 0.0172 |     - |     - |      72 B |
|   TypeMapperTwoParametersWrapperString |    .NET 4.6.2 | 3,287,775.436 ns | 1,016,268.3983 ns | 672,198.6222 ns | 313,715.34 | 58,167.62 |      - |     - |     - |   98304 B |
| DirectAccessTwoParametersWrapperString |    .NET 4.6.2 |        18.413 ns |         1.2037 ns |       0.7962 ns |       1.77 |      0.13 | 0.0172 |     - |     - |      72 B |
|              TypeMapperThreeParameters |    .NET 4.6.2 | 3,301,838.262 ns | 1,284,737.0637 ns | 764,526.4192 ns | 314,978.16 | 81,364.29 |      - |     - |     - |  106496 B |
|            DirectAccessThreeParameters |    .NET 4.6.2 |        11.211 ns |         0.6053 ns |       0.3602 ns |       1.06 |      0.06 | 0.0172 |     - |     - |      72 B |
|                  TypeMapperTSTZFactory |    .NET 4.6.2 | 3,781,047.076 ns |   977,906.4510 ns | 646,824.5692 ns | 363,803.53 | 73,702.68 |      - |     - |     - |  139264 B |
|                DirectAccessTSTZFactory |    .NET 4.6.2 |       421.819 ns |        42.8682 ns |      28.3547 ns |      40.46 |      3.77 | 0.0172 |     - |     - |      72 B |
|                TypeMapperParameterless | .NET Core 2.1 | 1,102,927.872 ns |    95,302.9165 ns |  63,036.9785 ns | 105,856.54 |  9,889.32 | 1.9531 |     - |     - |   22406 B |
|              DirectAccessParameterless | .NET Core 2.1 |         9.806 ns |         1.0202 ns |       0.6748 ns |       0.94 |      0.10 | 0.0171 |     - |     - |      72 B |
|           TypeMapperOneParameterString | .NET Core 2.1 | 1,260,251.578 ns |    91,724.6607 ns |  60,670.1838 ns | 120,884.67 |  9,599.23 | 1.9531 |     - |     - |   23750 B |
|         DirectAccessOneParameterString | .NET Core 2.1 |        13.525 ns |         1.0981 ns |       0.7263 ns |       1.30 |      0.10 | 0.0171 |     - |     - |      72 B |
|   TypeMapperOneParameterTimeSpanUnwrap | .NET Core 2.1 | 1,253,126.033 ns |    58,358.3079 ns |  38,600.4074 ns | 120,264.53 |  9,535.02 | 1.9531 |     - |     - |   23598 B |
| DirectAccessOneParameterTimeSpanUnwrap | .NET Core 2.1 |        12.671 ns |         1.1718 ns |       0.7751 ns |       1.22 |      0.12 | 0.0171 |     - |     - |      72 B |
|       TypeMapperTwoParametersIntString | .NET Core 2.1 | 1,339,744.739 ns |    79,566.0495 ns |  52,628.0154 ns | 128,403.18 |  7,820.58 | 3.9063 |     - |     - |   24783 B |
|     DirectAccessTwoParametersIntString | .NET Core 2.1 |        12.464 ns |         1.0628 ns |       0.7030 ns |       1.20 |      0.12 | 0.0171 |     - |     - |      72 B |
|    TypeMapperTwoParametersStringString | .NET Core 2.1 | 1,292,346.818 ns |    89,238.3650 ns |  59,025.6532 ns | 124,094.13 | 11,656.85 | 1.9531 |     - |     - |   24406 B |
|  DirectAccessTwoParametersStringString | .NET Core 2.1 |        12.078 ns |         1.0478 ns |       0.6931 ns |       1.16 |      0.06 | 0.0171 |     - |     - |      72 B |
|     TypeMapperTwoParametersWrapperEnum | .NET Core 2.1 | 1,621,750.859 ns |   198,675.8708 ns | 131,411.7873 ns | 155,763.09 | 17,683.46 | 3.9063 |     - |     - |   27917 B |
|   DirectAccessTwoParametersWrapperEnum | .NET Core 2.1 |        10.615 ns |         1.0527 ns |       0.6265 ns |       1.01 |      0.08 | 0.0171 |     - |     - |      72 B |
|   TypeMapperTwoParametersWrapperString | .NET Core 2.1 | 1,402,560.771 ns |    94,580.5814 ns |  62,559.1985 ns | 134,552.81 | 10,765.26 | 3.9063 |     - |     - |   24968 B |
| DirectAccessTwoParametersWrapperString | .NET Core 2.1 |        11.127 ns |         1.8899 ns |       1.2500 ns |       1.07 |      0.14 | 0.0171 |     - |     - |      72 B |
|              TypeMapperThreeParameters | .NET Core 2.1 | 1,434,382.641 ns |    54,781.3555 ns |  36,234.4748 ns | 137,676.57 | 10,866.95 | 3.9063 |     - |     - |   28398 B |
|            DirectAccessThreeParameters | .NET Core 2.1 |        10.156 ns |         1.6619 ns |       1.0993 ns |       0.97 |      0.11 | 0.0171 |     - |     - |      72 B |
|                  TypeMapperTSTZFactory | .NET Core 2.1 | 1,535,052.611 ns |    72,548.6951 ns |  47,986.4699 ns | 147,261.70 | 10,865.10 | 3.9063 |     - |     - |   31681 B |
|                DirectAccessTSTZFactory | .NET Core 2.1 |       327.352 ns |        10.0231 ns |       5.9646 ns |      31.07 |      1.87 | 0.0167 |     - |     - |      72 B |
|                TypeMapperParameterless | .NET Core 3.1 |   886,634.276 ns |    44,151.9231 ns |  29,203.7634 ns |  84,994.66 |  5,274.36 | 1.9531 |     - |     - |   21896 B |
|              DirectAccessParameterless | .NET Core 3.1 |         9.846 ns |         1.1137 ns |       0.6627 ns |       0.94 |      0.10 | 0.0172 |     - |     - |      72 B |
|           TypeMapperOneParameterString | .NET Core 3.1 |   993,613.956 ns |    35,571.6378 ns |  21,168.1111 ns |  94,247.36 |  4,503.89 | 1.9531 |     - |     - |   23228 B |
|         DirectAccessOneParameterString | .NET Core 3.1 |        11.805 ns |         1.7282 ns |       1.1431 ns |       1.13 |      0.14 | 0.0172 |     - |     - |      72 B |
|   TypeMapperOneParameterTimeSpanUnwrap | .NET Core 3.1 | 1,272,506.608 ns |    35,045.7251 ns |  20,855.1489 ns | 120,808.42 |  7,713.80 | 1.9531 |     - |     - |   23072 B |
| DirectAccessOneParameterTimeSpanUnwrap | .NET Core 3.1 |        12.393 ns |         0.8532 ns |       0.5643 ns |       1.19 |      0.09 | 0.0172 |     - |     - |      72 B |
|       TypeMapperTwoParametersIntString | .NET Core 3.1 | 1,396,871.043 ns |    46,615.1326 ns |  30,833.0240 ns | 134,067.54 | 10,212.20 | 1.9531 |     - |     - |   24251 B |
|     DirectAccessTwoParametersIntString | .NET Core 3.1 |        12.112 ns |         0.7871 ns |       0.5206 ns |       1.16 |      0.09 | 0.0172 |     - |     - |      72 B |
|    TypeMapperTwoParametersStringString | .NET Core 3.1 | 1,394,289.463 ns |    87,164.9549 ns |  57,654.2208 ns | 133,680.02 |  9,406.26 | 1.9531 |     - |     - |   23886 B |
|  DirectAccessTwoParametersStringString | .NET Core 3.1 |        12.210 ns |         0.9673 ns |       0.6398 ns |       1.17 |      0.12 | 0.0172 |     - |     - |      72 B |
|     TypeMapperTwoParametersWrapperEnum | .NET Core 3.1 | 1,687,359.327 ns |    65,261.8655 ns |  43,166.6833 ns | 161,838.08 | 10,803.02 | 3.9063 |     - |     - |   27552 B |
|   DirectAccessTwoParametersWrapperEnum | .NET Core 3.1 |        13.325 ns |         0.8605 ns |       0.5692 ns |       1.28 |      0.08 | 0.0172 |     - |     - |      72 B |
|   TypeMapperTwoParametersWrapperString | .NET Core 3.1 | 1,476,162.720 ns |    38,490.7205 ns |  25,459.2284 ns | 141,564.17 |  8,820.97 | 1.9531 |     - |     - |   24438 B |
| DirectAccessTwoParametersWrapperString | .NET Core 3.1 |        13.348 ns |         2.0038 ns |       1.3254 ns |       1.28 |      0.12 | 0.0172 |     - |     - |      72 B |
|              TypeMapperThreeParameters | .NET Core 3.1 | 1,574,099.168 ns |    63,213.6002 ns |  41,811.8826 ns | 150,888.68 |  8,714.93 | 3.9063 |     - |     - |   28022 B |
|            DirectAccessThreeParameters | .NET Core 3.1 |        12.004 ns |         0.9026 ns |       0.5970 ns |       1.15 |      0.11 | 0.0172 |     - |     - |      72 B |
|                  TypeMapperTSTZFactory | .NET Core 3.1 | 1,878,378.717 ns |   255,172.4321 ns | 151,849.0213 ns | 178,359.52 | 18,040.70 | 3.9063 |     - |     - |   31135 B |
|                DirectAccessTSTZFactory | .NET Core 3.1 |       378.489 ns |        30.7150 ns |      20.3161 ns |      36.35 |      3.62 | 0.0172 |     - |     - |      72 B |
