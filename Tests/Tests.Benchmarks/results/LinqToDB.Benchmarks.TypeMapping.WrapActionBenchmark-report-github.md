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
|                          Method |              Runtime |       Mean |     Median | Ratio | Allocated | Alloc Ratio |
|-------------------------------- |--------------------- |-----------:|-----------:|------:|----------:|------------:|
|                TypeMapperAction |             .NET 6.0 |  5.1521 ns |  5.2655 ns |  5.41 |         - |          NA |
|              DirectAccessAction |             .NET 6.0 |  0.8945 ns |  0.9092 ns |  0.94 |         - |          NA |
|        TypeMapperActionWithCast |             .NET 6.0 |  4.1485 ns |  4.1677 ns |  4.37 |         - |          NA |
|      DirectAccessActionWithCast |             .NET 6.0 |  0.4438 ns |  0.4629 ns |  0.47 |         - |          NA |
|   TypeMapperActionWithParameter |             .NET 6.0 |  6.0710 ns |  6.0737 ns |  6.40 |         - |          NA |
| DirectAccessActionWithParameter |             .NET 6.0 |  1.4071 ns |  1.4223 ns |  1.48 |         - |          NA |
|                TypeMapperAction |             .NET 7.0 |  5.1941 ns |  5.1992 ns |  5.47 |         - |          NA |
|              DirectAccessAction |             .NET 7.0 |  0.5047 ns |  0.5285 ns |  0.54 |         - |          NA |
|        TypeMapperActionWithCast |             .NET 7.0 |  5.1518 ns |  5.1662 ns |  5.42 |         - |          NA |
|      DirectAccessActionWithCast |             .NET 7.0 |  0.2112 ns |  0.3016 ns |  0.19 |         - |          NA |
|   TypeMapperActionWithParameter |             .NET 7.0 |  5.6934 ns |  5.6642 ns |  6.11 |         - |          NA |
| DirectAccessActionWithParameter |             .NET 7.0 |  0.4242 ns |  0.4598 ns |  0.44 |         - |          NA |
|                TypeMapperAction |        .NET Core 3.1 |  1.2104 ns |  1.2106 ns |  1.27 |         - |          NA |
|              DirectAccessAction |        .NET Core 3.1 |  0.9093 ns |  0.9200 ns |  0.96 |         - |          NA |
|        TypeMapperActionWithCast |        .NET Core 3.1 |  4.6670 ns |  4.6764 ns |  4.92 |         - |          NA |
|      DirectAccessActionWithCast |        .NET Core 3.1 |  0.4078 ns |  0.3813 ns |  0.41 |         - |          NA |
|   TypeMapperActionWithParameter |        .NET Core 3.1 |  6.0918 ns |  6.1312 ns |  6.43 |         - |          NA |
| DirectAccessActionWithParameter |        .NET Core 3.1 |  0.9012 ns |  0.9132 ns |  0.96 |         - |          NA |
|                TypeMapperAction | .NET Framework 4.7.2 | 22.8045 ns | 22.8045 ns | 24.00 |         - |          NA |
|              DirectAccessAction | .NET Framework 4.7.2 |  0.9504 ns |  0.9470 ns |  1.00 |         - |          NA |
|        TypeMapperActionWithCast | .NET Framework 4.7.2 | 15.3578 ns | 15.3804 ns | 16.15 |         - |          NA |
|      DirectAccessActionWithCast | .NET Framework 4.7.2 |  0.9430 ns |  0.9057 ns |  1.00 |         - |          NA |
|   TypeMapperActionWithParameter | .NET Framework 4.7.2 | 23.1393 ns | 23.2849 ns | 24.34 |         - |          NA |
| DirectAccessActionWithParameter | .NET Framework 4.7.2 |  0.9912 ns |  0.9702 ns |  1.04 |         - |          NA |
