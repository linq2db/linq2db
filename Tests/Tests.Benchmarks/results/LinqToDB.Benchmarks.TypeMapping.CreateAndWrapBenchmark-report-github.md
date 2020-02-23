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
|                                 Method |       Runtime |       Mean |      Error |    StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------------------------------------- |-------------- |-----------:|-----------:|----------:|------:|--------:|-------:|------:|------:|----------:|
|                TypeMapperParameterless |    .NET 4.6.2 |  63.215 ns |  1.8326 ns | 0.4759 ns | 11.29 |    0.19 | 0.0248 |     - |     - |     104 B |
|              DirectAccessParameterless |    .NET 4.6.2 |   5.598 ns |  0.2894 ns | 0.0752 ns |  1.00 |    0.00 | 0.0153 |     - |     - |      64 B |
|           TypeMapperOneParameterString |    .NET 4.6.2 |  72.562 ns |  8.7985 ns | 2.2849 ns | 12.96 |    0.44 | 0.0248 |     - |     - |     104 B |
|         DirectAccessOneParameterString |    .NET 4.6.2 |  10.884 ns |  0.2104 ns | 0.0326 ns |  1.94 |    0.03 | 0.0153 |     - |     - |      64 B |
|   TypeMapperOneParameterTimeSpanUnwrap |    .NET 4.6.2 |  18.128 ns |  0.5755 ns | 0.1495 ns |  3.24 |    0.07 | 0.0153 |     - |     - |      64 B |
| DirectAccessOneParameterTimeSpanUnwrap |    .NET 4.6.2 |  11.214 ns |  0.1179 ns | 0.0306 ns |  2.00 |    0.03 | 0.0153 |     - |     - |      64 B |
|       TypeMapperTwoParametersIntString |    .NET 4.6.2 |  70.373 ns |  2.9162 ns | 0.7573 ns | 12.57 |    0.25 | 0.0248 |     - |     - |     104 B |
|     DirectAccessTwoParametersIntString |    .NET 4.6.2 |  11.278 ns |  1.4996 ns | 0.3894 ns |  2.01 |    0.07 | 0.0153 |     - |     - |      64 B |
|    TypeMapperTwoParametersStringString |    .NET 4.6.2 |  68.687 ns |  3.4505 ns | 0.8961 ns | 12.27 |    0.19 | 0.0248 |     - |     - |     104 B |
|  DirectAccessTwoParametersStringString |    .NET 4.6.2 |  11.197 ns |  0.2716 ns | 0.0705 ns |  2.00 |    0.03 | 0.0153 |     - |     - |      64 B |
|     TypeMapperTwoParametersWrapperEnum |    .NET 4.6.2 |  84.224 ns | 11.7448 ns | 3.0501 ns | 15.05 |    0.66 | 0.0248 |     - |     - |     104 B |
|   DirectAccessTwoParametersWrapperEnum |    .NET 4.6.2 |   6.142 ns |  0.2413 ns | 0.0627 ns |  1.10 |    0.02 | 0.0153 |     - |     - |      64 B |
|   TypeMapperTwoParametersWrapperString |    .NET 4.6.2 |  68.821 ns |  1.1988 ns | 0.1855 ns | 12.26 |    0.14 | 0.0248 |     - |     - |     104 B |
| DirectAccessTwoParametersWrapperString |    .NET 4.6.2 |  11.223 ns |  0.2150 ns | 0.0558 ns |  2.01 |    0.04 | 0.0153 |     - |     - |      64 B |
|              TypeMapperThreeParameters |    .NET 4.6.2 |  92.049 ns | 13.7540 ns | 3.5719 ns | 16.45 |    0.71 | 0.0248 |     - |     - |     104 B |
|            DirectAccessThreeParameters |    .NET 4.6.2 |   6.197 ns |  0.4939 ns | 0.1283 ns |  1.11 |    0.02 | 0.0153 |     - |     - |      64 B |
|                  TypeMapperTSTZFactory |    .NET 4.6.2 | 278.679 ns | 10.6477 ns | 2.7652 ns | 49.79 |    0.82 | 0.0153 |     - |     - |      64 B |
|                DirectAccessTSTZFactory |    .NET 4.6.2 | 264.346 ns | 11.7579 ns | 3.0535 ns | 47.22 |    0.61 | 0.0153 |     - |     - |      64 B |
|                TypeMapperParameterless | .NET Core 2.1 |  52.167 ns |  2.4268 ns | 0.6302 ns |  9.32 |    0.19 | 0.0247 |     - |     - |     104 B |
|              DirectAccessParameterless | .NET Core 2.1 |   6.012 ns |  0.1916 ns | 0.0498 ns |  1.07 |    0.01 | 0.0152 |     - |     - |      64 B |
|           TypeMapperOneParameterString | .NET Core 2.1 |  51.891 ns |  1.0789 ns | 0.1670 ns |  9.24 |    0.12 | 0.0247 |     - |     - |     104 B |
|         DirectAccessOneParameterString | .NET Core 2.1 |   7.743 ns |  1.5219 ns | 0.3952 ns |  1.38 |    0.08 | 0.0152 |     - |     - |      64 B |
|   TypeMapperOneParameterTimeSpanUnwrap | .NET Core 2.1 |   8.739 ns |  0.3331 ns | 0.0865 ns |  1.56 |    0.02 | 0.0152 |     - |     - |      64 B |
| DirectAccessOneParameterTimeSpanUnwrap | .NET Core 2.1 |   7.959 ns |  0.2855 ns | 0.0742 ns |  1.42 |    0.02 | 0.0152 |     - |     - |      64 B |
|       TypeMapperTwoParametersIntString | .NET Core 2.1 |  52.446 ns | 10.0666 ns | 2.6143 ns |  9.37 |    0.45 | 0.0247 |     - |     - |     104 B |
|     DirectAccessTwoParametersIntString | .NET Core 2.1 |   7.862 ns |  0.2476 ns | 0.0643 ns |  1.40 |    0.02 | 0.0152 |     - |     - |      64 B |
|    TypeMapperTwoParametersStringString | .NET Core 2.1 |  51.283 ns |  1.2268 ns | 0.3186 ns |  9.16 |    0.16 | 0.0247 |     - |     - |     104 B |
|  DirectAccessTwoParametersStringString | .NET Core 2.1 |   8.754 ns |  1.1833 ns | 0.3073 ns |  1.56 |    0.07 | 0.0152 |     - |     - |      64 B |
|     TypeMapperTwoParametersWrapperEnum | .NET Core 2.1 |  66.136 ns |  6.8062 ns | 1.7675 ns | 11.82 |    0.39 | 0.0247 |     - |     - |     104 B |
|   DirectAccessTwoParametersWrapperEnum | .NET Core 2.1 |   6.599 ns |  0.1137 ns | 0.0176 ns |  1.18 |    0.01 | 0.0152 |     - |     - |      64 B |
|   TypeMapperTwoParametersWrapperString | .NET Core 2.1 |  55.504 ns | 13.1900 ns | 3.4254 ns |  9.92 |    0.60 | 0.0247 |     - |     - |     104 B |
| DirectAccessTwoParametersWrapperString | .NET Core 2.1 |   8.048 ns |  0.2336 ns | 0.0607 ns |  1.44 |    0.02 | 0.0152 |     - |     - |      64 B |
|              TypeMapperThreeParameters | .NET Core 2.1 |  62.670 ns |  1.3454 ns | 0.3494 ns | 11.20 |    0.18 | 0.0247 |     - |     - |     104 B |
|            DirectAccessThreeParameters | .NET Core 2.1 |   6.561 ns |  0.9502 ns | 0.2468 ns |  1.17 |    0.04 | 0.0152 |     - |     - |      64 B |
|                  TypeMapperTSTZFactory | .NET Core 2.1 | 259.737 ns |  4.8607 ns | 0.7522 ns | 46.25 |    0.55 | 0.0148 |     - |     - |      64 B |
|                DirectAccessTSTZFactory | .NET Core 2.1 | 259.611 ns |  3.2198 ns | 0.8362 ns | 46.38 |    0.71 | 0.0148 |     - |     - |      64 B |
|                TypeMapperParameterless | .NET Core 3.1 |  52.763 ns |  1.9768 ns | 0.5134 ns |  9.43 |    0.16 | 0.0249 |     - |     - |     104 B |
|              DirectAccessParameterless | .NET Core 3.1 |   6.543 ns |  0.1435 ns | 0.0373 ns |  1.17 |    0.01 | 0.0153 |     - |     - |      64 B |
|           TypeMapperOneParameterString | .NET Core 3.1 |  52.225 ns |  0.7997 ns | 0.2077 ns |  9.33 |    0.12 | 0.0248 |     - |     - |     104 B |
|         DirectAccessOneParameterString | .NET Core 3.1 |   6.931 ns |  0.3663 ns | 0.0951 ns |  1.24 |    0.02 | 0.0153 |     - |     - |      64 B |
|   TypeMapperOneParameterTimeSpanUnwrap | .NET Core 3.1 |   7.425 ns |  0.3836 ns | 0.0996 ns |  1.33 |    0.03 | 0.0153 |     - |     - |      64 B |
| DirectAccessOneParameterTimeSpanUnwrap | .NET Core 3.1 |   6.712 ns |  0.4932 ns | 0.1281 ns |  1.20 |    0.02 | 0.0153 |     - |     - |      64 B |
|       TypeMapperTwoParametersIntString | .NET Core 3.1 |  52.577 ns |  1.3589 ns | 0.3529 ns |  9.39 |    0.15 | 0.0249 |     - |     - |     104 B |
|     DirectAccessTwoParametersIntString | .NET Core 3.1 |   6.891 ns |  0.1608 ns | 0.0088 ns |  1.23 |    0.02 | 0.0153 |     - |     - |      64 B |
|    TypeMapperTwoParametersStringString | .NET Core 3.1 |  52.104 ns |  2.9974 ns | 0.7784 ns |  9.31 |    0.15 | 0.0249 |     - |     - |     104 B |
|  DirectAccessTwoParametersStringString | .NET Core 3.1 |   6.883 ns |  1.6004 ns | 0.4156 ns |  1.23 |    0.08 | 0.0153 |     - |     - |      64 B |
|     TypeMapperTwoParametersWrapperEnum | .NET Core 3.1 |  63.268 ns |  2.2202 ns | 0.5766 ns | 11.30 |    0.21 | 0.0248 |     - |     - |     104 B |
|   DirectAccessTwoParametersWrapperEnum | .NET Core 3.1 |   6.882 ns |  0.3574 ns | 0.0928 ns |  1.23 |    0.02 | 0.0153 |     - |     - |      64 B |
|   TypeMapperTwoParametersWrapperString | .NET Core 3.1 |  52.872 ns |  0.9880 ns | 0.1529 ns |  9.42 |    0.15 | 0.0249 |     - |     - |     104 B |
| DirectAccessTwoParametersWrapperString | .NET Core 3.1 |   6.683 ns |  0.2627 ns | 0.0682 ns |  1.19 |    0.02 | 0.0153 |     - |     - |      64 B |
|              TypeMapperThreeParameters | .NET Core 3.1 |  66.181 ns |  1.3813 ns | 0.3587 ns | 11.82 |    0.21 | 0.0248 |     - |     - |     104 B |
|            DirectAccessThreeParameters | .NET Core 3.1 |   6.599 ns |  0.3213 ns | 0.0834 ns |  1.18 |    0.02 | 0.0153 |     - |     - |      64 B |
|                  TypeMapperTSTZFactory | .NET Core 3.1 | 261.963 ns |  7.8621 ns | 2.0418 ns | 46.80 |    0.93 | 0.0148 |     - |     - |      64 B |
|                DirectAccessTSTZFactory | .NET Core 3.1 | 269.499 ns | 31.3057 ns | 8.1300 ns | 48.14 |    1.28 | 0.0153 |     - |     - |      64 B |
