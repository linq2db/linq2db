``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-WUZRIO : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-EMBONI : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-HZWTXS : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-VIGHHX : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                                 Method |              Runtime |       Mean |     Median | Ratio |   Gen0 | Allocated | Alloc Ratio |
|--------------------------------------- |--------------------- |-----------:|-----------:|------:|-------:|----------:|------------:|
|                TypeMapperParameterless |             .NET 6.0 |  43.339 ns |  43.189 ns |  6.02 | 0.0057 |      96 B |        1.50 |
|              DirectAccessParameterless |             .NET 6.0 |   6.579 ns |   6.530 ns |  0.91 | 0.0038 |      64 B |        1.00 |
|           TypeMapperOneParameterString |             .NET 6.0 |  50.132 ns |  50.577 ns |  6.91 | 0.0057 |      96 B |        1.50 |
|         DirectAccessOneParameterString |             .NET 6.0 |   7.515 ns |   7.512 ns |  1.05 | 0.0038 |      64 B |        1.00 |
|   TypeMapperOneParameterTimeSpanUnwrap |             .NET 6.0 |   8.574 ns |   8.427 ns |  1.21 | 0.0038 |      64 B |        1.00 |
| DirectAccessOneParameterTimeSpanUnwrap |             .NET 6.0 |   5.797 ns |   7.425 ns |  0.57 | 0.0038 |      64 B |        1.00 |
|       TypeMapperTwoParametersIntString |             .NET 6.0 |  45.391 ns |  47.587 ns |  5.92 | 0.0057 |      96 B |        1.50 |
|     DirectAccessTwoParametersIntString |             .NET 6.0 |   7.754 ns |   7.850 ns |  1.04 | 0.0038 |      64 B |        1.00 |
|    TypeMapperTwoParametersStringString |             .NET 6.0 |  48.306 ns |  48.054 ns |  6.80 | 0.0057 |      96 B |        1.50 |
|  DirectAccessTwoParametersStringString |             .NET 6.0 |   8.441 ns |  10.703 ns |  1.17 | 0.0038 |      64 B |        1.00 |
|     TypeMapperTwoParametersWrapperEnum |             .NET 6.0 |  57.084 ns |  57.082 ns |  8.07 | 0.0057 |      96 B |        1.50 |
|   DirectAccessTwoParametersWrapperEnum |             .NET 6.0 |   7.928 ns |   7.954 ns |  1.12 | 0.0038 |      64 B |        1.00 |
|   TypeMapperTwoParametersWrapperString |             .NET 6.0 |  48.409 ns |  48.370 ns |  6.75 | 0.0057 |      96 B |        1.50 |
| DirectAccessTwoParametersWrapperString |             .NET 6.0 |   7.795 ns |   7.794 ns |  1.11 | 0.0038 |      64 B |        1.00 |
|              TypeMapperThreeParameters |             .NET 6.0 |  63.665 ns |  63.468 ns |  8.87 | 0.0057 |      96 B |        1.50 |
|            DirectAccessThreeParameters |             .NET 6.0 |   7.719 ns |   7.720 ns |  1.09 | 0.0038 |      64 B |        1.00 |
|                  TypeMapperTSTZFactory |             .NET 6.0 | 168.620 ns | 168.569 ns | 23.50 | 0.0038 |      64 B |        1.00 |
|                DirectAccessTSTZFactory |             .NET 6.0 | 161.932 ns | 162.773 ns | 22.80 | 0.0038 |      64 B |        1.00 |
|                TypeMapperParameterless |             .NET 7.0 |  44.952 ns |  44.963 ns |  6.36 | 0.0057 |      96 B |        1.50 |
|              DirectAccessParameterless |             .NET 7.0 |   8.987 ns |   8.948 ns |  1.27 | 0.0038 |      64 B |        1.00 |
|           TypeMapperOneParameterString |             .NET 7.0 |  46.545 ns |  46.647 ns |  6.56 | 0.0057 |      96 B |        1.50 |
|         DirectAccessOneParameterString |             .NET 7.0 |   9.229 ns |   9.236 ns |  1.30 | 0.0038 |      64 B |        1.00 |
|   TypeMapperOneParameterTimeSpanUnwrap |             .NET 7.0 |   9.106 ns |   9.416 ns |  1.31 | 0.0038 |      64 B |        1.00 |
| DirectAccessOneParameterTimeSpanUnwrap |             .NET 7.0 |   9.729 ns |   9.684 ns |  1.37 | 0.0038 |      64 B |        1.00 |
|       TypeMapperTwoParametersIntString |             .NET 7.0 |  46.072 ns |  46.026 ns |  6.49 | 0.0057 |      96 B |        1.50 |
|     DirectAccessTwoParametersIntString |             .NET 7.0 |   9.365 ns |   9.413 ns |  1.30 | 0.0038 |      64 B |        1.00 |
|    TypeMapperTwoParametersStringString |             .NET 7.0 |  47.752 ns |  48.002 ns |  6.73 | 0.0057 |      96 B |        1.50 |
|  DirectAccessTwoParametersStringString |             .NET 7.0 |   8.768 ns |   8.849 ns |  1.24 | 0.0038 |      64 B |        1.00 |
|     TypeMapperTwoParametersWrapperEnum |             .NET 7.0 |  52.043 ns |  52.306 ns |  7.36 | 0.0057 |      96 B |        1.50 |
|   DirectAccessTwoParametersWrapperEnum |             .NET 7.0 |   8.124 ns |   8.015 ns |  1.13 | 0.0038 |      64 B |        1.00 |
|   TypeMapperTwoParametersWrapperString |             .NET 7.0 |  48.061 ns |  48.189 ns |  6.83 | 0.0057 |      96 B |        1.50 |
| DirectAccessTwoParametersWrapperString |             .NET 7.0 |   9.542 ns |   9.564 ns |  1.35 | 0.0038 |      64 B |        1.00 |
|              TypeMapperThreeParameters |             .NET 7.0 |  23.481 ns |  23.388 ns |  3.34 | 0.0057 |      96 B |        1.50 |
|            DirectAccessThreeParameters |             .NET 7.0 |   9.298 ns |   9.322 ns |  1.31 | 0.0038 |      64 B |        1.00 |
|                  TypeMapperTSTZFactory |             .NET 7.0 | 137.133 ns | 134.424 ns | 18.94 | 0.0038 |      64 B |        1.00 |
|                DirectAccessTSTZFactory |             .NET 7.0 | 135.076 ns | 134.282 ns | 18.62 | 0.0038 |      64 B |        1.00 |
|                TypeMapperParameterless |        .NET Core 3.1 |  53.234 ns |  52.344 ns |  7.50 | 0.0057 |      96 B |        1.50 |
|              DirectAccessParameterless |        .NET Core 3.1 |   7.463 ns |   7.477 ns |  1.05 | 0.0038 |      64 B |        1.00 |
|           TypeMapperOneParameterString |        .NET Core 3.1 |  53.269 ns |  53.278 ns |  7.42 | 0.0057 |      96 B |        1.50 |
|         DirectAccessOneParameterString |        .NET Core 3.1 |   9.683 ns |   9.796 ns |  1.33 | 0.0038 |      64 B |        1.00 |
|   TypeMapperOneParameterTimeSpanUnwrap |        .NET Core 3.1 |   7.179 ns |   6.919 ns |  1.00 | 0.0038 |      64 B |        1.00 |
| DirectAccessOneParameterTimeSpanUnwrap |        .NET Core 3.1 |   6.448 ns |   6.320 ns |  0.90 | 0.0038 |      64 B |        1.00 |
|       TypeMapperTwoParametersIntString |        .NET Core 3.1 |  54.018 ns |  53.557 ns |  7.62 | 0.0057 |      96 B |        1.50 |
|     DirectAccessTwoParametersIntString |        .NET Core 3.1 |   7.780 ns |   7.824 ns |  1.10 | 0.0038 |      64 B |        1.00 |
|    TypeMapperTwoParametersStringString |        .NET Core 3.1 |  52.355 ns |  52.393 ns |  7.30 | 0.0057 |      96 B |        1.50 |
|  DirectAccessTwoParametersStringString |        .NET Core 3.1 |   7.294 ns |   7.290 ns |  1.03 | 0.0038 |      64 B |        1.00 |
|     TypeMapperTwoParametersWrapperEnum |        .NET Core 3.1 |  63.738 ns |  63.580 ns |  8.98 | 0.0057 |      96 B |        1.50 |
|   DirectAccessTwoParametersWrapperEnum |        .NET Core 3.1 |   7.455 ns |   7.543 ns |  1.05 | 0.0038 |      64 B |        1.00 |
|   TypeMapperTwoParametersWrapperString |        .NET Core 3.1 |  52.842 ns |  52.608 ns |  7.44 | 0.0057 |      96 B |        1.50 |
| DirectAccessTwoParametersWrapperString |        .NET Core 3.1 |   9.364 ns |   9.359 ns |  1.32 | 0.0038 |      64 B |        1.00 |
|              TypeMapperThreeParameters |        .NET Core 3.1 |  64.146 ns |  63.702 ns |  9.07 | 0.0057 |      96 B |        1.50 |
|            DirectAccessThreeParameters |        .NET Core 3.1 |   8.766 ns |   8.751 ns |  1.22 | 0.0038 |      64 B |        1.00 |
|                  TypeMapperTSTZFactory |        .NET Core 3.1 | 255.359 ns | 281.416 ns | 31.65 | 0.0038 |      64 B |        1.00 |
|                DirectAccessTSTZFactory |        .NET Core 3.1 | 266.384 ns | 266.184 ns | 37.84 | 0.0038 |      64 B |        1.00 |
|                TypeMapperParameterless | .NET Framework 4.7.2 |  74.012 ns |  74.048 ns | 10.32 | 0.0153 |      96 B |        1.50 |
|              DirectAccessParameterless | .NET Framework 4.7.2 |   7.233 ns |   7.414 ns |  1.00 | 0.0102 |      64 B |        1.00 |
|           TypeMapperOneParameterString | .NET Framework 4.7.2 |  54.777 ns |  67.140 ns |  6.59 | 0.0153 |      96 B |        1.50 |
|         DirectAccessOneParameterString | .NET Framework 4.7.2 |   8.355 ns |   8.394 ns |  1.18 | 0.0102 |      64 B |        1.00 |
|   TypeMapperOneParameterTimeSpanUnwrap | .NET Framework 4.7.2 |  17.152 ns |  16.632 ns |  2.38 | 0.0102 |      64 B |        1.00 |
| DirectAccessOneParameterTimeSpanUnwrap | .NET Framework 4.7.2 |   8.206 ns |   8.228 ns |  1.16 | 0.0102 |      64 B |        1.00 |
|       TypeMapperTwoParametersIntString | .NET Framework 4.7.2 |  76.406 ns |  76.686 ns | 10.77 | 0.0153 |      96 B |        1.50 |
|     DirectAccessTwoParametersIntString | .NET Framework 4.7.2 |   8.212 ns |   8.409 ns |  1.12 | 0.0102 |      64 B |        1.00 |
|    TypeMapperTwoParametersStringString | .NET Framework 4.7.2 |  75.998 ns |  76.494 ns | 10.70 | 0.0153 |      96 B |        1.50 |
|  DirectAccessTwoParametersStringString | .NET Framework 4.7.2 |   7.864 ns |   7.866 ns |  1.11 | 0.0102 |      64 B |        1.00 |
|     TypeMapperTwoParametersWrapperEnum | .NET Framework 4.7.2 |  89.205 ns |  88.408 ns | 12.42 | 0.0153 |      96 B |        1.50 |
|   DirectAccessTwoParametersWrapperEnum | .NET Framework 4.7.2 |   7.622 ns |   7.653 ns |  1.07 | 0.0102 |      64 B |        1.00 |
|   TypeMapperTwoParametersWrapperString | .NET Framework 4.7.2 |  75.931 ns |  76.519 ns | 10.70 | 0.0153 |      96 B |        1.50 |
| DirectAccessTwoParametersWrapperString | .NET Framework 4.7.2 |   8.296 ns |   8.364 ns |  1.16 | 0.0102 |      64 B |        1.00 |
|              TypeMapperThreeParameters | .NET Framework 4.7.2 |  99.526 ns |  99.585 ns | 14.02 | 0.0153 |      96 B |        1.50 |
|            DirectAccessThreeParameters | .NET Framework 4.7.2 |   7.817 ns |   7.852 ns |  1.10 | 0.0102 |      64 B |        1.00 |
|                  TypeMapperTSTZFactory | .NET Framework 4.7.2 | 283.531 ns | 278.918 ns | 39.65 | 0.0100 |      64 B |        1.00 |
|                DirectAccessTSTZFactory | .NET Framework 4.7.2 | 303.031 ns | 305.640 ns | 42.68 | 0.0100 |      64 B |        1.00 |
