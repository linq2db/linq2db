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
|              Method |              Runtime |       Mean |     Median | Ratio | Allocated | Alloc Ratio |
|-------------------- |--------------------- |-----------:|-----------:|------:|----------:|------------:|
|    TypeMapperString |             .NET 6.0 |  8.4608 ns |  8.5475 ns |  2.23 |         - |          NA |
|  DirectAccessString |             .NET 6.0 |  3.7569 ns |  3.7846 ns |  0.99 |         - |          NA |
|       TypeMapperInt |             .NET 6.0 |  4.5901 ns |  4.5568 ns |  1.21 |         - |          NA |
|     DirectAccessInt |             .NET 6.0 |  0.9097 ns |  0.9421 ns |  0.20 |         - |          NA |
|   TypeMapperBoolean |             .NET 6.0 |  4.2178 ns |  5.1918 ns |  1.23 |         - |          NA |
| DirectAccessBoolean |             .NET 6.0 |  0.9187 ns |  0.9241 ns |  0.24 |         - |          NA |
|   TypeMapperWrapper |             .NET 6.0 |  8.7947 ns |  8.7834 ns |  2.32 |         - |          NA |
| DirectAccessWrapper |             .NET 6.0 |  2.7501 ns |  3.2193 ns |  0.36 |         - |          NA |
|    TypeMapperString |             .NET 7.0 |  8.8354 ns |  8.8491 ns |  2.33 |         - |          NA |
|  DirectAccessString |             .NET 7.0 |  3.8905 ns |  4.5867 ns |  0.85 |         - |          NA |
|       TypeMapperInt |             .NET 7.0 |  5.1757 ns |  5.1384 ns |  1.34 |         - |          NA |
|     DirectAccessInt |             .NET 7.0 |  0.4644 ns |  0.4744 ns |  0.12 |         - |          NA |
|   TypeMapperBoolean |             .NET 7.0 |  4.6476 ns |  4.6749 ns |  1.22 |         - |          NA |
| DirectAccessBoolean |             .NET 7.0 |  1.3378 ns |  1.3401 ns |  0.35 |         - |          NA |
|   TypeMapperWrapper |             .NET 7.0 |  9.5319 ns |  9.7201 ns |  2.16 |         - |          NA |
| DirectAccessWrapper |             .NET 7.0 |  4.1989 ns |  4.2404 ns |  1.11 |         - |          NA |
|    TypeMapperString |        .NET Core 3.1 |  8.4065 ns |  8.4567 ns |  2.22 |         - |          NA |
|  DirectAccessString |        .NET Core 3.1 |  3.2820 ns |  3.2271 ns |  0.87 |         - |          NA |
|       TypeMapperInt |        .NET Core 3.1 |  5.6311 ns |  5.6407 ns |  1.48 |         - |          NA |
|     DirectAccessInt |        .NET Core 3.1 |  1.1516 ns |  1.1550 ns |  0.30 |         - |          NA |
|   TypeMapperBoolean |        .NET Core 3.1 |  6.0482 ns |  6.0386 ns |  1.59 |         - |          NA |
| DirectAccessBoolean |        .NET Core 3.1 |  0.9854 ns |  0.9990 ns |  0.26 |         - |          NA |
|   TypeMapperWrapper |        .NET Core 3.1 |  9.0280 ns |  9.2357 ns |  2.22 |         - |          NA |
| DirectAccessWrapper |        .NET Core 3.1 |  3.2961 ns |  3.2897 ns |  0.87 |         - |          NA |
|    TypeMapperString | .NET Framework 4.7.2 | 26.0271 ns | 26.1199 ns |  6.87 |         - |          NA |
|  DirectAccessString | .NET Framework 4.7.2 |  3.7915 ns |  3.8068 ns |  1.00 |         - |          NA |
|       TypeMapperInt | .NET Framework 4.7.2 | 21.2202 ns | 20.9240 ns |  5.62 |         - |          NA |
|     DirectAccessInt | .NET Framework 4.7.2 |  0.9835 ns |  0.9759 ns |  0.26 |         - |          NA |
|   TypeMapperBoolean | .NET Framework 4.7.2 | 21.2937 ns | 20.9809 ns |  5.63 |         - |          NA |
| DirectAccessBoolean | .NET Framework 4.7.2 |  0.7974 ns |  0.8469 ns |  0.04 |         - |          NA |
|   TypeMapperWrapper | .NET Framework 4.7.2 | 34.2577 ns | 34.2411 ns |  9.04 |         - |          NA |
| DirectAccessWrapper | .NET Framework 4.7.2 |  4.8451 ns |  4.8742 ns |  1.27 |         - |          NA |
