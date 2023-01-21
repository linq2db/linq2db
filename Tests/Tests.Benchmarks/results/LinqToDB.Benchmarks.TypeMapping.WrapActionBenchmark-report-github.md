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
|                          Method |              Runtime |       Mean |     Median | Ratio | Allocated | Alloc Ratio |
|-------------------------------- |--------------------- |-----------:|-----------:|------:|----------:|------------:|
|                TypeMapperAction |             .NET 6.0 |  6.1271 ns |  6.2812 ns |     ? |         - |           ? |
|              DirectAccessAction |             .NET 6.0 |  0.8612 ns |  1.0699 ns |     ? |         - |           ? |
|        TypeMapperActionWithCast |             .NET 6.0 |  4.0292 ns |  4.6239 ns |     ? |         - |           ? |
|      DirectAccessActionWithCast |             .NET 6.0 |  0.3273 ns |  0.3754 ns |     ? |         - |           ? |
|   TypeMapperActionWithParameter |             .NET 6.0 |  5.7550 ns |  6.1315 ns |     ? |         - |           ? |
| DirectAccessActionWithParameter |             .NET 6.0 |  1.3939 ns |  1.6708 ns |     ? |         - |           ? |
|                TypeMapperAction |             .NET 7.0 |  5.3951 ns |  6.0233 ns |     ? |         - |           ? |
|              DirectAccessAction |             .NET 7.0 |  0.4759 ns |  0.5255 ns |     ? |         - |           ? |
|        TypeMapperActionWithCast |             .NET 7.0 |  4.8465 ns |  5.0045 ns |     ? |         - |           ? |
|      DirectAccessActionWithCast |             .NET 7.0 |  0.4619 ns |  0.5015 ns |     ? |         - |           ? |
|   TypeMapperActionWithParameter |             .NET 7.0 |  5.2330 ns |  5.5940 ns |     ? |         - |           ? |
| DirectAccessActionWithParameter |             .NET 7.0 |  0.2561 ns |  0.1692 ns |     ? |         - |           ? |
|                TypeMapperAction |        .NET Core 3.1 |  5.1746 ns |  5.8910 ns |     ? |         - |           ? |
|              DirectAccessAction |        .NET Core 3.1 |  1.0292 ns |  1.1073 ns |     ? |         - |           ? |
|        TypeMapperActionWithCast |        .NET Core 3.1 |  4.2728 ns |  5.1345 ns |     ? |         - |           ? |
|      DirectAccessActionWithCast |        .NET Core 3.1 |  0.5221 ns |  0.5380 ns |     ? |         - |           ? |
|   TypeMapperActionWithParameter |        .NET Core 3.1 |  8.1615 ns |  8.6497 ns |     ? |         - |           ? |
| DirectAccessActionWithParameter |        .NET Core 3.1 |  1.8937 ns |  1.9996 ns |     ? |         - |           ? |
|                TypeMapperAction | .NET Framework 4.7.2 | 25.0610 ns | 27.5379 ns |     ? |         - |           ? |
|              DirectAccessAction | .NET Framework 4.7.2 |  1.3276 ns |  1.5126 ns |     ? |         - |           ? |
|        TypeMapperActionWithCast | .NET Framework 4.7.2 | 17.7987 ns | 19.2937 ns |     ? |         - |           ? |
|      DirectAccessActionWithCast | .NET Framework 4.7.2 |  1.0586 ns |  1.0573 ns |     ? |         - |           ? |
|   TypeMapperActionWithParameter | .NET Framework 4.7.2 | 21.1265 ns | 19.8089 ns |     ? |         - |           ? |
| DirectAccessActionWithParameter | .NET Framework 4.7.2 |  2.0775 ns |  2.3171 ns |     ? |         - |           ? |
