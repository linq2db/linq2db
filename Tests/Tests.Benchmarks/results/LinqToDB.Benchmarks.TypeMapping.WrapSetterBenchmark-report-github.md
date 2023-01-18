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
|              Method |              Runtime |       Mean |     Median | Ratio | Allocated | Alloc Ratio |
|-------------------- |--------------------- |-----------:|-----------:|------:|----------:|------------:|
|    TypeMapperString |             .NET 6.0 |  8.2913 ns |  8.2240 ns | 2.156 |         - |          NA |
|  DirectAccessString |             .NET 6.0 |  3.2300 ns |  3.1494 ns | 0.852 |         - |          NA |
|       TypeMapperInt |             .NET 6.0 |  5.1331 ns |  5.1664 ns | 1.346 |         - |          NA |
|     DirectAccessInt |             .NET 6.0 |  0.8426 ns |  0.9242 ns | 0.143 |         - |          NA |
|   TypeMapperBoolean |             .NET 6.0 |  5.1892 ns |  5.2834 ns | 1.361 |         - |          NA |
| DirectAccessBoolean |             .NET 6.0 |  0.9114 ns |  0.9166 ns | 0.239 |         - |          NA |
|   TypeMapperWrapper |             .NET 6.0 |  8.8576 ns |  8.8838 ns | 2.322 |         - |          NA |
| DirectAccessWrapper |             .NET 6.0 |  3.2969 ns |  3.3360 ns | 0.865 |         - |          NA |
|    TypeMapperString |             .NET 7.0 |  8.8940 ns |  8.9632 ns | 2.336 |         - |          NA |
|  DirectAccessString |             .NET 7.0 |  4.1090 ns |  4.0424 ns | 1.079 |         - |          NA |
|       TypeMapperInt |             .NET 7.0 |  4.5648 ns |  4.7135 ns | 1.191 |         - |          NA |
|     DirectAccessInt |             .NET 7.0 |  0.0000 ns |  0.0000 ns | 0.000 |         - |          NA |
|   TypeMapperBoolean |             .NET 7.0 |  4.6678 ns |  4.6782 ns | 1.225 |         - |          NA |
| DirectAccessBoolean |             .NET 7.0 |  1.2439 ns |  1.2331 ns | 0.327 |         - |          NA |
|   TypeMapperWrapper |             .NET 7.0 |  8.7174 ns |  8.4770 ns | 2.291 |         - |          NA |
| DirectAccessWrapper |             .NET 7.0 |  4.5949 ns |  5.5111 ns | 1.186 |         - |          NA |
|    TypeMapperString |        .NET Core 3.1 |  8.8838 ns |  8.9302 ns | 2.333 |         - |          NA |
|  DirectAccessString |        .NET Core 3.1 |  3.2968 ns |  3.3386 ns | 0.864 |         - |          NA |
|       TypeMapperInt |        .NET Core 3.1 |  5.6378 ns |  5.6213 ns | 1.477 |         - |          NA |
|     DirectAccessInt |        .NET Core 3.1 |  0.9093 ns |  0.9175 ns | 0.238 |         - |          NA |
|   TypeMapperBoolean |        .NET Core 3.1 |  6.9669 ns |  7.3973 ns | 1.394 |         - |          NA |
| DirectAccessBoolean |        .NET Core 3.1 |  0.9199 ns |  0.9369 ns | 0.241 |         - |          NA |
|   TypeMapperWrapper |        .NET Core 3.1 |  7.8582 ns |  7.5833 ns | 2.046 |         - |          NA |
| DirectAccessWrapper |        .NET Core 3.1 |  3.2992 ns |  3.3386 ns | 0.866 |         - |          NA |
|    TypeMapperString | .NET Framework 4.7.2 | 24.8421 ns | 25.0118 ns | 6.094 |         - |          NA |
|  DirectAccessString | .NET Framework 4.7.2 |  3.8127 ns |  3.8389 ns | 1.000 |         - |          NA |
|       TypeMapperInt | .NET Framework 4.7.2 | 23.4360 ns | 23.5438 ns | 6.137 |         - |          NA |
|     DirectAccessInt | .NET Framework 4.7.2 |  0.9621 ns |  0.9721 ns | 0.252 |         - |          NA |
|   TypeMapperBoolean | .NET Framework 4.7.2 | 23.3173 ns | 23.3494 ns | 6.124 |         - |          NA |
| DirectAccessBoolean | .NET Framework 4.7.2 |  0.9815 ns |  0.9862 ns | 0.257 |         - |          NA |
|   TypeMapperWrapper | .NET Framework 4.7.2 | 31.3029 ns | 30.7947 ns | 8.327 |         - |          NA |
| DirectAccessWrapper | .NET Framework 4.7.2 |  4.2145 ns |  4.2527 ns | 1.106 |         - |          NA |
