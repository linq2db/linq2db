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
|                TypeMapperParameterless |             .NET 5.0 |  43.998 ns |  43.874 ns |  9.32 |      96 B |
|              DirectAccessParameterless |             .NET 5.0 |   5.733 ns |   5.685 ns |  1.19 |      64 B |
|           TypeMapperOneParameterString |             .NET 5.0 |  41.402 ns |  41.049 ns |  8.68 |      96 B |
|         DirectAccessOneParameterString |             .NET 5.0 |   5.876 ns |   5.832 ns |  1.24 |      64 B |
|   TypeMapperOneParameterTimeSpanUnwrap |             .NET 5.0 |   6.480 ns |   6.493 ns |  1.35 |      64 B |
| DirectAccessOneParameterTimeSpanUnwrap |             .NET 5.0 |   5.759 ns |   5.695 ns |  1.21 |      64 B |
|       TypeMapperTwoParametersIntString |             .NET 5.0 |  41.589 ns |  41.210 ns |  8.77 |      96 B |
|     DirectAccessTwoParametersIntString |             .NET 5.0 |   5.549 ns |   5.553 ns |  1.15 |      64 B |
|    TypeMapperTwoParametersStringString |             .NET 5.0 |  44.821 ns |  44.564 ns |  9.47 |      96 B |
|  DirectAccessTwoParametersStringString |             .NET 5.0 |   6.184 ns |   6.192 ns |  1.31 |      64 B |
|     TypeMapperTwoParametersWrapperEnum |             .NET 5.0 |  54.123 ns |  54.078 ns | 11.45 |      96 B |
|   DirectAccessTwoParametersWrapperEnum |             .NET 5.0 |   5.713 ns |   5.686 ns |  1.18 |      64 B |
|   TypeMapperTwoParametersWrapperString |             .NET 5.0 |  46.533 ns |  45.981 ns | 10.08 |      96 B |
| DirectAccessTwoParametersWrapperString |             .NET 5.0 |   5.854 ns |   5.685 ns |  1.25 |      64 B |
|              TypeMapperThreeParameters |             .NET 5.0 |  55.102 ns |  54.736 ns | 11.62 |      96 B |
|            DirectAccessThreeParameters |             .NET 5.0 |   6.068 ns |   6.033 ns |  1.26 |      64 B |
|                  TypeMapperTSTZFactory |             .NET 5.0 | 217.774 ns | 213.778 ns | 45.85 |      64 B |
|                DirectAccessTSTZFactory |             .NET 5.0 | 216.320 ns | 215.925 ns | 45.81 |      64 B |
|                TypeMapperParameterless |        .NET Core 3.1 |  44.502 ns |  44.464 ns |  9.24 |      96 B |
|              DirectAccessParameterless |        .NET Core 3.1 |   5.687 ns |   5.683 ns |  1.18 |      64 B |
|           TypeMapperOneParameterString |        .NET Core 3.1 |  46.475 ns |  46.176 ns |  9.81 |      96 B |
|         DirectAccessOneParameterString |        .NET Core 3.1 |   6.406 ns |   6.306 ns |  1.37 |      64 B |
|   TypeMapperOneParameterTimeSpanUnwrap |        .NET Core 3.1 |   6.945 ns |   6.827 ns |  1.46 |      64 B |
| DirectAccessOneParameterTimeSpanUnwrap |        .NET Core 3.1 |   6.180 ns |   6.217 ns |  1.31 |      64 B |
|       TypeMapperTwoParametersIntString |        .NET Core 3.1 |  45.655 ns |  45.153 ns |  9.62 |      96 B |
|     DirectAccessTwoParametersIntString |        .NET Core 3.1 |   5.775 ns |   5.695 ns |  1.20 |      64 B |
|    TypeMapperTwoParametersStringString |        .NET Core 3.1 |  45.466 ns |  44.788 ns |  9.59 |      96 B |
|  DirectAccessTwoParametersStringString |        .NET Core 3.1 |   6.077 ns |   6.065 ns |  1.26 |      64 B |
|     TypeMapperTwoParametersWrapperEnum |        .NET Core 3.1 |  57.944 ns |  57.873 ns | 12.05 |      96 B |
|   DirectAccessTwoParametersWrapperEnum |        .NET Core 3.1 |   5.889 ns |   5.849 ns |  1.23 |      64 B |
|   TypeMapperTwoParametersWrapperString |        .NET Core 3.1 |  46.757 ns |  46.619 ns |  9.71 |      96 B |
| DirectAccessTwoParametersWrapperString |        .NET Core 3.1 |   6.293 ns |   6.174 ns |  1.33 |      64 B |
|              TypeMapperThreeParameters |        .NET Core 3.1 |  61.712 ns |  61.743 ns | 13.08 |      96 B |
|            DirectAccessThreeParameters |        .NET Core 3.1 |   6.088 ns |   6.064 ns |  1.27 |      64 B |
|                  TypeMapperTSTZFactory |        .NET Core 3.1 | 252.127 ns | 252.391 ns | 52.44 |      64 B |
|                DirectAccessTSTZFactory |        .NET Core 3.1 | 256.429 ns | 256.719 ns | 53.23 |      64 B |
|                TypeMapperParameterless | .NET Framework 4.7.2 |  62.530 ns |  62.526 ns | 12.98 |      96 B |
|              DirectAccessParameterless | .NET Framework 4.7.2 |   4.761 ns |   4.810 ns |  1.00 |      64 B |
|           TypeMapperOneParameterString | .NET Framework 4.7.2 |  67.803 ns |  67.732 ns | 14.15 |      96 B |
|         DirectAccessOneParameterString | .NET Framework 4.7.2 |  11.103 ns |  10.891 ns |  2.34 |      64 B |
|   TypeMapperOneParameterTimeSpanUnwrap | .NET Framework 4.7.2 |  19.845 ns |  19.748 ns |  4.24 |      64 B |
| DirectAccessOneParameterTimeSpanUnwrap | .NET Framework 4.7.2 |  11.100 ns |  10.967 ns |  2.37 |      64 B |
|       TypeMapperTwoParametersIntString | .NET Framework 4.7.2 |  70.541 ns |  69.883 ns | 14.87 |      96 B |
|     DirectAccessTwoParametersIntString | .NET Framework 4.7.2 |  10.882 ns |  10.765 ns |  2.31 |      64 B |
|    TypeMapperTwoParametersStringString | .NET Framework 4.7.2 |  67.310 ns |  66.443 ns | 14.24 |      96 B |
|  DirectAccessTwoParametersStringString | .NET Framework 4.7.2 |  10.567 ns |  10.492 ns |  2.19 |      64 B |
|     TypeMapperTwoParametersWrapperEnum | .NET Framework 4.7.2 |  90.862 ns |  90.664 ns | 18.92 |      96 B |
|   DirectAccessTwoParametersWrapperEnum | .NET Framework 4.7.2 |   5.937 ns |   5.830 ns |  1.26 |      64 B |
|   TypeMapperTwoParametersWrapperString | .NET Framework 4.7.2 |  72.130 ns |  71.327 ns | 15.43 |      96 B |
| DirectAccessTwoParametersWrapperString | .NET Framework 4.7.2 |  10.655 ns |  10.583 ns |  2.25 |      64 B |
|              TypeMapperThreeParameters | .NET Framework 4.7.2 |  89.318 ns |  88.677 ns | 18.92 |      96 B |
|            DirectAccessThreeParameters | .NET Framework 4.7.2 |   6.248 ns |   6.220 ns |  1.32 |      64 B |
|                  TypeMapperTSTZFactory | .NET Framework 4.7.2 | 291.708 ns | 292.776 ns | 60.57 |      64 B |
|                DirectAccessTSTZFactory | .NET Framework 4.7.2 | 271.765 ns | 270.581 ns | 56.42 |      64 B |
