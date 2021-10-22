``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417994 Hz, Resolution=292.5693 ns, Timer=TSC
.NET SDK=5.0.402
  [Host]     : .NET 5.0.11 (5.0.1121.47308), X64 RyuJIT
  Job-ODZCDL : .NET 5.0.11 (5.0.1121.47308), X64 RyuJIT
  Job-PCJJBI : .NET Core 3.1.20 (CoreCLR 4.700.21.47003, CoreFX 4.700.21.47101), X64 RyuJIT
  Job-HHEMGO : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT

Jit=RyuJit  Platform=X64  

```
|                                 Method |              Runtime |       Mean |     Median | Ratio | Allocated |
|--------------------------------------- |--------------------- |-----------:|-----------:|------:|----------:|
|                TypeMapperParameterless |             .NET 5.0 |  41.043 ns |  41.031 ns |  8.54 |      96 B |
|              DirectAccessParameterless |             .NET 5.0 |   5.423 ns |   5.422 ns |  1.13 |      64 B |
|           TypeMapperOneParameterString |             .NET 5.0 |  41.360 ns |  40.776 ns |  8.70 |      96 B |
|         DirectAccessOneParameterString |             .NET 5.0 |   5.902 ns |   5.898 ns |  1.23 |      64 B |
|   TypeMapperOneParameterTimeSpanUnwrap |             .NET 5.0 |   6.214 ns |   6.211 ns |  1.29 |      64 B |
| DirectAccessOneParameterTimeSpanUnwrap |             .NET 5.0 |   5.658 ns |   5.647 ns |  1.18 |      64 B |
|       TypeMapperTwoParametersIntString |             .NET 5.0 |  39.167 ns |  38.827 ns |  8.20 |      96 B |
|     DirectAccessTwoParametersIntString |             .NET 5.0 |   6.005 ns |   5.991 ns |  1.25 |      64 B |
|    TypeMapperTwoParametersStringString |             .NET 5.0 |  83.553 ns |  82.779 ns | 17.39 |      96 B |
|  DirectAccessTwoParametersStringString |             .NET 5.0 |   5.588 ns |   5.577 ns |  1.16 |      64 B |
|     TypeMapperTwoParametersWrapperEnum |             .NET 5.0 |  52.556 ns |  52.204 ns | 10.97 |      96 B |
|   DirectAccessTwoParametersWrapperEnum |             .NET 5.0 |   5.739 ns |   5.653 ns |  1.22 |      64 B |
|   TypeMapperTwoParametersWrapperString |             .NET 5.0 |  41.996 ns |  41.779 ns |  8.74 |      96 B |
| DirectAccessTwoParametersWrapperString |             .NET 5.0 |   5.826 ns |   5.817 ns |  1.21 |      64 B |
|              TypeMapperThreeParameters |             .NET 5.0 |  52.182 ns |  51.990 ns | 10.87 |      96 B |
|            DirectAccessThreeParameters |             .NET 5.0 |   6.051 ns |   6.013 ns |  1.26 |      64 B |
|                  TypeMapperTSTZFactory |             .NET 5.0 | 218.179 ns | 218.374 ns | 45.39 |      64 B |
|                DirectAccessTSTZFactory |             .NET 5.0 | 217.857 ns | 217.512 ns | 45.29 |      64 B |
|                TypeMapperParameterless |        .NET Core 3.1 |  44.122 ns |  43.423 ns |  9.24 |      96 B |
|              DirectAccessParameterless |        .NET Core 3.1 |   5.878 ns |   5.871 ns |  1.22 |      64 B |
|           TypeMapperOneParameterString |        .NET Core 3.1 |  43.490 ns |  43.401 ns |  9.04 |      96 B |
|         DirectAccessOneParameterString |        .NET Core 3.1 |   5.778 ns |   5.778 ns |  1.20 |      64 B |
|   TypeMapperOneParameterTimeSpanUnwrap |        .NET Core 3.1 |   6.758 ns |   6.679 ns |  1.41 |      64 B |
| DirectAccessOneParameterTimeSpanUnwrap |        .NET Core 3.1 |   5.921 ns |   5.819 ns |  1.23 |      64 B |
|       TypeMapperTwoParametersIntString |        .NET Core 3.1 |  43.643 ns |  43.623 ns |  9.08 |      96 B |
|     DirectAccessTwoParametersIntString |        .NET Core 3.1 |   5.787 ns |   5.779 ns |  1.21 |      64 B |
|    TypeMapperTwoParametersStringString |        .NET Core 3.1 |  43.817 ns |  43.810 ns |  9.11 |      96 B |
|  DirectAccessTwoParametersStringString |        .NET Core 3.1 |   6.299 ns |   6.168 ns |  1.34 |      64 B |
|     TypeMapperTwoParametersWrapperEnum |        .NET Core 3.1 |  59.486 ns |  58.501 ns | 12.33 |      96 B |
|   DirectAccessTwoParametersWrapperEnum |        .NET Core 3.1 |   5.921 ns |   5.913 ns |  1.23 |      64 B |
|   TypeMapperTwoParametersWrapperString |        .NET Core 3.1 |  46.176 ns |  46.138 ns |  9.61 |      96 B |
| DirectAccessTwoParametersWrapperString |        .NET Core 3.1 |   6.304 ns |   6.308 ns |  1.31 |      64 B |
|              TypeMapperThreeParameters |        .NET Core 3.1 |  58.833 ns |  59.144 ns | 12.25 |      96 B |
|            DirectAccessThreeParameters |        .NET Core 3.1 |   6.006 ns |   5.999 ns |  1.25 |      64 B |
|                  TypeMapperTSTZFactory |        .NET Core 3.1 | 262.780 ns | 259.792 ns | 54.85 |      64 B |
|                DirectAccessTSTZFactory |        .NET Core 3.1 | 256.555 ns | 256.267 ns | 53.38 |      64 B |
|                TypeMapperParameterless | .NET Framework 4.7.2 |  61.432 ns |  61.416 ns | 12.78 |      96 B |
|              DirectAccessParameterless | .NET Framework 4.7.2 |   4.806 ns |   4.801 ns |  1.00 |      64 B |
|           TypeMapperOneParameterString | .NET Framework 4.7.2 |  64.611 ns |  64.027 ns | 13.52 |      96 B |
|         DirectAccessOneParameterString | .NET Framework 4.7.2 |  10.410 ns |  10.405 ns |  2.17 |      64 B |
|   TypeMapperOneParameterTimeSpanUnwrap | .NET Framework 4.7.2 |  19.534 ns |  19.518 ns |  4.06 |      64 B |
| DirectAccessOneParameterTimeSpanUnwrap | .NET Framework 4.7.2 |  10.565 ns |  10.567 ns |  2.20 |      64 B |
|       TypeMapperTwoParametersIntString | .NET Framework 4.7.2 |  65.529 ns |  65.448 ns | 13.63 |      96 B |
|     DirectAccessTwoParametersIntString | .NET Framework 4.7.2 |  10.256 ns |  10.259 ns |  2.13 |      64 B |
|    TypeMapperTwoParametersStringString | .NET Framework 4.7.2 |  63.808 ns |  63.778 ns | 13.27 |      96 B |
|  DirectAccessTwoParametersStringString | .NET Framework 4.7.2 |  10.530 ns |  10.529 ns |  2.19 |      64 B |
|     TypeMapperTwoParametersWrapperEnum | .NET Framework 4.7.2 |  85.471 ns |  85.487 ns | 17.78 |      96 B |
|   DirectAccessTwoParametersWrapperEnum | .NET Framework 4.7.2 |   5.613 ns |   5.625 ns |  1.17 |      64 B |
|   TypeMapperTwoParametersWrapperString | .NET Framework 4.7.2 |  65.585 ns |  64.924 ns | 13.71 |      96 B |
| DirectAccessTwoParametersWrapperString | .NET Framework 4.7.2 |  10.267 ns |  10.266 ns |  2.14 |      64 B |
|              TypeMapperThreeParameters | .NET Framework 4.7.2 |  85.634 ns |  85.096 ns | 17.82 |      96 B |
|            DirectAccessThreeParameters | .NET Framework 4.7.2 |   5.759 ns |   5.749 ns |  1.20 |      64 B |
|                  TypeMapperTSTZFactory | .NET Framework 4.7.2 | 291.613 ns | 291.081 ns | 60.67 |      64 B |
|                DirectAccessTSTZFactory | .NET Framework 4.7.2 | 280.639 ns | 276.757 ns | 59.45 |      64 B |
