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
|              Method |              Runtime |       Mean |     Median | Ratio | Allocated | Alloc Ratio |
|-------------------- |--------------------- |-----------:|-----------:|------:|----------:|------------:|
|    TypeMapperString |             .NET 6.0 |  7.5217 ns |  7.2902 ns |  1.70 |         - |          NA |
|  DirectAccessString |             .NET 6.0 |  3.8154 ns |  3.7997 ns |  0.85 |         - |          NA |
|       TypeMapperInt |             .NET 6.0 |  5.2756 ns |  5.3084 ns |  1.17 |         - |          NA |
|     DirectAccessInt |             .NET 6.0 |  0.8227 ns |  0.9209 ns |  0.15 |         - |          NA |
|   TypeMapperBoolean |             .NET 6.0 |  6.5793 ns |  6.6601 ns |  1.47 |         - |          NA |
| DirectAccessBoolean |             .NET 6.0 |  0.8941 ns |  0.9370 ns |  0.20 |         - |          NA |
|   TypeMapperWrapper |             .NET 6.0 |  8.9367 ns |  8.9672 ns |  1.99 |         - |          NA |
| DirectAccessWrapper |             .NET 6.0 |  3.2599 ns |  3.2895 ns |  0.73 |         - |          NA |
|    TypeMapperString |             .NET 7.0 |  8.8266 ns |  8.9539 ns |  1.96 |         - |          NA |
|  DirectAccessString |             .NET 7.0 |  0.7825 ns |  0.7802 ns |  0.17 |         - |          NA |
|       TypeMapperInt |             .NET 7.0 |  5.2299 ns |  5.2268 ns |  1.17 |         - |          NA |
|     DirectAccessInt |             .NET 7.0 |  0.5694 ns |  0.5081 ns |  0.13 |         - |          NA |
|   TypeMapperBoolean |             .NET 7.0 |  5.0995 ns |  5.5066 ns |  1.04 |         - |          NA |
| DirectAccessBoolean |             .NET 7.0 |  1.0031 ns |  1.1527 ns |  0.26 |         - |          NA |
|   TypeMapperWrapper |             .NET 7.0 |  9.1449 ns |  9.8594 ns |  1.46 |         - |          NA |
| DirectAccessWrapper |             .NET 7.0 |  4.2393 ns |  4.2531 ns |  0.95 |         - |          NA |
|    TypeMapperString |        .NET Core 3.1 |  7.9404 ns |  7.7118 ns |  1.76 |         - |          NA |
|  DirectAccessString |        .NET Core 3.1 |  3.7594 ns |  3.7831 ns |  0.84 |         - |          NA |
|       TypeMapperInt |        .NET Core 3.1 |  5.6911 ns |  5.7375 ns |  1.27 |         - |          NA |
|     DirectAccessInt |        .NET Core 3.1 |  1.1238 ns |  1.1507 ns |  0.21 |         - |          NA |
|   TypeMapperBoolean |        .NET Core 3.1 |  6.1348 ns |  6.1101 ns |  1.37 |         - |          NA |
| DirectAccessBoolean |        .NET Core 3.1 |  0.9580 ns |  0.9565 ns |  0.21 |         - |          NA |
|   TypeMapperWrapper |        .NET Core 3.1 |  9.1441 ns |  9.1255 ns |  2.04 |         - |          NA |
| DirectAccessWrapper |        .NET Core 3.1 |  4.6591 ns |  4.6584 ns |  1.04 |         - |          NA |
|    TypeMapperString | .NET Framework 4.7.2 | 26.1810 ns | 26.1542 ns |  5.85 |         - |          NA |
|  DirectAccessString | .NET Framework 4.7.2 |  4.4739 ns |  4.4816 ns |  1.00 |         - |          NA |
|       TypeMapperInt | .NET Framework 4.7.2 | 23.2655 ns | 23.3403 ns |  5.19 |         - |          NA |
|     DirectAccessInt | .NET Framework 4.7.2 |  1.0214 ns |  1.0309 ns |  0.23 |         - |          NA |
|   TypeMapperBoolean | .NET Framework 4.7.2 | 23.1522 ns | 22.8653 ns |  5.17 |         - |          NA |
| DirectAccessBoolean | .NET Framework 4.7.2 |  1.0019 ns |  1.0178 ns |  0.22 |         - |          NA |
|   TypeMapperWrapper | .NET Framework 4.7.2 | 29.7746 ns | 34.3845 ns |  7.49 |         - |          NA |
| DirectAccessWrapper | .NET Framework 4.7.2 |  4.9697 ns |  4.9839 ns |  1.11 |         - |          NA |
