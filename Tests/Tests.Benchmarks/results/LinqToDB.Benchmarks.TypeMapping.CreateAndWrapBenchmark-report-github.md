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
|                                 Method |       Runtime |       Mean |      Error |     StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------------------------------------- |-------------- |-----------:|-----------:|-----------:|------:|--------:|-------:|------:|------:|----------:|
|                TypeMapperParameterless |    .NET 4.6.2 |  59.895 ns |  2.4505 ns |  0.6364 ns | 10.35 |    0.55 | 0.0229 |     - |     - |      96 B |
|              DirectAccessParameterless |    .NET 4.6.2 |   5.802 ns |  1.1485 ns |  0.2983 ns |  1.00 |    0.00 | 0.0153 |     - |     - |      64 B |
|           TypeMapperOneParameterString |    .NET 4.6.2 |  63.148 ns |  3.3228 ns |  0.8629 ns | 10.90 |    0.44 | 0.0229 |     - |     - |      96 B |
|         DirectAccessOneParameterString |    .NET 4.6.2 |  11.356 ns |  0.5766 ns |  0.1497 ns |  1.96 |    0.10 | 0.0153 |     - |     - |      64 B |
|   TypeMapperOneParameterTimeSpanUnwrap |    .NET 4.6.2 |  20.368 ns |  0.8355 ns |  0.2170 ns |  3.52 |    0.19 | 0.0153 |     - |     - |      64 B |
| DirectAccessOneParameterTimeSpanUnwrap |    .NET 4.6.2 |  11.336 ns |  0.2924 ns |  0.0759 ns |  1.96 |    0.09 | 0.0153 |     - |     - |      64 B |
|       TypeMapperTwoParametersIntString |    .NET 4.6.2 |  63.756 ns |  1.6101 ns |  0.4181 ns | 11.01 |    0.49 | 0.0229 |     - |     - |      96 B |
|     DirectAccessTwoParametersIntString |    .NET 4.6.2 |  11.387 ns |  0.4319 ns |  0.1122 ns |  1.97 |    0.10 | 0.0153 |     - |     - |      64 B |
|    TypeMapperTwoParametersStringString |    .NET 4.6.2 |  63.452 ns |  5.5353 ns |  1.4375 ns | 10.96 |    0.66 | 0.0229 |     - |     - |      96 B |
|  DirectAccessTwoParametersStringString |    .NET 4.6.2 |  12.231 ns |  2.5870 ns |  0.6718 ns |  2.11 |    0.10 | 0.0153 |     - |     - |      64 B |
|     TypeMapperTwoParametersWrapperEnum |    .NET 4.6.2 |  77.748 ns |  3.3109 ns |  0.8598 ns | 13.43 |    0.74 | 0.0229 |     - |     - |      96 B |
|   DirectAccessTwoParametersWrapperEnum |    .NET 4.6.2 |   6.545 ns |  0.3123 ns |  0.0811 ns |  1.13 |    0.05 | 0.0153 |     - |     - |      64 B |
|   TypeMapperTwoParametersWrapperString |    .NET 4.6.2 |  65.753 ns |  1.9418 ns |  0.5043 ns | 11.36 |    0.53 | 0.0229 |     - |     - |      96 B |
| DirectAccessTwoParametersWrapperString |    .NET 4.6.2 |  11.427 ns |  0.3754 ns |  0.0975 ns |  1.97 |    0.10 | 0.0153 |     - |     - |      64 B |
|              TypeMapperThreeParameters |    .NET 4.6.2 |  78.220 ns |  1.9495 ns |  0.5063 ns | 13.51 |    0.71 | 0.0229 |     - |     - |      96 B |
|            DirectAccessThreeParameters |    .NET 4.6.2 |   6.666 ns |  0.5836 ns |  0.1515 ns |  1.15 |    0.06 | 0.0153 |     - |     - |      64 B |
|                  TypeMapperTSTZFactory |    .NET 4.6.2 | 305.826 ns | 52.5196 ns | 13.6392 ns | 52.86 |    4.09 | 0.0153 |     - |     - |      64 B |
|                DirectAccessTSTZFactory |    .NET 4.6.2 | 269.949 ns |  4.0789 ns |  1.0593 ns | 46.62 |    2.19 | 0.0153 |     - |     - |      64 B |
|                TypeMapperParameterless | .NET Core 2.1 |  45.515 ns |  0.2820 ns |  0.0155 ns |  7.73 |    0.46 | 0.0228 |     - |     - |      96 B |
|              DirectAccessParameterless | .NET Core 2.1 |   6.189 ns |  0.2198 ns |  0.0571 ns |  1.07 |    0.06 | 0.0152 |     - |     - |      64 B |
|           TypeMapperOneParameterString | .NET Core 2.1 |  47.657 ns |  1.3574 ns |  0.3525 ns |  8.23 |    0.39 | 0.0228 |     - |     - |      96 B |
|         DirectAccessOneParameterString | .NET Core 2.1 |   8.052 ns |  0.2709 ns |  0.0703 ns |  1.39 |    0.07 | 0.0152 |     - |     - |      64 B |
|   TypeMapperOneParameterTimeSpanUnwrap | .NET Core 2.1 |   9.103 ns |  0.6803 ns |  0.1767 ns |  1.57 |    0.08 | 0.0152 |     - |     - |      64 B |
| DirectAccessOneParameterTimeSpanUnwrap | .NET Core 2.1 |   7.868 ns |  0.4539 ns |  0.1179 ns |  1.36 |    0.05 | 0.0152 |     - |     - |      64 B |
|       TypeMapperTwoParametersIntString | .NET Core 2.1 |  46.347 ns |  1.4557 ns |  0.3781 ns |  8.00 |    0.35 | 0.0228 |     - |     - |      96 B |
|     DirectAccessTwoParametersIntString | .NET Core 2.1 |   8.330 ns |  0.5689 ns |  0.1477 ns |  1.44 |    0.08 | 0.0152 |     - |     - |      64 B |
|    TypeMapperTwoParametersStringString | .NET Core 2.1 |  45.861 ns |  0.7554 ns |  0.1962 ns |  7.92 |    0.36 | 0.0228 |     - |     - |      96 B |
|  DirectAccessTwoParametersStringString | .NET Core 2.1 |   8.008 ns |  0.3113 ns |  0.0809 ns |  1.38 |    0.06 | 0.0152 |     - |     - |      64 B |
|     TypeMapperTwoParametersWrapperEnum | .NET Core 2.1 |  60.418 ns |  1.8203 ns |  0.4727 ns | 10.43 |    0.44 | 0.0228 |     - |     - |      96 B |
|   DirectAccessTwoParametersWrapperEnum | .NET Core 2.1 |   7.145 ns |  1.1827 ns |  0.3072 ns |  1.23 |    0.03 | 0.0152 |     - |     - |      64 B |
|   TypeMapperTwoParametersWrapperString | .NET Core 2.1 |  47.447 ns |  1.0433 ns |  0.2709 ns |  8.19 |    0.36 | 0.0228 |     - |     - |      96 B |
| DirectAccessTwoParametersWrapperString | .NET Core 2.1 |   8.297 ns |  0.6156 ns |  0.1599 ns |  1.43 |    0.08 | 0.0152 |     - |     - |      64 B |
|              TypeMapperThreeParameters | .NET Core 2.1 |  60.121 ns |  1.0616 ns |  0.2757 ns | 10.38 |    0.51 | 0.0228 |     - |     - |      96 B |
|            DirectAccessThreeParameters | .NET Core 2.1 |   6.740 ns |  0.1018 ns |  0.0158 ns |  1.15 |    0.06 | 0.0152 |     - |     - |      64 B |
|                  TypeMapperTSTZFactory | .NET Core 2.1 | 256.573 ns |  9.5308 ns |  2.4751 ns | 44.30 |    1.89 | 0.0148 |     - |     - |      64 B |
|                DirectAccessTSTZFactory | .NET Core 2.1 | 263.926 ns |  5.2085 ns |  0.8060 ns | 45.14 |    2.40 | 0.0148 |     - |     - |      64 B |
|                TypeMapperParameterless | .NET Core 3.1 |  48.494 ns |  4.5619 ns |  1.1847 ns |  8.38 |    0.59 | 0.0229 |     - |     - |      96 B |
|              DirectAccessParameterless | .NET Core 3.1 |   6.524 ns |  0.1385 ns |  0.0214 ns |  1.12 |    0.05 | 0.0153 |     - |     - |      64 B |
|           TypeMapperOneParameterString | .NET Core 3.1 |  46.109 ns |  1.4599 ns |  0.3791 ns |  7.96 |    0.40 | 0.0229 |     - |     - |      96 B |
|         DirectAccessOneParameterString | .NET Core 3.1 |   6.869 ns |  0.1215 ns |  0.0316 ns |  1.19 |    0.06 | 0.0153 |     - |     - |      64 B |
|   TypeMapperOneParameterTimeSpanUnwrap | .NET Core 3.1 |   7.484 ns |  0.3110 ns |  0.0808 ns |  1.29 |    0.06 | 0.0153 |     - |     - |      64 B |
| DirectAccessOneParameterTimeSpanUnwrap | .NET Core 3.1 |   6.596 ns |  0.4230 ns |  0.1098 ns |  1.14 |    0.05 | 0.0153 |     - |     - |      64 B |
|       TypeMapperTwoParametersIntString | .NET Core 3.1 |  47.794 ns |  5.7867 ns |  1.5028 ns |  8.26 |    0.58 | 0.0229 |     - |     - |      96 B |
|     DirectAccessTwoParametersIntString | .NET Core 3.1 |   6.928 ns |  0.8868 ns |  0.2303 ns |  1.20 |    0.03 | 0.0153 |     - |     - |      64 B |
|    TypeMapperTwoParametersStringString | .NET Core 3.1 |  45.628 ns |  1.6004 ns |  0.4156 ns |  7.88 |    0.32 | 0.0229 |     - |     - |      96 B |
|  DirectAccessTwoParametersStringString | .NET Core 3.1 |   6.635 ns |  0.1863 ns |  0.0288 ns |  1.13 |    0.06 | 0.0153 |     - |     - |      64 B |
|     TypeMapperTwoParametersWrapperEnum | .NET Core 3.1 |  60.851 ns |  0.9646 ns |  0.1493 ns | 10.40 |    0.51 | 0.0229 |     - |     - |      96 B |
|   DirectAccessTwoParametersWrapperEnum | .NET Core 3.1 |   6.898 ns |  0.2475 ns |  0.0643 ns |  1.19 |    0.06 | 0.0153 |     - |     - |      64 B |
|   TypeMapperTwoParametersWrapperString | .NET Core 3.1 |  48.424 ns |  1.0490 ns |  0.2724 ns |  8.36 |    0.38 | 0.0229 |     - |     - |      96 B |
| DirectAccessTwoParametersWrapperString | .NET Core 3.1 |   7.253 ns |  0.5709 ns |  0.1483 ns |  1.25 |    0.06 | 0.0153 |     - |     - |      64 B |
|              TypeMapperThreeParameters | .NET Core 3.1 |  62.690 ns |  5.8765 ns |  1.5261 ns | 10.82 |    0.53 | 0.0229 |     - |     - |      96 B |
|            DirectAccessThreeParameters | .NET Core 3.1 |   6.540 ns |  0.1626 ns |  0.0252 ns |  1.12 |    0.05 | 0.0153 |     - |     - |      64 B |
|                  TypeMapperTSTZFactory | .NET Core 3.1 | 264.875 ns |  3.6861 ns |  0.9573 ns | 45.74 |    2.13 | 0.0148 |     - |     - |      64 B |
|                DirectAccessTSTZFactory | .NET Core 3.1 | 259.139 ns |  4.2951 ns |  0.6647 ns | 44.31 |    2.15 | 0.0148 |     - |     - |      64 B |
