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
|                  Method |              Runtime |       Mean |     Median |  Ratio | Allocated | Alloc Ratio |
|------------------------ |--------------------- |-----------:|-----------:|-------:|----------:|------------:|
|        TypeMapperAsEnum |             .NET 6.0 | 10.7278 ns | 10.8353 ns |  7.535 |         - |          NA |
|      DirectAccessAsEnum |             .NET 6.0 |  0.8750 ns |  0.9243 ns |  0.588 |         - |          NA |
|   TypeMapperAsKnownEnum |             .NET 6.0 |  2.3381 ns |  2.3577 ns |  1.640 |         - |          NA |
| DirectAccessAsKnownEnum |             .NET 6.0 |  0.9294 ns |  0.9217 ns |  0.652 |         - |          NA |
|      TypeMapperAsString |             .NET 6.0 |  4.3509 ns |  4.6933 ns |  1.875 |         - |          NA |
|    DirectAccessAsString |             .NET 6.0 |  3.6520 ns |  3.6516 ns |  2.571 |         - |          NA |
|        TypeMapperAsEnum |             .NET 7.0 | 10.1589 ns | 10.1360 ns |  7.124 |         - |          NA |
|      DirectAccessAsEnum |             .NET 7.0 |  0.4547 ns |  0.4732 ns |  0.320 |         - |          NA |
|   TypeMapperAsKnownEnum |             .NET 7.0 |  2.0423 ns |  2.1064 ns |  1.324 |         - |          NA |
| DirectAccessAsKnownEnum |             .NET 7.0 |  1.5307 ns |  1.8479 ns |  0.600 |         - |          NA |
|      TypeMapperAsString |             .NET 7.0 |  6.1100 ns |  6.0359 ns |  4.308 |         - |          NA |
|    DirectAccessAsString |             .NET 7.0 |  4.6251 ns |  4.6192 ns |  3.243 |         - |          NA |
|        TypeMapperAsEnum |        .NET Core 3.1 | 14.3870 ns | 14.4670 ns | 10.098 |         - |          NA |
|      DirectAccessAsEnum |        .NET Core 3.1 |  0.0000 ns |  0.0000 ns |  0.000 |         - |          NA |
|   TypeMapperAsKnownEnum |        .NET Core 3.1 |  1.8970 ns |  1.8312 ns |  1.330 |         - |          NA |
| DirectAccessAsKnownEnum |        .NET Core 3.1 |  0.7687 ns |  0.7726 ns |  0.539 |         - |          NA |
|      TypeMapperAsString |        .NET Core 3.1 |  5.5991 ns |  5.6026 ns |  3.930 |         - |          NA |
|    DirectAccessAsString |        .NET Core 3.1 |  3.7760 ns |  3.7844 ns |  2.649 |         - |          NA |
|        TypeMapperAsEnum | .NET Framework 4.7.2 | 15.2116 ns | 15.1531 ns | 10.733 |         - |          NA |
|      DirectAccessAsEnum | .NET Framework 4.7.2 |  1.4240 ns |  1.4241 ns |  1.000 |         - |          NA |
|   TypeMapperAsKnownEnum | .NET Framework 4.7.2 | 10.3218 ns | 10.3585 ns |  7.268 |         - |          NA |
| DirectAccessAsKnownEnum | .NET Framework 4.7.2 |  1.4083 ns |  1.4507 ns |  0.988 |         - |          NA |
|      TypeMapperAsString | .NET Framework 4.7.2 | 13.1272 ns | 13.1282 ns |  9.211 |         - |          NA |
|    DirectAccessAsString | .NET Framework 4.7.2 |  4.1373 ns |  4.1381 ns |  2.904 |         - |          NA |
