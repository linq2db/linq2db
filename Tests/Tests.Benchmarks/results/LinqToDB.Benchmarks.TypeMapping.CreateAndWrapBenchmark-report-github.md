``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-RNZPMW : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-XCCWXF : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-WSMVMG : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-FMTKFQ : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                                 Method |              Runtime |       Mean |     Median |  Ratio |   Gen0 | Allocated | Alloc Ratio |
|--------------------------------------- |--------------------- |-----------:|-----------:|-------:|-------:|----------:|------------:|
|                TypeMapperParameterless |             .NET 6.0 |  58.590 ns |  61.097 ns |  27.78 | 0.0057 |      96 B |        1.50 |
|              DirectAccessParameterless |             .NET 6.0 |  13.540 ns |  14.187 ns |   8.09 | 0.0038 |      64 B |        1.00 |
|           TypeMapperOneParameterString |             .NET 6.0 |  63.351 ns |  66.066 ns |  34.74 | 0.0057 |      96 B |        1.50 |
|         DirectAccessOneParameterString |             .NET 6.0 |  10.893 ns |  12.098 ns |   6.97 | 0.0038 |      64 B |        1.00 |
|   TypeMapperOneParameterTimeSpanUnwrap |             .NET 6.0 |  11.578 ns |  12.342 ns |   7.08 | 0.0038 |      64 B |        1.00 |
| DirectAccessOneParameterTimeSpanUnwrap |             .NET 6.0 |  12.567 ns |  12.770 ns |   7.46 | 0.0038 |      64 B |        1.00 |
|       TypeMapperTwoParametersIntString |             .NET 6.0 |  61.819 ns |  61.110 ns |  41.20 | 0.0057 |      96 B |        1.50 |
|     DirectAccessTwoParametersIntString |             .NET 6.0 |  11.986 ns |  12.213 ns |   7.67 | 0.0038 |      64 B |        1.00 |
|    TypeMapperTwoParametersStringString |             .NET 6.0 |  57.067 ns |  59.386 ns |  30.90 | 0.0057 |      96 B |        1.50 |
|  DirectAccessTwoParametersStringString |             .NET 6.0 |  11.468 ns |  11.831 ns |   5.51 | 0.0038 |      64 B |        1.00 |
|     TypeMapperTwoParametersWrapperEnum |             .NET 6.0 |  66.939 ns |  67.965 ns |  36.93 | 0.0057 |      96 B |        1.50 |
|   DirectAccessTwoParametersWrapperEnum |             .NET 6.0 |  12.503 ns |  12.837 ns |   7.09 | 0.0038 |      64 B |        1.00 |
|   TypeMapperTwoParametersWrapperString |             .NET 6.0 |  61.748 ns |  63.988 ns |  27.21 | 0.0057 |      96 B |        1.50 |
| DirectAccessTwoParametersWrapperString |             .NET 6.0 |  11.220 ns |  12.169 ns |   5.59 | 0.0038 |      64 B |        1.00 |
|              TypeMapperThreeParameters |             .NET 6.0 |  69.192 ns |  71.572 ns |  35.75 | 0.0057 |      96 B |        1.50 |
|            DirectAccessThreeParameters |             .NET 6.0 |  12.240 ns |  12.658 ns |   5.48 | 0.0038 |      64 B |        1.00 |
|                  TypeMapperTSTZFactory |             .NET 6.0 | 172.113 ns | 176.648 ns |  92.00 | 0.0038 |      64 B |        1.00 |
|                DirectAccessTSTZFactory |             .NET 6.0 | 167.623 ns | 167.342 ns |  98.62 | 0.0038 |      64 B |        1.00 |
|                TypeMapperParameterless |             .NET 7.0 |  56.554 ns |  56.176 ns |  29.44 | 0.0057 |      96 B |        1.50 |
|              DirectAccessParameterless |             .NET 7.0 |  12.430 ns |  12.799 ns |   5.61 | 0.0038 |      64 B |        1.00 |
|           TypeMapperOneParameterString |             .NET 7.0 |  53.750 ns |  56.411 ns |  27.56 | 0.0057 |      96 B |        1.50 |
|         DirectAccessOneParameterString |             .NET 7.0 |  14.636 ns |  14.736 ns |   8.60 | 0.0038 |      64 B |        1.00 |
|   TypeMapperOneParameterTimeSpanUnwrap |             .NET 7.0 |  16.168 ns |  16.866 ns |   8.92 | 0.0038 |      64 B |        1.00 |
| DirectAccessOneParameterTimeSpanUnwrap |             .NET 7.0 |  15.620 ns |  15.487 ns |   8.32 | 0.0038 |      64 B |        1.00 |
|       TypeMapperTwoParametersIntString |             .NET 7.0 |  63.610 ns |  65.948 ns |  33.72 | 0.0057 |      96 B |        1.50 |
|     DirectAccessTwoParametersIntString |             .NET 7.0 |  17.057 ns |  17.556 ns |  10.43 | 0.0038 |      64 B |        1.00 |
|    TypeMapperTwoParametersStringString |             .NET 7.0 |  60.853 ns |  63.614 ns |  23.01 | 0.0057 |      96 B |        1.50 |
|  DirectAccessTwoParametersStringString |             .NET 7.0 |  14.450 ns |  15.771 ns |   5.76 | 0.0038 |      64 B |        1.00 |
|     TypeMapperTwoParametersWrapperEnum |             .NET 7.0 |  82.429 ns |  86.482 ns |  42.07 | 0.0057 |      96 B |        1.50 |
|   DirectAccessTwoParametersWrapperEnum |             .NET 7.0 |  16.143 ns |  16.893 ns |  10.00 | 0.0038 |      64 B |        1.00 |
|   TypeMapperTwoParametersWrapperString |             .NET 7.0 |  61.139 ns |  64.090 ns |  32.63 | 0.0057 |      96 B |        1.50 |
| DirectAccessTwoParametersWrapperString |             .NET 7.0 |  15.478 ns |  16.679 ns |   8.96 | 0.0038 |      64 B |        1.00 |
|              TypeMapperThreeParameters |             .NET 7.0 |  75.679 ns |  79.870 ns |  39.40 | 0.0057 |      96 B |        1.50 |
|            DirectAccessThreeParameters |             .NET 7.0 |  15.994 ns |  16.674 ns |   5.83 | 0.0038 |      64 B |        1.00 |
|                  TypeMapperTSTZFactory |             .NET 7.0 | 162.476 ns | 174.488 ns |  98.08 | 0.0038 |      64 B |        1.00 |
|                DirectAccessTSTZFactory |             .NET 7.0 | 158.431 ns | 168.466 ns |  91.96 | 0.0038 |      64 B |        1.00 |
|                TypeMapperParameterless |        .NET Core 3.1 |  66.557 ns |  71.673 ns |  37.56 | 0.0057 |      96 B |        1.50 |
|              DirectAccessParameterless |        .NET Core 3.1 |  12.052 ns |  12.782 ns |   6.13 | 0.0038 |      64 B |        1.00 |
|           TypeMapperOneParameterString |        .NET Core 3.1 |  65.339 ns |  67.939 ns |  28.84 | 0.0057 |      96 B |        1.50 |
|         DirectAccessOneParameterString |        .NET Core 3.1 |  11.382 ns |  12.002 ns |   4.01 | 0.0038 |      64 B |        1.00 |
|   TypeMapperOneParameterTimeSpanUnwrap |        .NET Core 3.1 |  12.249 ns |  12.803 ns |   6.42 | 0.0038 |      64 B |        1.00 |
| DirectAccessOneParameterTimeSpanUnwrap |        .NET Core 3.1 |  11.629 ns |  12.154 ns |   7.11 | 0.0038 |      64 B |        1.00 |
|       TypeMapperTwoParametersIntString |        .NET Core 3.1 |  67.353 ns |  73.687 ns |  26.97 | 0.0057 |      96 B |        1.50 |
|     DirectAccessTwoParametersIntString |        .NET Core 3.1 |  11.373 ns |  12.186 ns |   3.91 | 0.0038 |      64 B |        1.00 |
|    TypeMapperTwoParametersStringString |        .NET Core 3.1 |  66.145 ns |  72.629 ns |  27.59 | 0.0057 |      96 B |        1.50 |
|  DirectAccessTwoParametersStringString |        .NET Core 3.1 |  12.490 ns |  13.209 ns |   6.94 | 0.0038 |      64 B |        1.00 |
|     TypeMapperTwoParametersWrapperEnum |        .NET Core 3.1 |  77.344 ns |  79.728 ns |  44.79 | 0.0057 |      96 B |        1.50 |
|   DirectAccessTwoParametersWrapperEnum |        .NET Core 3.1 |  11.376 ns |  11.638 ns |   6.04 | 0.0038 |      64 B |        1.00 |
|   TypeMapperTwoParametersWrapperString |        .NET Core 3.1 |  65.758 ns |  65.869 ns |  31.15 | 0.0057 |      96 B |        1.50 |
| DirectAccessTwoParametersWrapperString |        .NET Core 3.1 |  10.674 ns |  11.350 ns |   3.83 | 0.0038 |      64 B |        1.00 |
|              TypeMapperThreeParameters |        .NET Core 3.1 |  78.195 ns |  83.009 ns |  31.08 | 0.0057 |      96 B |        1.50 |
|            DirectAccessThreeParameters |        .NET Core 3.1 |  21.012 ns |  22.538 ns |  11.64 | 0.0038 |      64 B |        1.00 |
|                  TypeMapperTSTZFactory |        .NET Core 3.1 | 315.898 ns | 333.570 ns | 178.16 | 0.0038 |      64 B |        1.00 |
|                DirectAccessTSTZFactory |        .NET Core 3.1 | 283.127 ns | 304.842 ns | 171.85 | 0.0038 |      64 B |        1.00 |
|                TypeMapperParameterless | .NET Framework 4.7.2 |  85.876 ns |  87.597 ns |  44.90 | 0.0153 |      96 B |        1.50 |
|              DirectAccessParameterless | .NET Framework 4.7.2 |   7.768 ns |   8.527 ns |   1.00 | 0.0102 |      64 B |        1.00 |
|           TypeMapperOneParameterString | .NET Framework 4.7.2 |  88.176 ns |  93.494 ns |  46.59 | 0.0153 |      96 B |        1.50 |
|         DirectAccessOneParameterString | .NET Framework 4.7.2 |  10.319 ns |  10.887 ns |   6.55 | 0.0102 |      64 B |        1.00 |
|   TypeMapperOneParameterTimeSpanUnwrap | .NET Framework 4.7.2 |  20.580 ns |  20.940 ns |  10.28 | 0.0102 |      64 B |        1.00 |
| DirectAccessOneParameterTimeSpanUnwrap | .NET Framework 4.7.2 |   7.633 ns |   8.609 ns |   4.63 | 0.0102 |      64 B |        1.00 |
|       TypeMapperTwoParametersIntString | .NET Framework 4.7.2 |  82.944 ns |  87.759 ns |  32.20 | 0.0153 |      96 B |        1.50 |
|     DirectAccessTwoParametersIntString | .NET Framework 4.7.2 |   9.869 ns |  11.333 ns |   6.47 | 0.0102 |      64 B |        1.00 |
|    TypeMapperTwoParametersStringString | .NET Framework 4.7.2 |  87.595 ns |  90.407 ns |  42.20 | 0.0153 |      96 B |        1.50 |
|  DirectAccessTwoParametersStringString | .NET Framework 4.7.2 |   9.567 ns |   9.930 ns |   5.29 | 0.0102 |      64 B |        1.00 |
|     TypeMapperTwoParametersWrapperEnum | .NET Framework 4.7.2 | 112.928 ns | 115.223 ns |  65.23 | 0.0153 |      96 B |        1.50 |
|   DirectAccessTwoParametersWrapperEnum | .NET Framework 4.7.2 |   8.231 ns |   8.713 ns |   4.37 | 0.0102 |      64 B |        1.00 |
|   TypeMapperTwoParametersWrapperString | .NET Framework 4.7.2 |  82.250 ns |  84.699 ns |  43.19 | 0.0153 |      96 B |        1.50 |
| DirectAccessTwoParametersWrapperString | .NET Framework 4.7.2 |   8.646 ns |   9.141 ns |   4.83 | 0.0102 |      64 B |        1.00 |
|              TypeMapperThreeParameters | .NET Framework 4.7.2 | 123.374 ns | 126.261 ns |  70.27 | 0.0153 |      96 B |        1.50 |
|            DirectAccessThreeParameters | .NET Framework 4.7.2 |   8.927 ns |   9.305 ns |   4.93 | 0.0102 |      64 B |        1.00 |
|                  TypeMapperTSTZFactory | .NET Framework 4.7.2 | 312.399 ns | 325.916 ns | 175.82 | 0.0100 |      64 B |        1.00 |
|                DirectAccessTSTZFactory | .NET Framework 4.7.2 | 314.840 ns | 327.578 ns | 175.59 | 0.0100 |      64 B |        1.00 |
