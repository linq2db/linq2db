``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-TEPEZT : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-ISYUTK : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-SMHCKK : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-DHDWVI : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                                 Method |              Runtime |        Mean |      Median |  Ratio |   Gen0 | Allocated | Alloc Ratio |
|--------------------------------------- |--------------------- |------------:|------------:|-------:|-------:|----------:|------------:|
|                TypeMapperParameterless |             .NET 6.0 |  50.6822 ns |  50.4517 ns |  25.87 | 0.0057 |      96 B |        1.50 |
|              DirectAccessParameterless |             .NET 6.0 |  10.3522 ns |  10.6154 ns |   2.06 | 0.0038 |      64 B |        1.00 |
|           TypeMapperOneParameterString |             .NET 6.0 |  49.0542 ns |  50.0210 ns |   9.76 | 0.0057 |      96 B |        1.50 |
|         DirectAccessOneParameterString |             .NET 6.0 |   6.1667 ns |   7.6361 ns |   0.81 | 0.0038 |      64 B |        1.00 |
|   TypeMapperOneParameterTimeSpanUnwrap |             .NET 6.0 |   8.7939 ns |   8.7353 ns |   5.21 | 0.0038 |      64 B |        1.00 |
| DirectAccessOneParameterTimeSpanUnwrap |             .NET 6.0 |   8.3098 ns |   8.2752 ns |   4.90 | 0.0038 |      64 B |        1.00 |
|       TypeMapperTwoParametersIntString |             .NET 6.0 |  49.8780 ns |  53.3298 ns |  10.06 | 0.0057 |      96 B |        1.50 |
|     DirectAccessTwoParametersIntString |             .NET 6.0 |   7.6486 ns |   7.6040 ns |   4.55 | 0.0038 |      64 B |        1.00 |
|    TypeMapperTwoParametersStringString |             .NET 6.0 |  50.8221 ns |  50.3192 ns |  19.46 | 0.0057 |      96 B |        1.50 |
|  DirectAccessTwoParametersStringString |             .NET 6.0 |   6.2812 ns |   7.6061 ns |   1.34 | 0.0038 |      64 B |        1.00 |
|     TypeMapperTwoParametersWrapperEnum |             .NET 6.0 |  53.0583 ns |  58.5751 ns |  10.95 | 0.0057 |      96 B |        1.50 |
|   DirectAccessTwoParametersWrapperEnum |             .NET 6.0 |   6.2104 ns |   8.1779 ns |   0.84 | 0.0038 |      64 B |        1.00 |
|   TypeMapperTwoParametersWrapperString |             .NET 6.0 |  47.4467 ns |  47.1092 ns |  12.78 | 0.0057 |      96 B |        1.50 |
| DirectAccessTwoParametersWrapperString |             .NET 6.0 |   8.0038 ns |   7.9291 ns |   4.71 | 0.0038 |      64 B |        1.00 |
|              TypeMapperThreeParameters |             .NET 6.0 |  58.9683 ns |  59.0856 ns |  31.68 | 0.0057 |      96 B |        1.50 |
|            DirectAccessThreeParameters |             .NET 6.0 |   7.8140 ns |   8.1003 ns |   1.56 | 0.0038 |      64 B |        1.00 |
|                  TypeMapperTSTZFactory |             .NET 6.0 | 160.3266 ns | 162.8260 ns |  43.35 | 0.0038 |      64 B |        1.00 |
|                DirectAccessTSTZFactory |             .NET 6.0 | 160.6051 ns | 165.1165 ns |  32.01 | 0.0038 |      64 B |        1.00 |
|                TypeMapperParameterless |             .NET 7.0 |  47.3811 ns |  47.6473 ns |  23.55 | 0.0057 |      96 B |        1.50 |
|              DirectAccessParameterless |             .NET 7.0 |   5.8227 ns |   7.8676 ns |   1.43 | 0.0038 |      64 B |        1.00 |
|           TypeMapperOneParameterString |             .NET 7.0 |  49.4837 ns |  49.0819 ns |  29.58 | 0.0057 |      96 B |        1.50 |
|         DirectAccessOneParameterString |             .NET 7.0 |  10.2277 ns |  10.2476 ns |   4.83 | 0.0038 |      64 B |        1.00 |
|   TypeMapperOneParameterTimeSpanUnwrap |             .NET 7.0 |  11.6293 ns |  11.5857 ns |   5.49 | 0.0038 |      64 B |        1.00 |
| DirectAccessOneParameterTimeSpanUnwrap |             .NET 7.0 |  10.4185 ns |  10.3828 ns |   3.65 | 0.0038 |      64 B |        1.00 |
|       TypeMapperTwoParametersIntString |             .NET 7.0 |  49.2416 ns |  49.3057 ns |  26.54 | 0.0057 |      96 B |        1.50 |
|     DirectAccessTwoParametersIntString |             .NET 7.0 |   9.9379 ns |   9.9909 ns |   5.81 | 0.0038 |      64 B |        1.00 |
|    TypeMapperTwoParametersStringString |             .NET 7.0 |  49.5353 ns |  49.5867 ns |  29.27 | 0.0057 |      96 B |        1.50 |
|  DirectAccessTwoParametersStringString |             .NET 7.0 |   9.8894 ns |   9.9537 ns |   3.81 | 0.0038 |      64 B |        1.00 |
|     TypeMapperTwoParametersWrapperEnum |             .NET 7.0 |  58.7107 ns |  58.7456 ns |  24.80 | 0.0057 |      96 B |        1.50 |
|   DirectAccessTwoParametersWrapperEnum |             .NET 7.0 |   9.4108 ns |   9.9818 ns |   1.32 | 0.0038 |      64 B |        1.00 |
|   TypeMapperTwoParametersWrapperString |             .NET 7.0 |  50.3819 ns |  50.8201 ns |  10.04 | 0.0057 |      96 B |        1.50 |
| DirectAccessTwoParametersWrapperString |             .NET 7.0 |   5.6181 ns |   2.5139 ns |   0.87 | 0.0038 |      64 B |        1.00 |
|              TypeMapperThreeParameters |             .NET 7.0 |  59.3194 ns |  60.1609 ns |  10.74 | 0.0057 |      96 B |        1.50 |
|            DirectAccessThreeParameters |             .NET 7.0 |   8.4088 ns |   8.5959 ns |   1.71 | 0.0038 |      64 B |        1.00 |
|                  TypeMapperTSTZFactory |             .NET 7.0 | 156.7834 ns | 156.9423 ns |  92.45 | 0.0038 |      64 B |        1.00 |
|                DirectAccessTSTZFactory |             .NET 7.0 | 149.2539 ns | 150.1967 ns |  69.96 | 0.0038 |      64 B |        1.00 |
|                TypeMapperParameterless |        .NET Core 3.1 |  56.4158 ns |  56.3735 ns |  24.49 | 0.0057 |      96 B |        1.50 |
|              DirectAccessParameterless |        .NET Core 3.1 |   7.9972 ns |   8.1652 ns |   1.78 | 0.0038 |      64 B |        1.00 |
|           TypeMapperOneParameterString |        .NET Core 3.1 |  61.4053 ns |  62.4374 ns |  12.96 | 0.0057 |      96 B |        1.50 |
|         DirectAccessOneParameterString |        .NET Core 3.1 |   8.5401 ns |   8.5780 ns |   5.07 | 0.0038 |      64 B |        1.00 |
|   TypeMapperOneParameterTimeSpanUnwrap |        .NET Core 3.1 |   8.7089 ns |   8.7254 ns |   4.65 | 0.0038 |      64 B |        1.00 |
| DirectAccessOneParameterTimeSpanUnwrap |        .NET Core 3.1 |   8.2283 ns |   8.5225 ns |   1.66 | 0.0038 |      64 B |        1.00 |
|       TypeMapperTwoParametersIntString |        .NET Core 3.1 |  57.3467 ns |  57.3085 ns |  30.88 | 0.0057 |      96 B |        1.50 |
|     DirectAccessTwoParametersIntString |        .NET Core 3.1 |   8.7646 ns |   8.7813 ns |   5.22 | 0.0038 |      64 B |        1.00 |
|    TypeMapperTwoParametersStringString |        .NET Core 3.1 |  50.7919 ns |  50.9391 ns |  12.88 | 0.0057 |      96 B |        1.50 |
|  DirectAccessTwoParametersStringString |        .NET Core 3.1 |   0.9118 ns |   0.9016 ns |   0.40 | 0.0038 |      64 B |        1.00 |
|     TypeMapperTwoParametersWrapperEnum |        .NET Core 3.1 |  65.6933 ns |  65.7255 ns |  39.18 | 0.0057 |      96 B |        1.50 |
|   DirectAccessTwoParametersWrapperEnum |        .NET Core 3.1 |   8.3977 ns |   8.4470 ns |   5.01 | 0.0038 |      64 B |        1.00 |
|   TypeMapperTwoParametersWrapperString |        .NET Core 3.1 |  54.6704 ns |  55.5741 ns |  13.67 | 0.0057 |      96 B |        1.50 |
| DirectAccessTwoParametersWrapperString |        .NET Core 3.1 |   8.2316 ns |   8.2241 ns |   4.10 | 0.0038 |      64 B |        1.00 |
|              TypeMapperThreeParameters |        .NET Core 3.1 |  67.2037 ns |  67.5277 ns |  39.69 | 0.0057 |      96 B |        1.50 |
|            DirectAccessThreeParameters |        .NET Core 3.1 |   8.1830 ns |   8.3007 ns |   3.35 | 0.0038 |      64 B |        1.00 |
|                  TypeMapperTSTZFactory |        .NET Core 3.1 | 291.2880 ns | 290.1885 ns | 139.37 | 0.0038 |      64 B |        1.00 |
|                DirectAccessTSTZFactory |        .NET Core 3.1 | 272.2427 ns | 275.0769 ns |  87.56 | 0.0038 |      64 B |        1.00 |
|                TypeMapperParameterless | .NET Framework 4.7.2 |  67.8835 ns |  66.4803 ns |  13.38 | 0.0153 |      96 B |        1.50 |
|              DirectAccessParameterless | .NET Framework 4.7.2 |   7.7054 ns |   7.9638 ns |   1.00 | 0.0102 |      64 B |        1.00 |
|           TypeMapperOneParameterString | .NET Framework 4.7.2 |  76.2190 ns |  76.6540 ns |  45.15 | 0.0153 |      96 B |        1.50 |
|         DirectAccessOneParameterString | .NET Framework 4.7.2 |   8.3462 ns |   8.4534 ns |   3.63 | 0.0102 |      64 B |        1.00 |
|   TypeMapperOneParameterTimeSpanUnwrap | .NET Framework 4.7.2 |  19.3871 ns |  19.4604 ns |  11.50 | 0.0102 |      64 B |        1.00 |
| DirectAccessOneParameterTimeSpanUnwrap | .NET Framework 4.7.2 |   7.2570 ns |   8.3137 ns |   1.32 | 0.0102 |      64 B |        1.00 |
|       TypeMapperTwoParametersIntString | .NET Framework 4.7.2 |  76.6541 ns |  76.9166 ns |  45.36 | 0.0153 |      96 B |        1.50 |
|     DirectAccessTwoParametersIntString | .NET Framework 4.7.2 |   8.7265 ns |   8.7988 ns |   4.87 | 0.0102 |      64 B |        1.00 |
|    TypeMapperTwoParametersStringString | .NET Framework 4.7.2 |  73.8023 ns |  76.6391 ns |  14.83 | 0.0153 |      96 B |        1.50 |
|  DirectAccessTwoParametersStringString | .NET Framework 4.7.2 |   8.4661 ns |   8.4832 ns |   4.01 | 0.0102 |      64 B |        1.00 |
|     TypeMapperTwoParametersWrapperEnum | .NET Framework 4.7.2 | 100.3424 ns | 100.2734 ns |  58.94 | 0.0153 |      96 B |        1.50 |
|   DirectAccessTwoParametersWrapperEnum | .NET Framework 4.7.2 |   8.3415 ns |   8.3647 ns |   4.92 | 0.0102 |      64 B |        1.00 |
|   TypeMapperTwoParametersWrapperString | .NET Framework 4.7.2 |  75.1020 ns |  79.7009 ns |  15.26 | 0.0153 |      96 B |        1.50 |
| DirectAccessTwoParametersWrapperString | .NET Framework 4.7.2 |   8.0039 ns |   8.0652 ns |   4.77 | 0.0102 |      64 B |        1.00 |
|              TypeMapperThreeParameters | .NET Framework 4.7.2 |  90.8123 ns |  89.9409 ns |  30.87 | 0.0153 |      96 B |        1.50 |
|            DirectAccessThreeParameters | .NET Framework 4.7.2 |   8.4713 ns |   8.4696 ns |   4.02 | 0.0102 |      64 B |        1.00 |
|                  TypeMapperTSTZFactory | .NET Framework 4.7.2 | 309.0665 ns | 309.9730 ns | 181.44 | 0.0100 |      64 B |        1.00 |
|                DirectAccessTSTZFactory | .NET Framework 4.7.2 | 305.7860 ns | 307.5284 ns | 181.40 | 0.0100 |      64 B |        1.00 |
