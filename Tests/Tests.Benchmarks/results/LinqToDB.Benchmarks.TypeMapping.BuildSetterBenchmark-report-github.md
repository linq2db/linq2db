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
|                  Method |              Runtime |       Mean | Allocated |
|------------------------ |--------------------- |-----------:|----------:|
|        TypeMapperAsEnum |             .NET 6.0 | 10.8447 ns |         - |
|      DirectAccessAsEnum |             .NET 6.0 |  0.9273 ns |         - |
|   TypeMapperAsKnownEnum |             .NET 6.0 |  2.3651 ns |         - |
| DirectAccessAsKnownEnum |             .NET 6.0 |  0.9230 ns |         - |
|      TypeMapperAsString |             .NET 6.0 |  4.6430 ns |         - |
|    DirectAccessAsString |             .NET 6.0 |  3.7310 ns |         - |
|        TypeMapperAsEnum |             .NET 7.0 | 11.3765 ns |         - |
|      DirectAccessAsEnum |             .NET 7.0 |  0.3548 ns |         - |
|   TypeMapperAsKnownEnum |             .NET 7.0 |  2.2931 ns |         - |
| DirectAccessAsKnownEnum |             .NET 7.0 |  0.4788 ns |         - |
|      TypeMapperAsString |             .NET 7.0 |  5.7594 ns |         - |
|    DirectAccessAsString |             .NET 7.0 |  4.2254 ns |         - |
|        TypeMapperAsEnum |        .NET Core 3.1 | 13.9558 ns |         - |
|      DirectAccessAsEnum |        .NET Core 3.1 |  0.0000 ns |         - |
|   TypeMapperAsKnownEnum |        .NET Core 3.1 |  1.6702 ns |         - |
| DirectAccessAsKnownEnum |        .NET Core 3.1 |  0.9249 ns |         - |
|      TypeMapperAsString |        .NET Core 3.1 |  3.8857 ns |         - |
|    DirectAccessAsString |        .NET Core 3.1 |  3.2884 ns |         - |
|        TypeMapperAsEnum | .NET Framework 4.7.2 | 33.0450 ns |         - |
|      DirectAccessAsEnum | .NET Framework 4.7.2 |  1.2453 ns |         - |
|   TypeMapperAsKnownEnum | .NET Framework 4.7.2 |  9.3493 ns |         - |
| DirectAccessAsKnownEnum | .NET Framework 4.7.2 |  1.3847 ns |         - |
|      TypeMapperAsString | .NET Framework 4.7.2 | 13.2547 ns |         - |
|    DirectAccessAsString | .NET Framework 4.7.2 |  4.0999 ns |         - |
