``` ini

BenchmarkDotNet=v0.12.1.1533-nightly, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417995 Hz, Resolution=292.5692 ns, Timer=TSC
  [Host]     : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-GUCTZK : .NET 5.0.5 (5.0.521.16609), X64 RyuJIT
  Job-IOHEYN : .NET Core 3.1.14 (CoreCLR 4.700.21.16201, CoreFX 4.700.21.16208), X64 RyuJIT
  Job-FWTWYQ : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT

Jit=RyuJit  Platform=X64  

```
|                                 Method |              Runtime |       Mean |     Median | Ratio | Allocated |
|--------------------------------------- |--------------------- |-----------:|-----------:|------:|----------:|
|                TypeMapperParameterless |             .NET 5.0 |  43.073 ns |  42.705 ns |  8.75 |      96 B |
|              DirectAccessParameterless |             .NET 5.0 |   5.563 ns |   5.560 ns |  1.13 |      64 B |
|           TypeMapperOneParameterString |             .NET 5.0 |  41.884 ns |  41.779 ns |  8.51 |      96 B |
|         DirectAccessOneParameterString |             .NET 5.0 |   5.686 ns |   5.672 ns |  1.16 |      64 B |
|   TypeMapperOneParameterTimeSpanUnwrap |             .NET 5.0 |   6.730 ns |   6.666 ns |  1.39 |      64 B |
| DirectAccessOneParameterTimeSpanUnwrap |             .NET 5.0 |   5.819 ns |   5.829 ns |  1.19 |      64 B |
|       TypeMapperTwoParametersIntString |             .NET 5.0 |  39.870 ns |  39.906 ns |  8.11 |      96 B |
|     DirectAccessTwoParametersIntString |             .NET 5.0 |   5.716 ns |   5.710 ns |  1.16 |      64 B |
|    TypeMapperTwoParametersStringString |             .NET 5.0 |  42.317 ns |  42.364 ns |  8.59 |      96 B |
|  DirectAccessTwoParametersStringString |             .NET 5.0 |   5.887 ns |   5.884 ns |  1.20 |      64 B |
|     TypeMapperTwoParametersWrapperEnum |             .NET 5.0 |  53.731 ns |  53.639 ns | 10.91 |      96 B |
|   DirectAccessTwoParametersWrapperEnum |             .NET 5.0 |   5.478 ns |   5.474 ns |  1.11 |      64 B |
|   TypeMapperTwoParametersWrapperString |             .NET 5.0 |  42.387 ns |  42.386 ns |  8.61 |      96 B |
| DirectAccessTwoParametersWrapperString |             .NET 5.0 |   5.871 ns |   5.883 ns |  1.19 |      64 B |
|              TypeMapperThreeParameters |             .NET 5.0 |  54.159 ns |  54.043 ns | 11.00 |      96 B |
|            DirectAccessThreeParameters |             .NET 5.0 |   5.891 ns |   5.880 ns |  1.20 |      64 B |
|                  TypeMapperTSTZFactory |             .NET 5.0 | 213.050 ns | 212.822 ns | 43.34 |      64 B |
|                DirectAccessTSTZFactory |             .NET 5.0 | 219.112 ns | 218.411 ns | 44.53 |      64 B |
|                TypeMapperParameterless |        .NET Core 3.1 |  44.552 ns |  44.050 ns |  9.06 |      96 B |
|              DirectAccessParameterless |        .NET Core 3.1 |   5.895 ns |   5.875 ns |  1.20 |      64 B |
|           TypeMapperOneParameterString |        .NET Core 3.1 |  46.004 ns |  45.531 ns |  9.35 |      96 B |
|         DirectAccessOneParameterString |        .NET Core 3.1 |   5.567 ns |   5.552 ns |  1.13 |      64 B |
|   TypeMapperOneParameterTimeSpanUnwrap |        .NET Core 3.1 |   6.727 ns |   6.718 ns |  1.37 |      64 B |
| DirectAccessOneParameterTimeSpanUnwrap |        .NET Core 3.1 |   5.886 ns |   5.771 ns |  1.21 |      64 B |
|       TypeMapperTwoParametersIntString |        .NET Core 3.1 |  46.810 ns |  46.585 ns |  9.50 |      96 B |
|     DirectAccessTwoParametersIntString |        .NET Core 3.1 |   5.852 ns |   5.779 ns |  1.19 |      64 B |
|    TypeMapperTwoParametersStringString |        .NET Core 3.1 |  44.218 ns |  44.228 ns |  8.99 |      96 B |
|  DirectAccessTwoParametersStringString |        .NET Core 3.1 |   6.304 ns |   6.174 ns |  1.27 |      64 B |
|     TypeMapperTwoParametersWrapperEnum |        .NET Core 3.1 |  57.843 ns |  58.033 ns | 11.76 |      96 B |
|   DirectAccessTwoParametersWrapperEnum |        .NET Core 3.1 |   6.080 ns |   6.075 ns |  1.24 |      64 B |
|   TypeMapperTwoParametersWrapperString |        .NET Core 3.1 |  48.033 ns |  47.619 ns |  9.82 |      96 B |
| DirectAccessTwoParametersWrapperString |        .NET Core 3.1 |   6.084 ns |   6.090 ns |  1.24 |      64 B |
|              TypeMapperThreeParameters |        .NET Core 3.1 |  58.621 ns |  57.936 ns | 12.04 |      96 B |
|            DirectAccessThreeParameters |        .NET Core 3.1 |   5.863 ns |   5.836 ns |  1.19 |      64 B |
|                  TypeMapperTSTZFactory |        .NET Core 3.1 | 261.548 ns | 261.717 ns | 53.15 |      64 B |
|                DirectAccessTSTZFactory |        .NET Core 3.1 | 263.306 ns | 261.825 ns | 53.88 |      64 B |
|                TypeMapperParameterless | .NET Framework 4.7.2 |  60.992 ns |  60.646 ns | 12.42 |      96 B |
|              DirectAccessParameterless | .NET Framework 4.7.2 |   4.921 ns |   4.922 ns |  1.00 |      64 B |
|           TypeMapperOneParameterString | .NET Framework 4.7.2 |  67.330 ns |  66.689 ns | 13.87 |      96 B |
|         DirectAccessOneParameterString | .NET Framework 4.7.2 |  10.229 ns |  10.237 ns |  2.08 |      64 B |
|   TypeMapperOneParameterTimeSpanUnwrap | .NET Framework 4.7.2 |  18.499 ns |  18.503 ns |  3.76 |      64 B |
| DirectAccessOneParameterTimeSpanUnwrap | .NET Framework 4.7.2 |  10.253 ns |  10.128 ns |  2.11 |      64 B |
|       TypeMapperTwoParametersIntString | .NET Framework 4.7.2 |  66.052 ns |  66.219 ns | 13.42 |      96 B |
|     DirectAccessTwoParametersIntString | .NET Framework 4.7.2 |  10.157 ns |  10.152 ns |  2.07 |      64 B |
|    TypeMapperTwoParametersStringString | .NET Framework 4.7.2 |  68.084 ns |  68.371 ns | 13.88 |      96 B |
|  DirectAccessTwoParametersStringString | .NET Framework 4.7.2 |  10.313 ns |  10.310 ns |  2.10 |      64 B |
|     TypeMapperTwoParametersWrapperEnum | .NET Framework 4.7.2 |  85.950 ns |  86.099 ns | 17.42 |      96 B |
|   DirectAccessTwoParametersWrapperEnum | .NET Framework 4.7.2 |   5.186 ns |   5.187 ns |  1.05 |      64 B |
|   TypeMapperTwoParametersWrapperString | .NET Framework 4.7.2 |  68.608 ns |  68.253 ns | 13.96 |      96 B |
| DirectAccessTwoParametersWrapperString | .NET Framework 4.7.2 |  10.157 ns |  10.160 ns |  2.06 |      64 B |
|              TypeMapperThreeParameters | .NET Framework 4.7.2 |  89.049 ns |  89.158 ns | 18.09 |      96 B |
|            DirectAccessThreeParameters | .NET Framework 4.7.2 |   5.538 ns |   5.458 ns |  1.13 |      64 B |
|                  TypeMapperTSTZFactory | .NET Framework 4.7.2 | 285.090 ns | 285.021 ns | 57.94 |      64 B |
|                DirectAccessTSTZFactory | .NET Framework 4.7.2 | 270.012 ns | 266.292 ns | 55.63 |      64 B |
