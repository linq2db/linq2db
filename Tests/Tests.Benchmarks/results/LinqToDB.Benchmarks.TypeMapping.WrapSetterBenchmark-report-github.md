``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-HCNGBR : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-XBFFOD : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-INBZNN : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-THZJXI : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|              Method |              Runtime |       Mean | Allocated |
|-------------------- |--------------------- |-----------:|----------:|
|    TypeMapperString |             .NET 6.0 |  7.5954 ns |         - |
|  DirectAccessString |             .NET 6.0 |  3.2727 ns |         - |
|       TypeMapperInt |             .NET 6.0 |  5.2403 ns |         - |
|     DirectAccessInt |             .NET 6.0 |  0.9547 ns |         - |
|   TypeMapperBoolean |             .NET 6.0 |  5.1427 ns |         - |
| DirectAccessBoolean |             .NET 6.0 |  0.9071 ns |         - |
|   TypeMapperWrapper |             .NET 6.0 |  8.9323 ns |         - |
| DirectAccessWrapper |             .NET 6.0 |  3.2888 ns |         - |
|    TypeMapperString |             .NET 7.0 |  8.8474 ns |         - |
|  DirectAccessString |             .NET 7.0 |  4.7144 ns |         - |
|       TypeMapperInt |             .NET 7.0 |  5.1957 ns |         - |
|     DirectAccessInt |             .NET 7.0 |  0.4657 ns |         - |
|   TypeMapperBoolean |             .NET 7.0 |  4.4941 ns |         - |
| DirectAccessBoolean |             .NET 7.0 |  1.4928 ns |         - |
|   TypeMapperWrapper |             .NET 7.0 |  8.7192 ns |         - |
| DirectAccessWrapper |             .NET 7.0 |  4.1103 ns |         - |
|    TypeMapperString |        .NET Core 3.1 |  8.9096 ns |         - |
|  DirectAccessString |        .NET Core 3.1 |  3.2237 ns |         - |
|       TypeMapperInt |        .NET Core 3.1 |  5.3810 ns |         - |
|     DirectAccessInt |        .NET Core 3.1 |  0.9316 ns |         - |
|   TypeMapperBoolean |        .NET Core 3.1 |  6.1028 ns |         - |
| DirectAccessBoolean |        .NET Core 3.1 |  0.5596 ns |         - |
|   TypeMapperWrapper |        .NET Core 3.1 |  8.9114 ns |         - |
| DirectAccessWrapper |        .NET Core 3.1 |  3.2735 ns |         - |
|    TypeMapperString | .NET Framework 4.7.2 | 25.9124 ns |         - |
|  DirectAccessString | .NET Framework 4.7.2 |  4.4159 ns |         - |
|       TypeMapperInt | .NET Framework 4.7.2 | 20.9236 ns |         - |
|     DirectAccessInt | .NET Framework 4.7.2 |  0.8988 ns |         - |
|   TypeMapperBoolean | .NET Framework 4.7.2 | 23.5435 ns |         - |
| DirectAccessBoolean | .NET Framework 4.7.2 |  1.0420 ns |         - |
|   TypeMapperWrapper | .NET Framework 4.7.2 | 31.9145 ns |         - |
| DirectAccessWrapper | .NET Framework 4.7.2 |  4.8932 ns |         - |
