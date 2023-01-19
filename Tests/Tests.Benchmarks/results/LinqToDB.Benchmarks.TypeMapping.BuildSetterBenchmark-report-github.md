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
|                  Method |              Runtime |       Mean |     Median | Ratio | Allocated | Alloc Ratio |
|------------------------ |--------------------- |-----------:|-----------:|------:|----------:|------------:|
|        TypeMapperAsEnum |             .NET 6.0 |  9.8892 ns | 10.5470 ns | 24.36 |         - |          NA |
|      DirectAccessAsEnum |             .NET 6.0 |  0.9129 ns |  0.9128 ns |  2.24 |         - |          NA |
|   TypeMapperAsKnownEnum |             .NET 6.0 |  2.3342 ns |  2.2835 ns |  5.76 |         - |          NA |
| DirectAccessAsKnownEnum |             .NET 6.0 |  0.9131 ns |  0.9131 ns |  2.24 |         - |          NA |
|      TypeMapperAsString |             .NET 6.0 |  4.5652 ns |  4.5651 ns | 11.21 |         - |          NA |
|    DirectAccessAsString |             .NET 6.0 |  3.1960 ns |  3.1960 ns |  7.84 |         - |          NA |
|        TypeMapperAsEnum |             .NET 7.0 | 11.2097 ns | 11.2096 ns | 27.51 |         - |          NA |
|      DirectAccessAsEnum |             .NET 7.0 |  0.4280 ns |  0.4266 ns |  1.05 |         - |          NA |
|   TypeMapperAsKnownEnum |             .NET 7.0 |  2.2924 ns |  2.3682 ns |  5.85 |         - |          NA |
| DirectAccessAsKnownEnum |             .NET 7.0 |  0.4685 ns |  0.3943 ns |  1.09 |         - |          NA |
|      TypeMapperAsString |             .NET 7.0 |  3.7903 ns |  5.0815 ns |  7.38 |         - |          NA |
|    DirectAccessAsString |             .NET 7.0 |  4.6547 ns |  4.5670 ns | 11.37 |         - |          NA |
|        TypeMapperAsEnum |        .NET Core 3.1 | 13.8704 ns | 13.9131 ns | 34.03 |         - |          NA |
|      DirectAccessAsEnum |        .NET Core 3.1 |  1.4317 ns |  1.3704 ns |  3.57 |         - |          NA |
|   TypeMapperAsKnownEnum |        .NET Core 3.1 |  2.2061 ns |  2.5211 ns |  5.82 |         - |          NA |
| DirectAccessAsKnownEnum |        .NET Core 3.1 |  1.3693 ns |  1.3693 ns |  3.36 |         - |          NA |
|      TypeMapperAsString |        .NET Core 3.1 |  4.6130 ns |  4.5679 ns | 11.32 |         - |          NA |
|    DirectAccessAsString |        .NET Core 3.1 |  3.1967 ns |  3.1966 ns |  7.85 |         - |          NA |
|        TypeMapperAsEnum | .NET Framework 4.7.2 | 31.5069 ns | 31.5047 ns | 77.34 |         - |          NA |
|      DirectAccessAsEnum | .NET Framework 4.7.2 |  0.4074 ns |  0.4074 ns |  1.00 |         - |          NA |
|   TypeMapperAsKnownEnum | .NET Framework 4.7.2 |  9.6396 ns |  9.5648 ns | 23.65 |         - |          NA |
| DirectAccessAsKnownEnum | .NET Framework 4.7.2 |  0.7712 ns |  0.8889 ns |  2.02 |         - |          NA |
|      TypeMapperAsString | .NET Framework 4.7.2 | 11.0475 ns | 11.1955 ns | 24.55 |         - |          NA |
|    DirectAccessAsString | .NET Framework 4.7.2 |  3.6601 ns |  3.6252 ns |  8.98 |         - |          NA |
