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
|              Method |       Runtime |           Mean |          Error |         StdDev |     Ratio |  RatioSD |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|-------------------- |-------------- |---------------:|---------------:|---------------:|----------:|---------:|-------:|-------:|------:|----------:|
|    TypeMapperString |    .NET 4.6.2 | 205,228.938 ns | 11,659.7796 ns |  7,712.2223 ns | 55,516.02 | 3,542.24 | 1.9531 | 0.2441 |     - |    8978 B |
|  DirectAccessString |    .NET 4.6.2 |       3.703 ns |      0.1650 ns |      0.1091 ns |      1.00 |     0.00 |      - |      - |     - |         - |
|       TypeMapperInt |    .NET 4.6.2 | 192,031.668 ns |  9,932.9697 ns |  6,570.0444 ns | 51,906.68 | 2,383.91 | 1.9531 | 0.2441 |     - |    9026 B |
|     DirectAccessInt |    .NET 4.6.2 |       1.354 ns |      0.1327 ns |      0.0878 ns |      0.37 |     0.03 |      - |      - |     - |         - |
|   TypeMapperBoolean |    .NET 4.6.2 | 194,860.711 ns |  6,921.3806 ns |  4,578.0647 ns | 52,665.62 | 1,872.71 | 1.9531 | 0.2441 |     - |    9026 B |
| DirectAccessBoolean |    .NET 4.6.2 |       1.382 ns |      0.1524 ns |      0.1008 ns |      0.37 |     0.03 |      - |      - |     - |         - |
|   TypeMapperWrapper |    .NET 4.6.2 | 345,609.431 ns | 24,653.0839 ns | 16,306.4886 ns | 93,389.68 | 4,659.21 | 1.9531 | 0.4883 |     - |    9260 B |
| DirectAccessWrapper |    .NET 4.6.2 |       3.525 ns |      0.1556 ns |      0.0926 ns |      0.95 |     0.03 |      - |      - |     - |         - |
|    TypeMapperString | .NET Core 2.1 | 144,364.291 ns | 10,005.1930 ns |  6,617.8157 ns | 39,051.74 | 2,682.71 | 1.4648 | 0.2441 |     - |    6889 B |
|  DirectAccessString | .NET Core 2.1 |       5.440 ns |      0.1801 ns |      0.1191 ns |      1.47 |     0.03 |      - |      - |     - |         - |
|       TypeMapperInt | .NET Core 2.1 | 130,822.513 ns |  8,379.1779 ns |  5,542.3074 ns | 35,354.56 | 1,706.83 | 1.5869 | 0.3662 |     - |    6825 B |
|     DirectAccessInt | .NET Core 2.1 |       3.261 ns |      0.5580 ns |      0.1449 ns |      0.89 |     0.06 |      - |      - |     - |         - |
|   TypeMapperBoolean | .NET Core 2.1 | 131,707.660 ns |  6,227.6225 ns |  4,119.1867 ns | 35,585.27 | 1,078.19 | 1.4648 | 0.2441 |     - |    6937 B |
| DirectAccessBoolean | .NET Core 2.1 |       3.318 ns |      0.1701 ns |      0.1125 ns |      0.90 |     0.04 |      - |      - |     - |         - |
|   TypeMapperWrapper | .NET Core 2.1 | 246,584.785 ns |  5,221.4600 ns |  3,453.6725 ns | 66,640.92 | 1,830.04 | 1.7090 | 0.2441 |     - |    7465 B |
| DirectAccessWrapper | .NET Core 2.1 |       4.044 ns |      0.2270 ns |      0.1351 ns |      1.09 |     0.05 |      - |      - |     - |         - |
|    TypeMapperString | .NET Core 3.1 | 134,271.733 ns |  7,404.0725 ns |  4,897.3355 ns | 36,302.36 | 1,939.71 | 1.4648 | 0.2441 |     - |    6711 B |
|  DirectAccessString | .NET Core 3.1 |       3.437 ns |      0.1801 ns |      0.1191 ns |      0.93 |     0.03 |      - |      - |     - |         - |
|       TypeMapperInt | .NET Core 3.1 | 119,355.205 ns |  7,230.2825 ns |  4,782.3842 ns | 32,248.13 | 1,279.23 | 1.4648 | 0.2441 |     - |    6647 B |
|     DirectAccessInt | .NET Core 3.1 |       1.599 ns |      0.0835 ns |      0.0553 ns |      0.43 |     0.02 |      - |      - |     - |         - |
|   TypeMapperBoolean | .NET Core 3.1 | 152,132.617 ns | 10,617.6562 ns |  7,022.9222 ns | 41,108.01 | 1,997.97 | 1.4648 | 0.2441 |     - |    6759 B |
| DirectAccessBoolean | .NET Core 3.1 |       1.333 ns |      0.0634 ns |      0.0377 ns |      0.36 |     0.02 |      - |      - |     - |         - |
|   TypeMapperWrapper | .NET Core 3.1 | 220,365.422 ns |  4,267.8952 ns |  1,108.3588 ns | 60,180.63 | 2,045.57 | 1.7090 | 0.2441 |     - |    7289 B |
| DirectAccessWrapper | .NET Core 3.1 |       2.976 ns |      0.0882 ns |      0.0461 ns |      0.81 |     0.03 |      - |      - |     - |         - |
