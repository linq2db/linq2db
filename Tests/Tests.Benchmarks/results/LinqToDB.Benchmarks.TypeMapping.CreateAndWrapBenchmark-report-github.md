``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-XCPGVR : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-RHOQGE : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-WEVYVV : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-ORXRGX : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                                 Method |              Runtime |       Mean |     Median | Ratio |   Gen0 | Allocated | Alloc Ratio |
|--------------------------------------- |--------------------- |-----------:|-----------:|------:|-------:|----------:|------------:|
|                TypeMapperParameterless |             .NET 6.0 |  46.781 ns |  47.737 ns | 14.50 | 0.0057 |      96 B |        1.50 |
|              DirectAccessParameterless |             .NET 6.0 |   7.524 ns |   7.468 ns |  2.34 | 0.0038 |      64 B |        1.00 |
|           TypeMapperOneParameterString |             .NET 6.0 |  47.710 ns |  47.422 ns | 14.86 | 0.0057 |      96 B |        1.50 |
|         DirectAccessOneParameterString |             .NET 6.0 |   7.731 ns |   7.672 ns |  2.41 | 0.0038 |      64 B |        1.00 |
|   TypeMapperOneParameterTimeSpanUnwrap |             .NET 6.0 |   8.559 ns |   8.650 ns |  2.66 | 0.0038 |      64 B |        1.00 |
| DirectAccessOneParameterTimeSpanUnwrap |             .NET 6.0 |   3.410 ns |   3.409 ns |  1.06 | 0.0038 |      64 B |        1.00 |
|       TypeMapperTwoParametersIntString |             .NET 6.0 |  47.700 ns |  47.697 ns | 14.86 | 0.0057 |      96 B |        1.50 |
|     DirectAccessTwoParametersIntString |             .NET 6.0 |   7.581 ns |   7.638 ns |  2.36 | 0.0038 |      64 B |        1.00 |
|    TypeMapperTwoParametersStringString |             .NET 6.0 |  44.237 ns |  43.591 ns | 13.78 | 0.0057 |      96 B |        1.50 |
|  DirectAccessTwoParametersStringString |             .NET 6.0 |   6.916 ns |   7.849 ns |  1.78 | 0.0038 |      64 B |        1.00 |
|     TypeMapperTwoParametersWrapperEnum |             .NET 6.0 |  53.701 ns |  53.698 ns | 16.76 | 0.0057 |      96 B |        1.50 |
|   DirectAccessTwoParametersWrapperEnum |             .NET 6.0 |   7.235 ns |   7.233 ns |  2.26 | 0.0038 |      64 B |        1.00 |
|   TypeMapperTwoParametersWrapperString |             .NET 6.0 |  43.564 ns |  48.606 ns | 14.28 | 0.0057 |      96 B |        1.50 |
| DirectAccessTwoParametersWrapperString |             .NET 6.0 |   7.716 ns |   7.699 ns |  2.41 | 0.0038 |      64 B |        1.00 |
|              TypeMapperThreeParameters |             .NET 6.0 |  56.118 ns |  55.770 ns | 17.48 | 0.0057 |      96 B |        1.50 |
|            DirectAccessThreeParameters |             .NET 6.0 |   7.523 ns |   7.523 ns |  2.34 | 0.0038 |      64 B |        1.00 |
|                  TypeMapperTSTZFactory |             .NET 6.0 | 158.677 ns | 158.674 ns | 49.52 | 0.0038 |      64 B |        1.00 |
|                DirectAccessTSTZFactory |             .NET 6.0 |  72.086 ns |  72.083 ns | 22.48 | 0.0038 |      64 B |        1.00 |
|                TypeMapperParameterless |             .NET 7.0 |  43.239 ns |  45.811 ns | 12.48 | 0.0057 |      96 B |        1.50 |
|              DirectAccessParameterless |             .NET 7.0 |   4.043 ns |   4.044 ns |  1.26 | 0.0038 |      64 B |        1.00 |
|           TypeMapperOneParameterString |             .NET 7.0 |  46.756 ns |  46.861 ns | 14.56 | 0.0057 |      96 B |        1.50 |
|         DirectAccessOneParameterString |             .NET 7.0 |   9.128 ns |   9.125 ns |  2.85 | 0.0038 |      64 B |        1.00 |
|   TypeMapperOneParameterTimeSpanUnwrap |             .NET 7.0 |  10.060 ns |  10.067 ns |  3.14 | 0.0038 |      64 B |        1.00 |
| DirectAccessOneParameterTimeSpanUnwrap |             .NET 7.0 |   7.410 ns |   8.381 ns |  2.02 | 0.0038 |      64 B |        1.00 |
|       TypeMapperTwoParametersIntString |             .NET 7.0 |  46.406 ns |  46.407 ns | 14.47 | 0.0057 |      96 B |        1.50 |
|     DirectAccessTwoParametersIntString |             .NET 7.0 |   8.076 ns |   8.129 ns |  2.52 | 0.0038 |      64 B |        1.00 |
|    TypeMapperTwoParametersStringString |             .NET 7.0 |  47.145 ns |  47.105 ns | 14.69 | 0.0057 |      96 B |        1.50 |
|  DirectAccessTwoParametersStringString |             .NET 7.0 |   8.950 ns |   8.957 ns |  2.79 | 0.0038 |      64 B |        1.00 |
|     TypeMapperTwoParametersWrapperEnum |             .NET 7.0 |  59.821 ns |  60.105 ns | 18.63 | 0.0057 |      96 B |        1.50 |
|   DirectAccessTwoParametersWrapperEnum |             .NET 7.0 |   9.149 ns |   9.157 ns |  2.85 | 0.0038 |      64 B |        1.00 |
|   TypeMapperTwoParametersWrapperString |             .NET 7.0 |  43.117 ns |  42.932 ns | 13.43 | 0.0057 |      96 B |        1.50 |
| DirectAccessTwoParametersWrapperString |             .NET 7.0 |   9.319 ns |   9.290 ns |  2.91 | 0.0038 |      64 B |        1.00 |
|              TypeMapperThreeParameters |             .NET 7.0 |  58.008 ns |  57.839 ns | 18.07 | 0.0057 |      96 B |        1.50 |
|            DirectAccessThreeParameters |             .NET 7.0 |   9.256 ns |  11.755 ns |  2.81 | 0.0038 |      64 B |        1.00 |
|                  TypeMapperTSTZFactory |             .NET 7.0 | 148.210 ns | 148.209 ns | 46.22 | 0.0038 |      64 B |        1.00 |
|                DirectAccessTSTZFactory |             .NET 7.0 | 134.638 ns | 133.767 ns | 42.17 | 0.0038 |      64 B |        1.00 |
|                TypeMapperParameterless |        .NET Core 3.1 |  51.873 ns |  52.515 ns | 16.17 | 0.0057 |      96 B |        1.50 |
|              DirectAccessParameterless |        .NET Core 3.1 |  10.556 ns |  10.746 ns |  3.27 | 0.0038 |      64 B |        1.00 |
|           TypeMapperOneParameterString |        .NET Core 3.1 |  52.514 ns |  52.525 ns | 16.38 | 0.0057 |      96 B |        1.50 |
|         DirectAccessOneParameterString |        .NET Core 3.1 |   7.801 ns |   7.864 ns |  2.43 | 0.0038 |      64 B |        1.00 |
|   TypeMapperOneParameterTimeSpanUnwrap |        .NET Core 3.1 |   7.733 ns |   8.595 ns |  1.94 | 0.0038 |      64 B |        1.00 |
| DirectAccessOneParameterTimeSpanUnwrap |        .NET Core 3.1 |   7.876 ns |  10.503 ns |  2.48 | 0.0038 |      64 B |        1.00 |
|       TypeMapperTwoParametersIntString |        .NET Core 3.1 |  54.997 ns |  54.183 ns | 17.13 | 0.0057 |      96 B |        1.50 |
|     DirectAccessTwoParametersIntString |        .NET Core 3.1 |   7.602 ns |   7.651 ns |  2.37 | 0.0038 |      64 B |        1.00 |
|    TypeMapperTwoParametersStringString |        .NET Core 3.1 |  49.251 ns |  48.928 ns | 15.35 | 0.0057 |      96 B |        1.50 |
|  DirectAccessTwoParametersStringString |        .NET Core 3.1 |  11.315 ns |  11.417 ns |  3.46 | 0.0038 |      64 B |        1.00 |
|     TypeMapperTwoParametersWrapperEnum |        .NET Core 3.1 |  66.680 ns |  66.589 ns | 20.76 | 0.0057 |      96 B |        1.50 |
|   DirectAccessTwoParametersWrapperEnum |        .NET Core 3.1 |   6.653 ns |   6.547 ns |  2.07 | 0.0038 |      64 B |        1.00 |
|   TypeMapperTwoParametersWrapperString |        .NET Core 3.1 |  52.732 ns |  53.130 ns | 16.44 | 0.0057 |      96 B |        1.50 |
| DirectAccessTwoParametersWrapperString |        .NET Core 3.1 |   6.763 ns |   7.967 ns |  2.37 | 0.0038 |      64 B |        1.00 |
|              TypeMapperThreeParameters |        .NET Core 3.1 |  64.845 ns |  65.456 ns | 20.16 | 0.0057 |      96 B |        1.50 |
|            DirectAccessThreeParameters |        .NET Core 3.1 |   7.976 ns |   7.987 ns |  2.49 | 0.0038 |      64 B |        1.00 |
|                  TypeMapperTSTZFactory |        .NET Core 3.1 | 280.726 ns | 280.672 ns | 87.54 | 0.0038 |      64 B |        1.00 |
|                DirectAccessTSTZFactory |        .NET Core 3.1 | 271.025 ns | 267.887 ns | 84.74 | 0.0038 |      64 B |        1.00 |
|                TypeMapperParameterless | .NET Framework 4.7.2 |  75.367 ns |  75.684 ns | 23.47 | 0.0153 |      96 B |        1.50 |
|              DirectAccessParameterless | .NET Framework 4.7.2 |   3.212 ns |   3.236 ns |  1.00 | 0.0102 |      64 B |        1.00 |
|           TypeMapperOneParameterString | .NET Framework 4.7.2 |  75.921 ns |  76.507 ns | 23.64 | 0.0153 |      96 B |        1.50 |
|         DirectAccessOneParameterString | .NET Framework 4.7.2 |   7.823 ns |   8.106 ns |  2.41 | 0.0102 |      64 B |        1.00 |
|   TypeMapperOneParameterTimeSpanUnwrap | .NET Framework 4.7.2 |  17.314 ns |  17.150 ns |  5.37 | 0.0102 |      64 B |        1.00 |
| DirectAccessOneParameterTimeSpanUnwrap | .NET Framework 4.7.2 |   8.236 ns |   8.275 ns |  2.57 | 0.0102 |      64 B |        1.00 |
|       TypeMapperTwoParametersIntString | .NET Framework 4.7.2 |  74.058 ns |  75.245 ns | 23.08 | 0.0153 |      96 B |        1.50 |
|     DirectAccessTwoParametersIntString | .NET Framework 4.7.2 |   8.708 ns |   8.636 ns |  2.72 | 0.0102 |      64 B |        1.00 |
|    TypeMapperTwoParametersStringString | .NET Framework 4.7.2 |  74.612 ns |  74.071 ns | 23.24 | 0.0153 |      96 B |        1.50 |
|  DirectAccessTwoParametersStringString | .NET Framework 4.7.2 |   8.383 ns |   8.427 ns |  2.61 | 0.0102 |      64 B |        1.00 |
|     TypeMapperTwoParametersWrapperEnum | .NET Framework 4.7.2 |  79.220 ns |  97.720 ns | 26.53 | 0.0153 |      96 B |        1.50 |
|   DirectAccessTwoParametersWrapperEnum | .NET Framework 4.7.2 |   7.017 ns |   7.640 ns |  1.99 | 0.0102 |      64 B |        1.00 |
|   TypeMapperTwoParametersWrapperString | .NET Framework 4.7.2 |  74.710 ns |  74.713 ns | 23.28 | 0.0153 |      96 B |        1.50 |
| DirectAccessTwoParametersWrapperString | .NET Framework 4.7.2 |   8.152 ns |   8.163 ns |  2.54 | 0.0102 |      64 B |        1.00 |
|              TypeMapperThreeParameters | .NET Framework 4.7.2 |  98.531 ns |  99.093 ns | 30.69 | 0.0153 |      96 B |        1.50 |
|            DirectAccessThreeParameters | .NET Framework 4.7.2 |   7.428 ns |   7.558 ns |  2.31 | 0.0102 |      64 B |        1.00 |
|                  TypeMapperTSTZFactory | .NET Framework 4.7.2 | 311.306 ns | 306.328 ns | 97.43 | 0.0100 |      64 B |        1.00 |
|                DirectAccessTSTZFactory | .NET Framework 4.7.2 | 299.724 ns | 297.137 ns | 93.34 | 0.0100 |      64 B |        1.00 |
