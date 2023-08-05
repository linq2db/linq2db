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
|    TypeMapperString |             .NET 6.0 |  4.7790 ns |         - |
|  DirectAccessString |             .NET 6.0 |  2.5687 ns |         - |
|       TypeMapperInt |             .NET 6.0 |  6.1492 ns |         - |
|     DirectAccessInt |             .NET 6.0 |  1.2641 ns |         - |
|      TypeMapperLong |             .NET 6.0 |  5.4502 ns |         - |
|    DirectAccessLong |             .NET 6.0 |  0.8531 ns |         - |
|   TypeMapperBoolean |             .NET 6.0 |  5.1855 ns |         - |
| DirectAccessBoolean |             .NET 6.0 |  0.9255 ns |         - |
|   TypeMapperWrapper |             .NET 6.0 | 13.6948 ns |         - |
| DirectAccessWrapper |             .NET 6.0 |  0.9378 ns |         - |
|      TypeMapperEnum |             .NET 6.0 | 29.2219 ns |      24 B |
|    DirectAccessEnum |             .NET 6.0 |  0.9017 ns |         - |
|   TypeMapperVersion |             .NET 6.0 |  6.1379 ns |         - |
| DirectAccessVersion |             .NET 6.0 |  1.0164 ns |         - |
|    TypeMapperString |             .NET 7.0 |  5.1813 ns |         - |
|  DirectAccessString |             .NET 7.0 |  0.4870 ns |         - |
|       TypeMapperInt |             .NET 7.0 |  5.1528 ns |         - |
|     DirectAccessInt |             .NET 7.0 |  0.4966 ns |         - |
|      TypeMapperLong |             .NET 7.0 |  5.0605 ns |         - |
|    DirectAccessLong |             .NET 7.0 |  0.4264 ns |         - |
|   TypeMapperBoolean |             .NET 7.0 |  5.0154 ns |         - |
| DirectAccessBoolean |             .NET 7.0 |  0.5628 ns |         - |
|   TypeMapperWrapper |             .NET 7.0 | 12.3221 ns |         - |
| DirectAccessWrapper |             .NET 7.0 |  0.5399 ns |         - |
|      TypeMapperEnum |             .NET 7.0 | 15.7713 ns |         - |
|    DirectAccessEnum |             .NET 7.0 |  0.4871 ns |         - |
|   TypeMapperVersion |             .NET 7.0 |  5.2495 ns |         - |
| DirectAccessVersion |             .NET 7.0 |  0.4155 ns |         - |
|    TypeMapperString |        .NET Core 3.1 |  6.2582 ns |         - |
|  DirectAccessString |        .NET Core 3.1 |  0.8734 ns |         - |
|       TypeMapperInt |        .NET Core 3.1 |  6.0442 ns |         - |
|     DirectAccessInt |        .NET Core 3.1 |  0.8182 ns |         - |
|      TypeMapperLong |        .NET Core 3.1 |  5.1034 ns |         - |
|    DirectAccessLong |        .NET Core 3.1 |  0.8332 ns |         - |
|   TypeMapperBoolean |        .NET Core 3.1 |  5.9419 ns |         - |
| DirectAccessBoolean |        .NET Core 3.1 |  2.0515 ns |         - |
|   TypeMapperWrapper |        .NET Core 3.1 | 15.5734 ns |         - |
| DirectAccessWrapper |        .NET Core 3.1 |  2.6162 ns |         - |
|      TypeMapperEnum |        .NET Core 3.1 | 35.5439 ns |      24 B |
|    DirectAccessEnum |        .NET Core 3.1 |  1.2395 ns |         - |
|   TypeMapperVersion |        .NET Core 3.1 |  5.7658 ns |         - |
| DirectAccessVersion |        .NET Core 3.1 |  0.9075 ns |         - |
|    TypeMapperString | .NET Framework 4.7.2 | 23.7345 ns |         - |
|  DirectAccessString | .NET Framework 4.7.2 |  0.5539 ns |         - |
|       TypeMapperInt | .NET Framework 4.7.2 | 23.3922 ns |         - |
|     DirectAccessInt | .NET Framework 4.7.2 |  1.0062 ns |         - |
|      TypeMapperLong | .NET Framework 4.7.2 | 22.7812 ns |         - |
|    DirectAccessLong | .NET Framework 4.7.2 |  0.9087 ns |         - |
|   TypeMapperBoolean | .NET Framework 4.7.2 | 22.5442 ns |         - |
| DirectAccessBoolean | .NET Framework 4.7.2 |  0.8425 ns |         - |
|   TypeMapperWrapper | .NET Framework 4.7.2 | 32.6655 ns |         - |
| DirectAccessWrapper | .NET Framework 4.7.2 |  0.8383 ns |         - |
|      TypeMapperEnum | .NET Framework 4.7.2 | 62.4537 ns |      24 B |
|    DirectAccessEnum | .NET Framework 4.7.2 |  0.6240 ns |         - |
|   TypeMapperVersion | .NET Framework 4.7.2 | 23.2775 ns |         - |
| DirectAccessVersion | .NET Framework 4.7.2 |  0.9491 ns |         - |
