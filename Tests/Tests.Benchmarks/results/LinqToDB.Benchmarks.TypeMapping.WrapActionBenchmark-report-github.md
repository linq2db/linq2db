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
|                          Method |              Runtime |       Mean |     Median | Ratio | Allocated | Alloc Ratio |
|-------------------------------- |--------------------- |-----------:|-----------:|------:|----------:|------------:|
|                TypeMapperAction |             .NET 6.0 |  5.0328 ns |  5.0331 ns |  5.10 |         - |          NA |
|              DirectAccessAction |             .NET 6.0 |  0.9153 ns |  0.9150 ns |  0.92 |         - |          NA |
|        TypeMapperActionWithCast |             .NET 6.0 |  4.5415 ns |  4.5756 ns |  4.59 |         - |          NA |
|      DirectAccessActionWithCast |             .NET 6.0 |  0.3217 ns |  0.4575 ns |  0.34 |         - |          NA |
|   TypeMapperActionWithParameter |             .NET 6.0 |  5.9052 ns |  5.9471 ns |  5.98 |         - |          NA |
| DirectAccessActionWithParameter |             .NET 6.0 |  1.3732 ns |  1.3729 ns |  1.39 |         - |          NA |
|                TypeMapperAction |             .NET 7.0 |  5.0544 ns |  5.0328 ns |  5.12 |         - |          NA |
|              DirectAccessAction |             .NET 7.0 |  0.4586 ns |  0.4589 ns |  0.46 |         - |          NA |
|        TypeMapperActionWithCast |             .NET 7.0 |  5.0415 ns |  5.0339 ns |  5.09 |         - |          NA |
|      DirectAccessActionWithCast |             .NET 7.0 |  0.4970 ns |  0.4942 ns |  0.50 |         - |          NA |
|   TypeMapperActionWithParameter |             .NET 7.0 |  6.2793 ns |  6.5681 ns |  4.69 |         - |          NA |
| DirectAccessActionWithParameter |             .NET 7.0 |  0.4717 ns |  0.4652 ns |  0.48 |         - |          NA |
|                TypeMapperAction |        .NET Core 3.1 |  6.1215 ns |  6.1685 ns |  6.21 |         - |          NA |
|              DirectAccessAction |        .NET Core 3.1 |  1.3633 ns |  1.3296 ns |  1.38 |         - |          NA |
|        TypeMapperActionWithCast |        .NET Core 3.1 |  4.6984 ns |  4.7181 ns |  4.76 |         - |          NA |
|      DirectAccessActionWithCast |        .NET Core 3.1 |  0.4559 ns |  0.4450 ns |  0.46 |         - |          NA |
|   TypeMapperActionWithParameter |        .NET Core 3.1 |  6.0502 ns |  5.9543 ns |  6.14 |         - |          NA |
| DirectAccessActionWithParameter |        .NET Core 3.1 |  0.9444 ns |  0.9745 ns |  0.94 |         - |          NA |
|                TypeMapperAction | .NET Framework 4.7.2 | 23.3049 ns | 23.2994 ns | 23.64 |         - |          NA |
|              DirectAccessAction | .NET Framework 4.7.2 |  0.9868 ns |  0.9955 ns |  1.00 |         - |          NA |
|        TypeMapperActionWithCast | .NET Framework 4.7.2 | 15.4979 ns | 15.5830 ns | 15.68 |         - |          NA |
|      DirectAccessActionWithCast | .NET Framework 4.7.2 |  0.4449 ns |  0.4448 ns |  0.45 |         - |          NA |
|   TypeMapperActionWithParameter | .NET Framework 4.7.2 | 23.2512 ns | 23.4359 ns | 23.56 |         - |          NA |
| DirectAccessActionWithParameter | .NET Framework 4.7.2 |  0.9485 ns |  0.9970 ns |  0.96 |         - |          NA |
