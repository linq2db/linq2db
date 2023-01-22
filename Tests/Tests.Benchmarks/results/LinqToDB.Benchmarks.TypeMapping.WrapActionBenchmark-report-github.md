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
|                          Method |              Runtime |       Mean |     Median | Ratio | Allocated | Alloc Ratio |
|-------------------------------- |--------------------- |-----------:|-----------:|------:|----------:|------------:|
|                TypeMapperAction |             .NET 6.0 |  5.5915 ns |  5.5968 ns |  4.01 |         - |          NA |
|              DirectAccessAction |             .NET 6.0 |  0.9071 ns |  0.9145 ns |  0.65 |         - |          NA |
|        TypeMapperActionWithCast |             .NET 6.0 |  4.7006 ns |  4.7065 ns |  3.35 |         - |          NA |
|      DirectAccessActionWithCast |             .NET 6.0 |  0.2890 ns |  0.4303 ns |  0.12 |         - |          NA |
|   TypeMapperActionWithParameter |             .NET 6.0 |  6.1136 ns |  6.1849 ns |  4.39 |         - |          NA |
| DirectAccessActionWithParameter |             .NET 6.0 |  1.2226 ns |  1.2420 ns |  0.88 |         - |          NA |
|                TypeMapperAction |             .NET 7.0 |  6.5009 ns |  6.4885 ns |  4.65 |         - |          NA |
|              DirectAccessAction |             .NET 7.0 |  0.4790 ns |  0.4960 ns |  0.35 |         - |          NA |
|        TypeMapperActionWithCast |             .NET 7.0 |  4.5060 ns |  4.4958 ns |  3.23 |         - |          NA |
|      DirectAccessActionWithCast |             .NET 7.0 |  0.3021 ns |  0.2587 ns |  0.21 |         - |          NA |
|   TypeMapperActionWithParameter |             .NET 7.0 |  5.7133 ns |  5.6765 ns |  4.09 |         - |          NA |
| DirectAccessActionWithParameter |             .NET 7.0 |  0.5386 ns |  0.5377 ns |  0.39 |         - |          NA |
|                TypeMapperAction |        .NET Core 3.1 |  4.8110 ns |  5.1268 ns |  2.96 |         - |          NA |
|              DirectAccessAction |        .NET Core 3.1 |  0.9302 ns |  0.9395 ns |  0.67 |         - |          NA |
|        TypeMapperActionWithCast |        .NET Core 3.1 |  4.6389 ns |  4.7051 ns |  3.27 |         - |          NA |
|      DirectAccessActionWithCast |        .NET Core 3.1 |  0.4500 ns |  0.4574 ns |  0.32 |         - |          NA |
|   TypeMapperActionWithParameter |        .NET Core 3.1 |  6.8821 ns |  6.8949 ns |  4.92 |         - |          NA |
| DirectAccessActionWithParameter |        .NET Core 3.1 |  1.2599 ns |  1.2756 ns |  0.89 |         - |          NA |
|                TypeMapperAction | .NET Framework 4.7.2 | 20.0420 ns | 23.8985 ns | 15.47 |         - |          NA |
|              DirectAccessAction | .NET Framework 4.7.2 |  1.4003 ns |  1.4063 ns |  1.00 |         - |          NA |
|        TypeMapperActionWithCast | .NET Framework 4.7.2 | 15.8489 ns | 16.0238 ns | 11.34 |         - |          NA |
|      DirectAccessActionWithCast | .NET Framework 4.7.2 |  2.4418 ns |  2.5053 ns |  1.77 |         - |          NA |
|   TypeMapperActionWithParameter | .NET Framework 4.7.2 | 23.8500 ns | 23.8239 ns | 17.12 |         - |          NA |
| DirectAccessActionWithParameter | .NET Framework 4.7.2 |  1.4439 ns |  1.4519 ns |  1.03 |         - |          NA |
