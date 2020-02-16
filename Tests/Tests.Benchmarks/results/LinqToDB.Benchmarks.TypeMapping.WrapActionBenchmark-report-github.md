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
|                          Method |       Runtime |            Mean |          Error |         StdDev |      Ratio |   RatioSD |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|-------------------------------- |-------------- |----------------:|---------------:|---------------:|-----------:|----------:|-------:|-------:|------:|----------:|
|                TypeMapperAction |    .NET 4.6.2 |  19,101.1113 ns |  4,853.9864 ns |  3,210.6115 ns |  12,678.92 |  2,440.27 | 0.5188 | 0.3052 |     - |    3331 B |
|              DirectAccessAction |    .NET 4.6.2 |       1.5161 ns |      0.1543 ns |      0.1020 ns |       1.00 |      0.00 |      - |      - |     - |         - |
|        TypeMapperActionWithCast |    .NET 4.6.2 | 244,779.4630 ns |  7,354.4247 ns |  4,864.4966 ns | 162,074.10 | 10,598.41 | 0.9766 | 0.4883 |     - |    7547 B |
|      DirectAccessActionWithCast |    .NET 4.6.2 |       1.7423 ns |      0.2238 ns |      0.1480 ns |       1.15 |      0.11 |      - |      - |     - |         - |
|   TypeMapperActionWithParameter |    .NET 4.6.2 |  28,840.0910 ns |  2,596.5978 ns |  1,717.4887 ns |  19,088.58 |  1,623.41 | 0.6714 | 0.1831 |     - |    4358 B |
| DirectAccessActionWithParameter |    .NET 4.6.2 |       1.4534 ns |      0.1860 ns |      0.1107 ns |       0.97 |      0.12 |      - |      - |     - |         - |
|                TypeMapperAction | .NET Core 2.1 |  16,396.4643 ns |  1,959.4260 ns |  1,296.0390 ns |  10,859.98 |  1,149.87 | 0.3052 | 0.0916 |     - |    1976 B |
|              DirectAccessAction | .NET Core 2.1 |       1.8139 ns |      0.0716 ns |      0.0473 ns |       1.20 |      0.09 |      - |      - |     - |         - |
|        TypeMapperActionWithCast | .NET Core 2.1 | 261,320.3788 ns | 16,528.0518 ns | 10,932.2829 ns | 173,049.02 | 13,344.42 | 0.9766 | 0.4883 |     - |    6321 B |
|      DirectAccessActionWithCast | .NET Core 2.1 |       1.7598 ns |      0.0694 ns |      0.0308 ns |       1.15 |      0.08 |      - |      - |     - |         - |
|   TypeMapperActionWithParameter | .NET Core 2.1 |  29,004.6889 ns |  2,803.7034 ns |  1,854.4762 ns |  19,236.93 |  2,104.16 | 0.4578 | 0.1526 |     - |    2976 B |
| DirectAccessActionWithParameter | .NET Core 2.1 |       1.9559 ns |      0.1450 ns |      0.0959 ns |       1.30 |      0.12 |      - |      - |     - |         - |
|                TypeMapperAction | .NET Core 3.1 |  11,199.0238 ns |    851.1149 ns |    562.9598 ns |   7,422.57 |    701.94 | 0.3052 | 0.0763 |     - |    1936 B |
|              DirectAccessAction | .NET Core 3.1 |       1.5527 ns |      0.1045 ns |      0.0691 ns |       1.03 |      0.08 |      - |      - |     - |         - |
|        TypeMapperActionWithCast | .NET Core 3.1 | 211,023.5293 ns |  5,438.4375 ns |  3,597.1897 ns | 139,789.15 | 10,162.45 | 0.9766 | 0.2441 |     - |    6170 B |
|      DirectAccessActionWithCast | .NET Core 3.1 |       0.5345 ns |      0.0505 ns |      0.0264 ns |       0.35 |      0.03 |      - |      - |     - |         - |
|   TypeMapperActionWithParameter | .NET Core 3.1 |  14,548.6682 ns |  1,306.6661 ns |    864.2787 ns |   9,635.28 |    876.68 | 0.4578 | 0.1678 |     - |    2904 B |
| DirectAccessActionWithParameter | .NET Core 3.1 |       1.6228 ns |      0.1468 ns |      0.0971 ns |       1.07 |      0.09 |      - |      - |     - |         - |
