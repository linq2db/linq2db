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
|                  Method |              Runtime |       Mean |     Median |  Ratio | Allocated | Alloc Ratio |
|------------------------ |--------------------- |-----------:|-----------:|-------:|----------:|------------:|
|        TypeMapperAsEnum |             .NET 6.0 | 10.5509 ns | 10.8561 ns |  6.975 |         - |          NA |
|      DirectAccessAsEnum |             .NET 6.0 |  2.1429 ns |  2.1923 ns |  1.295 |         - |          NA |
|   TypeMapperAsKnownEnum |             .NET 6.0 |  2.3910 ns |  2.4195 ns |  1.615 |         - |          NA |
| DirectAccessAsKnownEnum |             .NET 6.0 |  0.9188 ns |  0.9157 ns |  0.625 |         - |          NA |
|      TypeMapperAsString |             .NET 6.0 |  4.6939 ns |  4.6578 ns |  3.194 |         - |          NA |
|    DirectAccessAsString |             .NET 6.0 |  3.1847 ns |  3.1978 ns |  2.162 |         - |          NA |
|        TypeMapperAsEnum |             .NET 7.0 | 11.3734 ns | 11.4994 ns |  7.701 |         - |          NA |
|      DirectAccessAsEnum |             .NET 7.0 |  0.0000 ns |  0.0000 ns |  0.000 |         - |          NA |
|   TypeMapperAsKnownEnum |             .NET 7.0 |  2.1875 ns |  2.1820 ns |  1.489 |         - |          NA |
| DirectAccessAsKnownEnum |             .NET 7.0 |  0.3624 ns |  0.3274 ns |  0.224 |         - |          NA |
|      TypeMapperAsString |             .NET 7.0 |  5.9799 ns |  6.0329 ns |  4.053 |         - |          NA |
|    DirectAccessAsString |             .NET 7.0 |  4.8338 ns |  4.7930 ns |  3.294 |         - |          NA |
|        TypeMapperAsEnum |        .NET Core 3.1 | 13.9534 ns | 13.9696 ns |  9.493 |         - |          NA |
|      DirectAccessAsEnum |        .NET Core 3.1 |  1.1408 ns |  1.2050 ns |  0.748 |         - |          NA |
|   TypeMapperAsKnownEnum |        .NET Core 3.1 |  1.9519 ns |  1.9682 ns |  1.331 |         - |          NA |
| DirectAccessAsKnownEnum |        .NET Core 3.1 |  0.9288 ns |  0.9215 ns |  0.631 |         - |          NA |
|      TypeMapperAsString |        .NET Core 3.1 |  5.6134 ns |  5.6257 ns |  3.822 |         - |          NA |
|    DirectAccessAsString |        .NET Core 3.1 |  2.4891 ns |  2.6504 ns |  1.910 |         - |          NA |
|        TypeMapperAsEnum | .NET Framework 4.7.2 | 33.0749 ns | 33.4428 ns | 22.514 |         - |          NA |
|      DirectAccessAsEnum | .NET Framework 4.7.2 |  1.4726 ns |  1.4955 ns |  1.000 |         - |          NA |
|   TypeMapperAsKnownEnum | .NET Framework 4.7.2 | 10.3253 ns | 10.3641 ns |  7.038 |         - |          NA |
| DirectAccessAsKnownEnum | .NET Framework 4.7.2 |  1.3412 ns |  1.3486 ns |  0.902 |         - |          NA |
|      TypeMapperAsString | .NET Framework 4.7.2 | 13.2012 ns | 13.1818 ns |  8.979 |         - |          NA |
|    DirectAccessAsString | .NET Framework 4.7.2 |  4.2818 ns |  4.2598 ns |  2.915 |         - |          NA |
