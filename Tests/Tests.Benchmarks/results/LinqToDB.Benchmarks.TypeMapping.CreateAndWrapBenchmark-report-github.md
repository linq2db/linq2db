``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-UZBSVL : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-AYZXIO : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-NXXYQT : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-HMCTKM : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                                 Method |              Runtime |       Mean |     Median | Ratio |   Gen0 | Allocated | Alloc Ratio |
|--------------------------------------- |--------------------- |-----------:|-----------:|------:|-------:|----------:|------------:|
|                TypeMapperParameterless |             .NET 6.0 |  48.606 ns |  48.751 ns |  6.14 | 0.0057 |      96 B |        1.50 |
|              DirectAccessParameterless |             .NET 6.0 |   5.974 ns |   5.929 ns |  0.76 | 0.0038 |      64 B |        1.00 |
|           TypeMapperOneParameterString |             .NET 6.0 |  48.333 ns |  48.472 ns |  6.10 | 0.0057 |      96 B |        1.50 |
|         DirectAccessOneParameterString |             .NET 6.0 |   6.845 ns |   6.863 ns |  0.86 | 0.0038 |      64 B |        1.00 |
|   TypeMapperOneParameterTimeSpanUnwrap |             .NET 6.0 |   7.915 ns |   8.102 ns |  1.00 | 0.0038 |      64 B |        1.00 |
| DirectAccessOneParameterTimeSpanUnwrap |             .NET 6.0 |   7.283 ns |   7.315 ns |  0.92 | 0.0038 |      64 B |        1.00 |
|       TypeMapperTwoParametersIntString |             .NET 6.0 |  48.875 ns |  48.974 ns |  6.18 | 0.0057 |      96 B |        1.50 |
|     DirectAccessTwoParametersIntString |             .NET 6.0 |   6.511 ns |   6.303 ns |  0.82 | 0.0038 |      64 B |        1.00 |
|    TypeMapperTwoParametersStringString |             .NET 6.0 |  48.893 ns |  48.945 ns |  6.18 | 0.0057 |      96 B |        1.50 |
|  DirectAccessTwoParametersStringString |             .NET 6.0 |  10.944 ns |  10.960 ns |  1.38 | 0.0038 |      64 B |        1.00 |
|     TypeMapperTwoParametersWrapperEnum |             .NET 6.0 |  56.537 ns |  56.159 ns |  7.12 | 0.0057 |      96 B |        1.50 |
|   DirectAccessTwoParametersWrapperEnum |             .NET 6.0 |   8.094 ns |   8.109 ns |  1.02 | 0.0038 |      64 B |        1.00 |
|   TypeMapperTwoParametersWrapperString |             .NET 6.0 |  42.762 ns |  48.885 ns |  3.22 | 0.0057 |      96 B |        1.50 |
| DirectAccessTwoParametersWrapperString |             .NET 6.0 |   7.326 ns |   7.391 ns |  0.91 | 0.0038 |      64 B |        1.00 |
|              TypeMapperThreeParameters |             .NET 6.0 |  56.479 ns |  56.591 ns |  6.97 | 0.0057 |      96 B |        1.50 |
|            DirectAccessThreeParameters |             .NET 6.0 |   6.331 ns |   6.209 ns |  0.79 | 0.0038 |      64 B |        1.00 |
|                  TypeMapperTSTZFactory |             .NET 6.0 | 146.698 ns | 164.418 ns | 15.59 | 0.0038 |      64 B |        1.00 |
|                DirectAccessTSTZFactory |             .NET 6.0 | 160.501 ns | 159.163 ns | 20.29 | 0.0038 |      64 B |        1.00 |
|                TypeMapperParameterless |             .NET 7.0 |  50.405 ns |  50.407 ns |  6.36 | 0.0057 |      96 B |        1.50 |
|              DirectAccessParameterless |             .NET 7.0 |   9.598 ns |  11.884 ns |  1.42 | 0.0038 |      64 B |        1.00 |
|           TypeMapperOneParameterString |             .NET 7.0 |  46.653 ns |  46.742 ns |  5.89 | 0.0057 |      96 B |        1.50 |
|         DirectAccessOneParameterString |             .NET 7.0 |   9.430 ns |   9.425 ns |  1.19 | 0.0038 |      64 B |        1.00 |
|   TypeMapperOneParameterTimeSpanUnwrap |             .NET 7.0 |  15.005 ns |  14.776 ns |  1.78 | 0.0038 |      64 B |        1.00 |
| DirectAccessOneParameterTimeSpanUnwrap |             .NET 7.0 |   9.493 ns |   9.501 ns |  1.20 | 0.0038 |      64 B |        1.00 |
|       TypeMapperTwoParametersIntString |             .NET 7.0 |  41.308 ns |  47.361 ns |  5.87 | 0.0057 |      96 B |        1.50 |
|     DirectAccessTwoParametersIntString |             .NET 7.0 |   9.274 ns |   9.329 ns |  1.17 | 0.0038 |      64 B |        1.00 |
|    TypeMapperTwoParametersStringString |             .NET 7.0 |  48.100 ns |  48.189 ns |  6.08 | 0.0057 |      96 B |        1.50 |
|  DirectAccessTwoParametersStringString |             .NET 7.0 |   9.790 ns |  11.858 ns |  0.86 | 0.0038 |      64 B |        1.00 |
|     TypeMapperTwoParametersWrapperEnum |             .NET 7.0 |  50.802 ns |  50.954 ns |  6.42 | 0.0057 |      96 B |        1.50 |
|   DirectAccessTwoParametersWrapperEnum |             .NET 7.0 |   9.557 ns |   9.544 ns |  1.21 | 0.0038 |      64 B |        1.00 |
|   TypeMapperTwoParametersWrapperString |             .NET 7.0 |  50.125 ns |  48.969 ns |  6.23 | 0.0057 |      96 B |        1.50 |
| DirectAccessTwoParametersWrapperString |             .NET 7.0 |   9.420 ns |  10.321 ns |  1.23 | 0.0038 |      64 B |        1.00 |
|              TypeMapperThreeParameters |             .NET 7.0 |  57.860 ns |  56.969 ns |  7.31 | 0.0057 |      96 B |        1.50 |
|            DirectAccessThreeParameters |             .NET 7.0 |   9.379 ns |   9.374 ns |  1.18 | 0.0038 |      64 B |        1.00 |
|                  TypeMapperTSTZFactory |             .NET 7.0 | 148.953 ns | 150.239 ns | 18.81 | 0.0038 |      64 B |        1.00 |
|                DirectAccessTSTZFactory |             .NET 7.0 | 134.684 ns | 132.811 ns | 17.44 | 0.0038 |      64 B |        1.00 |
|                TypeMapperParameterless |        .NET Core 3.1 |  52.429 ns |  52.569 ns |  6.62 | 0.0057 |      96 B |        1.50 |
|              DirectAccessParameterless |        .NET Core 3.1 |   7.036 ns |   7.076 ns |  0.89 | 0.0038 |      64 B |        1.00 |
|           TypeMapperOneParameterString |        .NET Core 3.1 |  52.936 ns |  52.627 ns |  6.67 | 0.0057 |      96 B |        1.50 |
|         DirectAccessOneParameterString |        .NET Core 3.1 |   6.706 ns |   6.650 ns |  0.82 | 0.0038 |      64 B |        1.00 |
|   TypeMapperOneParameterTimeSpanUnwrap |        .NET Core 3.1 |   8.136 ns |   8.172 ns |  1.02 | 0.0038 |      64 B |        1.00 |
| DirectAccessOneParameterTimeSpanUnwrap |        .NET Core 3.1 |   7.708 ns |   8.117 ns |  0.68 | 0.0038 |      64 B |        1.00 |
|       TypeMapperTwoParametersIntString |        .NET Core 3.1 |  54.909 ns |  54.934 ns |  6.94 | 0.0057 |      96 B |        1.50 |
|     DirectAccessTwoParametersIntString |        .NET Core 3.1 |   6.715 ns |   6.712 ns |  0.85 | 0.0038 |      64 B |        1.00 |
|    TypeMapperTwoParametersStringString |        .NET Core 3.1 |  54.558 ns |  54.535 ns |  6.89 | 0.0057 |      96 B |        1.50 |
|  DirectAccessTwoParametersStringString |        .NET Core 3.1 |   7.953 ns |   7.977 ns |  1.00 | 0.0038 |      64 B |        1.00 |
|     TypeMapperTwoParametersWrapperEnum |        .NET Core 3.1 |  68.215 ns |  68.559 ns |  8.57 | 0.0057 |      96 B |        1.50 |
|   DirectAccessTwoParametersWrapperEnum |        .NET Core 3.1 |   8.126 ns |   8.224 ns |  1.03 | 0.0038 |      64 B |        1.00 |
|   TypeMapperTwoParametersWrapperString |        .NET Core 3.1 |  56.258 ns |  61.102 ns |  6.89 | 0.0057 |      96 B |        1.50 |
| DirectAccessTwoParametersWrapperString |        .NET Core 3.1 |   7.899 ns |   8.007 ns |  0.98 | 0.0038 |      64 B |        1.00 |
|              TypeMapperThreeParameters |        .NET Core 3.1 |  48.571 ns |  57.396 ns |  7.43 | 0.0057 |      96 B |        1.50 |
|            DirectAccessThreeParameters |        .NET Core 3.1 |   7.029 ns |   7.080 ns |  0.89 | 0.0038 |      64 B |        1.00 |
|                  TypeMapperTSTZFactory |        .NET Core 3.1 | 289.152 ns | 290.400 ns | 36.54 | 0.0038 |      64 B |        1.00 |
|                DirectAccessTSTZFactory |        .NET Core 3.1 | 243.912 ns | 267.278 ns | 34.49 | 0.0038 |      64 B |        1.00 |
|                TypeMapperParameterless | .NET Framework 4.7.2 |  74.508 ns |  73.755 ns |  9.40 | 0.0153 |      96 B |        1.50 |
|              DirectAccessParameterless | .NET Framework 4.7.2 |   7.916 ns |   7.988 ns |  1.00 | 0.0102 |      64 B |        1.00 |
|           TypeMapperOneParameterString | .NET Framework 4.7.2 |  75.406 ns |  75.669 ns |  9.53 | 0.0153 |      96 B |        1.50 |
|         DirectAccessOneParameterString | .NET Framework 4.7.2 |   6.362 ns |   8.156 ns |  0.28 | 0.0102 |      64 B |        1.00 |
|   TypeMapperOneParameterTimeSpanUnwrap | .NET Framework 4.7.2 |  18.959 ns |  19.063 ns |  2.40 | 0.0102 |      64 B |        1.00 |
| DirectAccessOneParameterTimeSpanUnwrap | .NET Framework 4.7.2 |   7.593 ns |   7.666 ns |  0.95 | 0.0102 |      64 B |        1.00 |
|       TypeMapperTwoParametersIntString | .NET Framework 4.7.2 |  55.801 ns |  66.169 ns |  8.85 | 0.0153 |      96 B |        1.50 |
|     DirectAccessTwoParametersIntString | .NET Framework 4.7.2 |   8.320 ns |   8.438 ns |  1.06 | 0.0102 |      64 B |        1.00 |
|    TypeMapperTwoParametersStringString | .NET Framework 4.7.2 |  76.382 ns |  76.759 ns |  9.65 | 0.0153 |      96 B |        1.50 |
|  DirectAccessTwoParametersStringString | .NET Framework 4.7.2 |   8.275 ns |   8.326 ns |  1.04 | 0.0102 |      64 B |        1.00 |
|     TypeMapperTwoParametersWrapperEnum | .NET Framework 4.7.2 |  42.276 ns |  42.241 ns |  5.35 | 0.0153 |      96 B |        1.50 |
|   DirectAccessTwoParametersWrapperEnum | .NET Framework 4.7.2 |   8.202 ns |   8.205 ns |  1.04 | 0.0102 |      64 B |        1.00 |
|   TypeMapperTwoParametersWrapperString | .NET Framework 4.7.2 |  69.012 ns |  69.445 ns |  8.66 | 0.0153 |      96 B |        1.50 |
| DirectAccessTwoParametersWrapperString | .NET Framework 4.7.2 |   8.042 ns |   8.323 ns |  0.97 | 0.0102 |      64 B |        1.00 |
|              TypeMapperThreeParameters | .NET Framework 4.7.2 | 104.460 ns | 104.679 ns | 13.22 | 0.0153 |      96 B |        1.50 |
|            DirectAccessThreeParameters | .NET Framework 4.7.2 |   8.103 ns |   8.045 ns |  1.02 | 0.0102 |      64 B |        1.00 |
|                  TypeMapperTSTZFactory | .NET Framework 4.7.2 | 262.936 ns | 306.440 ns | 25.57 | 0.0100 |      64 B |        1.00 |
|                DirectAccessTSTZFactory | .NET Framework 4.7.2 | 299.784 ns | 298.703 ns | 37.89 | 0.0100 |      64 B |        1.00 |
