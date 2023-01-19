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
|              Method |              Runtime |       Mean |     Median | Ratio | Allocated | Alloc Ratio |
|-------------------- |--------------------- |-----------:|-----------:|------:|----------:|------------:|
|    TypeMapperString |             .NET 6.0 |  8.2202 ns |  8.2202 ns | 2.033 |         - |          NA |
|  DirectAccessString |             .NET 6.0 |  2.8351 ns |  2.6788 ns | 0.688 |         - |          NA |
|       TypeMapperInt |             .NET 6.0 |  5.1600 ns |  5.1529 ns | 1.272 |         - |          NA |
|     DirectAccessInt |             .NET 6.0 |  0.8989 ns |  0.9134 ns | 0.223 |         - |          NA |
|   TypeMapperBoolean |             .NET 6.0 |  5.8157 ns |  5.8157 ns | 1.438 |         - |          NA |
| DirectAccessBoolean |             .NET 6.0 |  0.6716 ns |  0.9229 ns | 0.164 |         - |          NA |
|   TypeMapperWrapper |             .NET 6.0 |  8.6759 ns |  8.6757 ns | 2.145 |         - |          NA |
| DirectAccessWrapper |             .NET 6.0 |  3.1962 ns |  3.1960 ns | 0.793 |         - |          NA |
|    TypeMapperString |             .NET 7.0 |  8.6744 ns |  8.6741 ns | 2.145 |         - |          NA |
|  DirectAccessString |             .NET 7.0 |  4.5663 ns |  4.5663 ns | 1.132 |         - |          NA |
|       TypeMapperInt |             .NET 7.0 |  5.4853 ns |  6.3912 ns | 1.049 |         - |          NA |
|     DirectAccessInt |             .NET 7.0 |  0.0000 ns |  0.0000 ns | 0.000 |         - |          NA |
|   TypeMapperBoolean |             .NET 7.0 |  4.6443 ns |  4.6486 ns | 1.148 |         - |          NA |
| DirectAccessBoolean |             .NET 7.0 |  1.1637 ns |  1.1841 ns | 0.282 |         - |          NA |
|   TypeMapperWrapper |             .NET 7.0 |  8.8066 ns |  8.5284 ns | 2.133 |         - |          NA |
| DirectAccessWrapper |             .NET 7.0 |  3.8723 ns |  3.8314 ns | 0.957 |         - |          NA |
|    TypeMapperString |        .NET Core 3.1 |  8.2129 ns |  8.2202 ns | 2.039 |         - |          NA |
|  DirectAccessString |        .NET Core 3.1 |  3.6522 ns |  3.6521 ns | 0.905 |         - |          NA |
|       TypeMapperInt |        .NET Core 3.1 |  5.9365 ns |  5.9364 ns | 1.474 |         - |          NA |
|     DirectAccessInt |        .NET Core 3.1 |  0.7588 ns |  0.9129 ns | 0.135 |         - |          NA |
|   TypeMapperBoolean |        .NET Core 3.1 |  6.0463 ns |  5.9833 ns | 1.495 |         - |          NA |
| DirectAccessBoolean |        .NET Core 3.1 |  0.0000 ns |  0.0000 ns | 0.000 |         - |          NA |
|   TypeMapperWrapper |        .NET Core 3.1 |  9.3435 ns |  9.3924 ns | 2.311 |         - |          NA |
| DirectAccessWrapper |        .NET Core 3.1 |  3.0391 ns |  3.0570 ns | 0.751 |         - |          NA |
|    TypeMapperString | .NET Framework 4.7.2 | 23.8575 ns | 23.9361 ns | 5.923 |         - |          NA |
|  DirectAccessString | .NET Framework 4.7.2 |  4.1105 ns |  4.1105 ns | 1.000 |         - |          NA |
|       TypeMapperInt | .NET Framework 4.7.2 | 24.0797 ns | 24.1541 ns | 5.956 |         - |          NA |
|     DirectAccessInt | .NET Framework 4.7.2 |  1.3697 ns |  1.3697 ns | 0.339 |         - |          NA |
|   TypeMapperBoolean | .NET Framework 4.7.2 | 21.3975 ns | 23.2565 ns | 4.979 |         - |          NA |
| DirectAccessBoolean | .NET Framework 4.7.2 |  1.3700 ns |  1.3699 ns | 0.339 |         - |          NA |
|   TypeMapperWrapper | .NET Framework 4.7.2 | 33.7645 ns | 33.7633 ns | 8.369 |         - |          NA |
| DirectAccessWrapper | .NET Framework 4.7.2 |  3.6247 ns |  3.6247 ns | 0.896 |         - |          NA |
