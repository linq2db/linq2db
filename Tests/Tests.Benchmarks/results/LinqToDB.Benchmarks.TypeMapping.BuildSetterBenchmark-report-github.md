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
|                  Method |              Runtime |       Mean |     Median | Ratio | Allocated | Alloc Ratio |
|------------------------ |--------------------- |-----------:|-----------:|------:|----------:|------------:|
|        TypeMapperAsEnum |             .NET 6.0 |  9.4801 ns |  9.3870 ns |     ? |         - |           ? |
|      DirectAccessAsEnum |             .NET 6.0 |  0.9703 ns |  0.9853 ns |     ? |         - |           ? |
|   TypeMapperAsKnownEnum |             .NET 6.0 |  2.7959 ns |  2.7469 ns |     ? |         - |           ? |
| DirectAccessAsKnownEnum |             .NET 6.0 |  0.8748 ns |  0.9375 ns |     ? |         - |           ? |
|      TypeMapperAsString |             .NET 6.0 |  4.4749 ns |  4.4795 ns |     ? |         - |           ? |
|    DirectAccessAsString |             .NET 6.0 |  3.2058 ns |  3.2021 ns |     ? |         - |           ? |
|        TypeMapperAsEnum |             .NET 7.0 | 12.9864 ns | 12.9926 ns |     ? |         - |           ? |
|      DirectAccessAsEnum |             .NET 7.0 |  0.4482 ns |  0.4301 ns |     ? |         - |           ? |
|   TypeMapperAsKnownEnum |             .NET 7.0 |  2.2157 ns |  2.2165 ns |     ? |         - |           ? |
| DirectAccessAsKnownEnum |             .NET 7.0 |  0.4778 ns |  0.4777 ns |     ? |         - |           ? |
|      TypeMapperAsString |             .NET 7.0 |  5.3306 ns |  5.3491 ns |     ? |         - |           ? |
|    DirectAccessAsString |             .NET 7.0 |  4.1072 ns |  4.0589 ns |     ? |         - |           ? |
|        TypeMapperAsEnum |        .NET Core 3.1 | 13.7677 ns | 13.7653 ns |     ? |         - |           ? |
|      DirectAccessAsEnum |        .NET Core 3.1 |  0.8591 ns |  0.9262 ns |     ? |         - |           ? |
|   TypeMapperAsKnownEnum |        .NET Core 3.1 |  1.8314 ns |  1.8323 ns |     ? |         - |           ? |
| DirectAccessAsKnownEnum |        .NET Core 3.1 |  0.9084 ns |  0.9213 ns |     ? |         - |           ? |
|      TypeMapperAsString |        .NET Core 3.1 |  4.1662 ns |  4.1186 ns |     ? |         - |           ? |
|    DirectAccessAsString |        .NET Core 3.1 |  3.7349 ns |  3.7724 ns |     ? |         - |           ? |
|        TypeMapperAsEnum | .NET Framework 4.7.2 | 31.8653 ns | 31.5765 ns |     ? |         - |           ? |
|      DirectAccessAsEnum | .NET Framework 4.7.2 |  0.8285 ns |  0.9065 ns |     ? |         - |           ? |
|   TypeMapperAsKnownEnum | .NET Framework 4.7.2 |  9.8130 ns |  9.8656 ns |     ? |         - |           ? |
| DirectAccessAsKnownEnum | .NET Framework 4.7.2 |  0.8185 ns |  0.8093 ns |     ? |         - |           ? |
|      TypeMapperAsString | .NET Framework 4.7.2 | 11.2605 ns | 11.2638 ns |     ? |         - |           ? |
|    DirectAccessAsString | .NET Framework 4.7.2 |  3.6821 ns |  3.6371 ns |     ? |         - |           ? |
