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
|                          Method |              Runtime |       Mean |     Median | Ratio | Allocated | Alloc Ratio |
|-------------------------------- |--------------------- |-----------:|-----------:|------:|----------:|------------:|
|                TypeMapperAction |             .NET 6.0 |  5.0221 ns |  5.0220 ns |  5.67 |         - |          NA |
|              DirectAccessAction |             .NET 6.0 |  0.9466 ns |  0.9452 ns |  1.08 |         - |          NA |
|        TypeMapperActionWithCast |             .NET 6.0 |  4.0788 ns |  4.1098 ns |  4.66 |         - |          NA |
|      DirectAccessActionWithCast |             .NET 6.0 |  0.4563 ns |  0.4565 ns |  0.52 |         - |          NA |
|   TypeMapperActionWithParameter |             .NET 6.0 |  5.9361 ns |  5.9359 ns |  6.71 |         - |          NA |
| DirectAccessActionWithParameter |             .NET 6.0 |  0.9133 ns |  0.9133 ns |  1.03 |         - |          NA |
|                TypeMapperAction |             .NET 7.0 |  5.0240 ns |  5.0240 ns |  5.68 |         - |          NA |
|              DirectAccessAction |             .NET 7.0 |  0.4575 ns |  0.4575 ns |  0.52 |         - |          NA |
|        TypeMapperActionWithCast |             .NET 7.0 |  4.5334 ns |  5.0234 ns |  5.12 |         - |          NA |
|      DirectAccessActionWithCast |             .NET 7.0 |  0.4567 ns |  0.4566 ns |  0.52 |         - |          NA |
|   TypeMapperActionWithParameter |             .NET 7.0 |  4.8060 ns |  4.8062 ns |  5.50 |         - |          NA |
| DirectAccessActionWithParameter |             .NET 7.0 |  0.7025 ns |  0.9124 ns |  0.76 |         - |          NA |
|                TypeMapperAction |        .NET Core 3.1 |  5.9700 ns |  5.9699 ns |  6.74 |         - |          NA |
|              DirectAccessAction |        .NET Core 3.1 |  1.1184 ns |  1.1272 ns |  1.26 |         - |          NA |
|        TypeMapperActionWithCast |        .NET Core 3.1 |  5.2027 ns |  5.1919 ns |  5.96 |         - |          NA |
|      DirectAccessActionWithCast |        .NET Core 3.1 |  0.4610 ns |  0.4566 ns |  0.52 |         - |          NA |
|   TypeMapperActionWithParameter |        .NET Core 3.1 |  6.5621 ns |  6.5903 ns |  7.53 |         - |          NA |
| DirectAccessActionWithParameter |        .NET Core 3.1 |  0.9137 ns |  0.9137 ns |  1.03 |         - |          NA |
|                TypeMapperAction | .NET Framework 4.7.2 | 10.3796 ns | 10.3461 ns | 11.72 |         - |          NA |
|              DirectAccessAction | .NET Framework 4.7.2 |  0.8749 ns |  0.8852 ns |  1.00 |         - |          NA |
|        TypeMapperActionWithCast | .NET Framework 4.7.2 | 14.9782 ns | 15.0966 ns | 17.12 |         - |          NA |
|      DirectAccessActionWithCast | .NET Framework 4.7.2 |  1.6524 ns |  2.1982 ns |  1.87 |         - |          NA |
|   TypeMapperActionWithParameter | .NET Framework 4.7.2 | 22.8891 ns | 22.8893 ns | 25.85 |         - |          NA |
| DirectAccessActionWithParameter | .NET Framework 4.7.2 |  0.7578 ns |  0.8523 ns |  0.93 |         - |          NA |
