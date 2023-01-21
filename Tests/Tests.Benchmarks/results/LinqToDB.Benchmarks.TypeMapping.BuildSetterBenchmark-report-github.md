``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-RNZPMW : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-XCCWXF : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-WSMVMG : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-FMTKFQ : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                  Method |              Runtime |       Mean |     Median | Ratio | Allocated | Alloc Ratio |
|------------------------ |--------------------- |-----------:|-----------:|------:|----------:|------------:|
|        TypeMapperAsEnum |             .NET 6.0 | 14.5759 ns | 15.0492 ns |     ? |         - |           ? |
|      DirectAccessAsEnum |             .NET 6.0 |  1.3419 ns |  1.6372 ns |     ? |         - |           ? |
|   TypeMapperAsKnownEnum |             .NET 6.0 |  3.2071 ns |  3.5777 ns |     ? |         - |           ? |
| DirectAccessAsKnownEnum |             .NET 6.0 |  0.9013 ns |  1.0407 ns |     ? |         - |           ? |
|      TypeMapperAsString |             .NET 6.0 |  4.0394 ns |  4.5414 ns |     ? |         - |           ? |
|    DirectAccessAsString |             .NET 6.0 |  3.6456 ns |  4.2112 ns |     ? |         - |           ? |
|        TypeMapperAsEnum |             .NET 7.0 | 12.1619 ns | 12.8767 ns |     ? |         - |           ? |
|      DirectAccessAsEnum |             .NET 7.0 |  0.4353 ns |  0.5087 ns |     ? |         - |           ? |
|   TypeMapperAsKnownEnum |             .NET 7.0 |  3.9070 ns |  4.1812 ns |     ? |         - |           ? |
| DirectAccessAsKnownEnum |             .NET 7.0 |  0.4254 ns |  0.1995 ns |     ? |         - |           ? |
|      TypeMapperAsString |             .NET 7.0 |  6.3168 ns |  7.0824 ns |     ? |         - |           ? |
|    DirectAccessAsString |             .NET 7.0 |  4.9025 ns |  5.4723 ns |     ? |         - |           ? |
|        TypeMapperAsEnum |        .NET Core 3.1 | 13.4681 ns | 14.2267 ns |     ? |         - |           ? |
|      DirectAccessAsEnum |        .NET Core 3.1 |  0.9625 ns |  1.0305 ns |     ? |         - |           ? |
|   TypeMapperAsKnownEnum |        .NET Core 3.1 |  2.6386 ns |  2.7801 ns |     ? |         - |           ? |
| DirectAccessAsKnownEnum |        .NET Core 3.1 |  1.0974 ns |  1.3475 ns |     ? |         - |           ? |
|      TypeMapperAsString |        .NET Core 3.1 |  3.9100 ns |  4.4575 ns |     ? |         - |           ? |
|    DirectAccessAsString |        .NET Core 3.1 |  3.0356 ns |  3.5532 ns |     ? |         - |           ? |
|        TypeMapperAsEnum | .NET Framework 4.7.2 | 39.4204 ns | 41.5883 ns |     ? |         - |           ? |
|      DirectAccessAsEnum | .NET Framework 4.7.2 |  1.9119 ns |  1.9968 ns |     ? |         - |           ? |
|   TypeMapperAsKnownEnum | .NET Framework 4.7.2 | 10.5898 ns | 11.4255 ns |     ? |         - |           ? |
| DirectAccessAsKnownEnum | .NET Framework 4.7.2 |  1.2113 ns |  1.3744 ns |     ? |         - |           ? |
|      TypeMapperAsString | .NET Framework 4.7.2 | 13.2980 ns | 14.2343 ns |     ? |         - |           ? |
|    DirectAccessAsString | .NET Framework 4.7.2 |  4.1320 ns |  4.4944 ns |     ? |         - |           ? |
