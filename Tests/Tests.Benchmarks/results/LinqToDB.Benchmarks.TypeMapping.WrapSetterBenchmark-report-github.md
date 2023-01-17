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
|              Method |              Runtime |       Mean |     Median | Ratio | Allocated | Alloc Ratio |
|-------------------- |--------------------- |-----------:|-----------:|------:|----------:|------------:|
|    TypeMapperString |             .NET 6.0 |  7.5642 ns |  7.5058 ns |  8.57 |         - |          NA |
|  DirectAccessString |             .NET 6.0 |  3.2463 ns |  3.2212 ns |  2.30 |         - |          NA |
|       TypeMapperInt |             .NET 6.0 |  3.9882 ns |  4.6817 ns |  4.38 |         - |          NA |
|     DirectAccessInt |             .NET 6.0 |  0.9022 ns |  0.9235 ns |  0.50 |         - |          NA |
|   TypeMapperBoolean |             .NET 6.0 |  5.1494 ns |  5.1609 ns |  2.97 |         - |          NA |
| DirectAccessBoolean |             .NET 6.0 |  0.9416 ns |  0.9483 ns |  0.40 |         - |          NA |
|   TypeMapperWrapper |             .NET 6.0 |  8.8748 ns |  8.8345 ns |  6.12 |         - |          NA |
| DirectAccessWrapper |             .NET 6.0 |  3.2878 ns |  3.3299 ns |  2.14 |         - |          NA |
|    TypeMapperString |             .NET 7.0 |  8.8819 ns |  8.9768 ns |  6.22 |         - |          NA |
|  DirectAccessString |             .NET 7.0 |  4.7523 ns |  4.7592 ns |  3.34 |         - |          NA |
|       TypeMapperInt |             .NET 7.0 |  5.1669 ns |  5.0859 ns |  3.62 |         - |          NA |
|     DirectAccessInt |             .NET 7.0 |  0.5083 ns |  0.4799 ns |  0.35 |         - |          NA |
|   TypeMapperBoolean |             .NET 7.0 |  4.6282 ns |  4.5902 ns |  3.23 |         - |          NA |
| DirectAccessBoolean |             .NET 7.0 |  1.3682 ns |  1.3745 ns |  0.96 |         - |          NA |
|   TypeMapperWrapper |             .NET 7.0 |  7.8429 ns |  9.6030 ns |  8.24 |         - |          NA |
| DirectAccessWrapper |             .NET 7.0 |  4.0683 ns |  4.0954 ns |  2.85 |         - |          NA |
|    TypeMapperString |        .NET Core 3.1 |  8.5121 ns |  8.5875 ns |  5.96 |         - |          NA |
|  DirectAccessString |        .NET Core 3.1 |  3.2783 ns |  3.3096 ns |  2.29 |         - |          NA |
|       TypeMapperInt |        .NET Core 3.1 |  6.1310 ns |  6.2022 ns |  4.29 |         - |          NA |
|     DirectAccessInt |        .NET Core 3.1 |  1.3568 ns |  1.3716 ns |  0.39 |         - |          NA |
|   TypeMapperBoolean |        .NET Core 3.1 |  5.7328 ns |  7.3609 ns |  6.04 |         - |          NA |
| DirectAccessBoolean |        .NET Core 3.1 |  1.4543 ns |  1.5121 ns |  1.03 |         - |          NA |
|   TypeMapperWrapper |        .NET Core 3.1 |  8.9430 ns |  8.9746 ns |  6.23 |         - |          NA |
| DirectAccessWrapper |        .NET Core 3.1 |  3.3183 ns |  3.3380 ns |  2.32 |         - |          NA |
|    TypeMapperString | .NET Framework 4.7.2 | 26.2472 ns | 26.1834 ns | 18.41 |         - |          NA |
|  DirectAccessString | .NET Framework 4.7.2 |  2.3927 ns |  3.7128 ns |  1.00 |         - |          NA |
|       TypeMapperInt | .NET Framework 4.7.2 | 23.4671 ns | 23.4186 ns | 16.41 |         - |          NA |
|     DirectAccessInt | .NET Framework 4.7.2 |  0.9366 ns |  0.9780 ns |  1.05 |         - |          NA |
|   TypeMapperBoolean | .NET Framework 4.7.2 | 23.6009 ns | 23.5813 ns | 16.59 |         - |          NA |
| DirectAccessBoolean | .NET Framework 4.7.2 |  1.0079 ns |  1.0205 ns |  0.69 |         - |          NA |
|   TypeMapperWrapper | .NET Framework 4.7.2 | 35.0676 ns | 35.1475 ns | 20.09 |         - |          NA |
| DirectAccessWrapper | .NET Framework 4.7.2 |  3.7263 ns |  4.1851 ns |  4.25 |         - |          NA |
