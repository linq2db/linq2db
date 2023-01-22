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
|              Method |              Runtime |       Mean |     Median | Ratio |   Gen0 | Allocated | Alloc Ratio |
|-------------------- |--------------------- |-----------:|-----------:|------:|-------:|----------:|------------:|
|    TypeMapperString |             .NET 6.0 |  5.2636 ns |  5.3076 ns |  4.49 |      - |         - |          NA |
|  DirectAccessString |             .NET 6.0 |  0.8431 ns |  0.9196 ns |  0.91 |      - |         - |          NA |
|       TypeMapperInt |             .NET 6.0 |  5.1748 ns |  5.1602 ns |  4.30 |      - |         - |          NA |
|     DirectAccessInt |             .NET 6.0 |  0.9214 ns |  0.9197 ns |  0.77 |      - |         - |          NA |
|      TypeMapperLong |             .NET 6.0 |  5.6514 ns |  5.6248 ns |  4.69 |      - |         - |          NA |
|    DirectAccessLong |             .NET 6.0 |  0.4634 ns |  0.2890 ns |  0.63 |      - |         - |          NA |
|   TypeMapperBoolean |             .NET 6.0 |  5.2161 ns |  5.2834 ns |  4.34 |      - |         - |          NA |
| DirectAccessBoolean |             .NET 6.0 |  0.8617 ns |  0.8795 ns |  0.80 |      - |         - |          NA |
|   TypeMapperWrapper |             .NET 6.0 | 13.1364 ns | 13.2693 ns | 10.90 |      - |         - |          NA |
| DirectAccessWrapper |             .NET 6.0 |  1.2161 ns |  1.2829 ns |  1.17 |      - |         - |          NA |
|      TypeMapperEnum |             .NET 6.0 | 26.2711 ns | 28.8106 ns | 27.95 | 0.0014 |      24 B |          NA |
|    DirectAccessEnum |             .NET 6.0 |  0.8854 ns |  0.9440 ns |  0.82 |      - |         - |          NA |
|   TypeMapperVersion |             .NET 6.0 |  6.0192 ns |  6.1950 ns |  5.85 |      - |         - |          NA |
| DirectAccessVersion |             .NET 6.0 |  0.9890 ns |  0.8780 ns |  0.98 |      - |         - |          NA |
|    TypeMapperString |             .NET 7.0 |  5.1806 ns |  5.2810 ns |  4.34 |      - |         - |          NA |
|  DirectAccessString |             .NET 7.0 |  0.4619 ns |  0.4450 ns |  0.38 |      - |         - |          NA |
|       TypeMapperInt |             .NET 7.0 |  5.2248 ns |  5.3024 ns |  5.19 |      - |         - |          NA |
|     DirectAccessInt |             .NET 7.0 |  0.4900 ns |  0.4777 ns |  0.40 |      - |         - |          NA |
|      TypeMapperLong |             .NET 7.0 |  4.7762 ns |  5.2275 ns |  4.18 |      - |         - |          NA |
|    DirectAccessLong |             .NET 7.0 |  0.4383 ns |  0.4599 ns |  0.37 |      - |         - |          NA |
|   TypeMapperBoolean |             .NET 7.0 |  5.1692 ns |  5.2000 ns |  4.29 |      - |         - |          NA |
| DirectAccessBoolean |             .NET 7.0 |  0.6190 ns |  0.6103 ns |  0.52 |      - |         - |          NA |
|   TypeMapperWrapper |             .NET 7.0 | 13.2138 ns | 14.7470 ns | 13.45 |      - |         - |          NA |
| DirectAccessWrapper |             .NET 7.0 |  0.4758 ns |  0.4702 ns |  0.40 |      - |         - |          NA |
|      TypeMapperEnum |             .NET 7.0 | 13.4767 ns | 13.9455 ns | 13.82 |      - |         - |          NA |
|    DirectAccessEnum |             .NET 7.0 |  1.4820 ns |  1.4577 ns |  1.49 |      - |         - |          NA |
|   TypeMapperVersion |             .NET 7.0 |  5.2321 ns |  5.2904 ns |  4.35 |      - |         - |          NA |
| DirectAccessVersion |             .NET 7.0 |  0.4508 ns |  0.4335 ns |  0.37 |      - |         - |          NA |
|    TypeMapperString |        .NET Core 3.1 |  6.0867 ns |  6.1448 ns |  5.07 |      - |         - |          NA |
|  DirectAccessString |        .NET Core 3.1 |  0.9780 ns |  1.0064 ns |  0.84 |      - |         - |          NA |
|       TypeMapperInt |        .NET Core 3.1 |  4.8196 ns |  6.1038 ns |  5.29 |      - |         - |          NA |
|     DirectAccessInt |        .NET Core 3.1 |  0.9036 ns |  0.9464 ns |  0.89 |      - |         - |          NA |
|      TypeMapperLong |        .NET Core 3.1 |  6.1045 ns |  6.1104 ns |  5.06 |      - |         - |          NA |
|    DirectAccessLong |        .NET Core 3.1 |  0.8360 ns |  0.8513 ns |  0.70 |      - |         - |          NA |
|   TypeMapperBoolean |        .NET Core 3.1 |  6.0923 ns |  6.1038 ns |  5.64 |      - |         - |          NA |
| DirectAccessBoolean |        .NET Core 3.1 |  1.0704 ns |  1.1961 ns |  1.09 |      - |         - |          NA |
|   TypeMapperWrapper |        .NET Core 3.1 | 14.7734 ns | 14.7856 ns | 12.85 |      - |         - |          NA |
| DirectAccessWrapper |        .NET Core 3.1 |  0.8551 ns |  0.8697 ns |  0.72 |      - |         - |          NA |
|      TypeMapperEnum |        .NET Core 3.1 | 35.3319 ns | 35.4623 ns | 29.11 | 0.0014 |      24 B |          NA |
|    DirectAccessEnum |        .NET Core 3.1 |  1.4316 ns |  1.4211 ns |  1.20 |      - |         - |          NA |
|   TypeMapperVersion |        .NET Core 3.1 |  5.8054 ns |  5.8323 ns |  4.95 |      - |         - |          NA |
| DirectAccessVersion |        .NET Core 3.1 |  0.8375 ns |  0.8641 ns |  0.69 |      - |         - |          NA |
|    TypeMapperString | .NET Framework 4.7.2 | 24.2270 ns | 24.4173 ns | 21.86 |      - |         - |          NA |
|  DirectAccessString | .NET Framework 4.7.2 |  1.2232 ns |  1.3232 ns |  1.00 |      - |         - |          NA |
|       TypeMapperInt | .NET Framework 4.7.2 | 23.6646 ns | 23.6139 ns | 19.69 |      - |         - |          NA |
|     DirectAccessInt | .NET Framework 4.7.2 |  0.5459 ns |  0.5506 ns |  0.46 |      - |         - |          NA |
|      TypeMapperLong | .NET Framework 4.7.2 | 20.7524 ns | 20.1893 ns | 20.85 |      - |         - |          NA |
|    DirectAccessLong | .NET Framework 4.7.2 |  0.9116 ns |  0.9154 ns |  0.77 |      - |         - |          NA |
|   TypeMapperBoolean | .NET Framework 4.7.2 | 22.8146 ns | 22.8020 ns | 19.24 |      - |         - |          NA |
| DirectAccessBoolean | .NET Framework 4.7.2 |  0.9846 ns |  1.0243 ns |  0.89 |      - |         - |          NA |
|   TypeMapperWrapper | .NET Framework 4.7.2 | 39.4729 ns | 39.4659 ns | 32.79 |      - |         - |          NA |
| DirectAccessWrapper | .NET Framework 4.7.2 |  0.9001 ns |  0.8652 ns |  0.76 |      - |         - |          NA |
|      TypeMapperEnum | .NET Framework 4.7.2 | 68.3467 ns | 68.7643 ns | 56.89 | 0.0038 |      24 B |          NA |
|    DirectAccessEnum | .NET Framework 4.7.2 |  0.9032 ns |  0.9208 ns |  0.90 |      - |         - |          NA |
|   TypeMapperVersion | .NET Framework 4.7.2 | 23.9121 ns | 24.0290 ns | 20.18 |      - |         - |          NA |
| DirectAccessVersion | .NET Framework 4.7.2 |  0.9534 ns |  0.9822 ns |  0.81 |      - |         - |          NA |
