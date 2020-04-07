``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417995 Hz, Resolution=292.5692 ns, Timer=TSC
  [Host]     : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-FSMYUH : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-TSQXSD : .NET Core 2.1.17 (CoreCLR 4.6.28619.01, CoreFX 4.6.28619.01), X64 RyuJIT
  Job-OUTKHJ : .NET Core 3.1.3 (CoreCLR 4.700.20.11803, CoreFX 4.700.20.12001), X64 RyuJIT

Jit=RyuJit  Platform=X64  MaxIterationCount=5  
MinIterationCount=3  WarmupCount=2  

```
|                                 Method |       Runtime |       Mean | Ratio |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------------------------------------- |-------------- |-----------:|------:|-------:|------:|------:|----------:|
|                TypeMapperParameterless |    .NET 4.6.2 |  61.556 ns | 11.25 | 0.0229 |     - |     - |      96 B |
|              DirectAccessParameterless |    .NET 4.6.2 |   5.474 ns |  1.00 | 0.0153 |     - |     - |      64 B |
|           TypeMapperOneParameterString |    .NET 4.6.2 |  64.086 ns | 11.71 | 0.0229 |     - |     - |      96 B |
|         DirectAccessOneParameterString |    .NET 4.6.2 |  10.931 ns |  2.00 | 0.0153 |     - |     - |      64 B |
|   TypeMapperOneParameterTimeSpanUnwrap |    .NET 4.6.2 |  20.066 ns |  3.67 | 0.0153 |     - |     - |      64 B |
| DirectAccessOneParameterTimeSpanUnwrap |    .NET 4.6.2 |  11.483 ns |  2.10 | 0.0153 |     - |     - |      64 B |
|       TypeMapperTwoParametersIntString |    .NET 4.6.2 |  64.744 ns | 11.83 | 0.0229 |     - |     - |      96 B |
|     DirectAccessTwoParametersIntString |    .NET 4.6.2 |  11.568 ns |  2.11 | 0.0153 |     - |     - |      64 B |
|    TypeMapperTwoParametersStringString |    .NET 4.6.2 |  63.740 ns | 11.65 | 0.0229 |     - |     - |      96 B |
|  DirectAccessTwoParametersStringString |    .NET 4.6.2 |  11.911 ns |  2.18 | 0.0153 |     - |     - |      64 B |
|     TypeMapperTwoParametersWrapperEnum |    .NET 4.6.2 |  77.032 ns | 14.07 | 0.0229 |     - |     - |      96 B |
|   DirectAccessTwoParametersWrapperEnum |    .NET 4.6.2 |   6.076 ns |  1.11 | 0.0153 |     - |     - |      64 B |
|   TypeMapperTwoParametersWrapperString |    .NET 4.6.2 |  64.663 ns | 11.81 | 0.0229 |     - |     - |      96 B |
| DirectAccessTwoParametersWrapperString |    .NET 4.6.2 |  11.280 ns |  2.06 | 0.0153 |     - |     - |      64 B |
|              TypeMapperThreeParameters |    .NET 4.6.2 |  77.736 ns | 14.26 | 0.0229 |     - |     - |      96 B |
|            DirectAccessThreeParameters |    .NET 4.6.2 |   6.422 ns |  1.17 | 0.0153 |     - |     - |      64 B |
|                  TypeMapperTSTZFactory |    .NET 4.6.2 | 286.997 ns | 52.44 | 0.0153 |     - |     - |      64 B |
|                DirectAccessTSTZFactory |    .NET 4.6.2 | 296.282 ns | 54.13 | 0.0153 |     - |     - |      64 B |
|                TypeMapperParameterless | .NET Core 2.1 |  43.761 ns |  8.00 | 0.0228 |     - |     - |      96 B |
|              DirectAccessParameterless | .NET Core 2.1 |   6.038 ns |  1.10 | 0.0152 |     - |     - |      64 B |
|           TypeMapperOneParameterString | .NET Core 2.1 |  47.997 ns |  8.77 | 0.0228 |     - |     - |      96 B |
|         DirectAccessOneParameterString | .NET Core 2.1 |   7.523 ns |  1.37 | 0.0152 |     - |     - |      64 B |
|   TypeMapperOneParameterTimeSpanUnwrap | .NET Core 2.1 |   8.835 ns |  1.61 | 0.0152 |     - |     - |      64 B |
| DirectAccessOneParameterTimeSpanUnwrap | .NET Core 2.1 |   9.116 ns |  1.67 | 0.0152 |     - |     - |      64 B |
|       TypeMapperTwoParametersIntString | .NET Core 2.1 |  46.319 ns |  8.46 | 0.0228 |     - |     - |      96 B |
|     DirectAccessTwoParametersIntString | .NET Core 2.1 |   7.784 ns |  1.42 | 0.0152 |     - |     - |      64 B |
|    TypeMapperTwoParametersStringString | .NET Core 2.1 |  45.784 ns |  8.36 | 0.0228 |     - |     - |      96 B |
|  DirectAccessTwoParametersStringString | .NET Core 2.1 |   7.758 ns |  1.42 | 0.0152 |     - |     - |      64 B |
|     TypeMapperTwoParametersWrapperEnum | .NET Core 2.1 |  60.758 ns | 11.10 | 0.0228 |     - |     - |      96 B |
|   DirectAccessTwoParametersWrapperEnum | .NET Core 2.1 |   6.741 ns |  1.23 | 0.0152 |     - |     - |      64 B |
|   TypeMapperTwoParametersWrapperString | .NET Core 2.1 |  47.603 ns |  8.70 | 0.0228 |     - |     - |      96 B |
| DirectAccessTwoParametersWrapperString | .NET Core 2.1 |   7.796 ns |  1.42 | 0.0152 |     - |     - |      64 B |
|              TypeMapperThreeParameters | .NET Core 2.1 |  57.410 ns | 10.49 | 0.0228 |     - |     - |      96 B |
|            DirectAccessThreeParameters | .NET Core 2.1 |   7.007 ns |  1.28 | 0.0152 |     - |     - |      64 B |
|                  TypeMapperTSTZFactory | .NET Core 2.1 | 254.073 ns | 46.42 | 0.0148 |     - |     - |      64 B |
|                DirectAccessTSTZFactory | .NET Core 2.1 | 269.445 ns | 49.23 | 0.0148 |     - |     - |      64 B |
|                TypeMapperParameterless | .NET Core 3.1 |  45.314 ns |  8.28 | 0.0229 |     - |     - |      96 B |
|              DirectAccessParameterless | .NET Core 3.1 |   6.414 ns |  1.17 | 0.0153 |     - |     - |      64 B |
|           TypeMapperOneParameterString | .NET Core 3.1 |  45.654 ns |  8.34 | 0.0229 |     - |     - |      96 B |
|         DirectAccessOneParameterString | .NET Core 3.1 |   6.716 ns |  1.23 | 0.0153 |     - |     - |      64 B |
|   TypeMapperOneParameterTimeSpanUnwrap | .NET Core 3.1 |   7.691 ns |  1.41 | 0.0153 |     - |     - |      64 B |
| DirectAccessOneParameterTimeSpanUnwrap | .NET Core 3.1 |   6.961 ns |  1.27 | 0.0153 |     - |     - |      64 B |
|       TypeMapperTwoParametersIntString | .NET Core 3.1 |  47.314 ns |  8.64 | 0.0229 |     - |     - |      96 B |
|     DirectAccessTwoParametersIntString | .NET Core 3.1 |   6.876 ns |  1.26 | 0.0153 |     - |     - |      64 B |
|    TypeMapperTwoParametersStringString | .NET Core 3.1 |  44.715 ns |  8.17 | 0.0229 |     - |     - |      96 B |
|  DirectAccessTwoParametersStringString | .NET Core 3.1 |   6.637 ns |  1.21 | 0.0153 |     - |     - |      64 B |
|     TypeMapperTwoParametersWrapperEnum | .NET Core 3.1 |  60.203 ns | 11.00 | 0.0229 |     - |     - |      96 B |
|   DirectAccessTwoParametersWrapperEnum | .NET Core 3.1 |   6.887 ns |  1.26 | 0.0153 |     - |     - |      64 B |
|   TypeMapperTwoParametersWrapperString | .NET Core 3.1 |  49.785 ns |  9.09 | 0.0229 |     - |     - |      96 B |
| DirectAccessTwoParametersWrapperString | .NET Core 3.1 |   7.345 ns |  1.34 | 0.0153 |     - |     - |      64 B |
|              TypeMapperThreeParameters | .NET Core 3.1 |  61.695 ns | 11.27 | 0.0229 |     - |     - |      96 B |
|            DirectAccessThreeParameters | .NET Core 3.1 |   6.520 ns |  1.19 | 0.0153 |     - |     - |      64 B |
|                  TypeMapperTSTZFactory | .NET Core 3.1 | 262.649 ns | 47.98 | 0.0153 |     - |     - |      64 B |
|                DirectAccessTSTZFactory | .NET Core 3.1 | 263.751 ns | 48.18 | 0.0148 |     - |     - |      64 B |
