``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-HCNGBR : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-XBFFOD : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-INBZNN : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-THZJXI : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                          Method |              Runtime |       Mean | Allocated |
|-------------------------------- |--------------------- |-----------:|----------:|
|                TypeMapperAction |             .NET 6.0 |  5.6311 ns |         - |
|              DirectAccessAction |             .NET 6.0 |  0.7306 ns |         - |
|        TypeMapperActionWithCast |             .NET 6.0 |  4.1622 ns |         - |
|      DirectAccessActionWithCast |             .NET 6.0 |  0.4473 ns |         - |
|   TypeMapperActionWithParameter |             .NET 6.0 |  5.2034 ns |         - |
| DirectAccessActionWithParameter |             .NET 6.0 |  1.4564 ns |         - |
|                TypeMapperAction |             .NET 7.0 |  5.0933 ns |         - |
|              DirectAccessAction |             .NET 7.0 |  0.1357 ns |         - |
|        TypeMapperActionWithCast |             .NET 7.0 |  5.1390 ns |         - |
|      DirectAccessActionWithCast |             .NET 7.0 |  0.4528 ns |         - |
|   TypeMapperActionWithParameter |             .NET 7.0 |  5.2198 ns |         - |
| DirectAccessActionWithParameter |             .NET 7.0 |  0.4598 ns |         - |
|                TypeMapperAction |        .NET Core 3.1 |  5.4181 ns |         - |
|              DirectAccessAction |        .NET Core 3.1 |  0.6985 ns |         - |
|        TypeMapperActionWithCast |        .NET Core 3.1 |  4.4515 ns |         - |
|      DirectAccessActionWithCast |        .NET Core 3.1 |  0.4557 ns |         - |
|   TypeMapperActionWithParameter |        .NET Core 3.1 |  6.3147 ns |         - |
| DirectAccessActionWithParameter |        .NET Core 3.1 |  1.1964 ns |         - |
|                TypeMapperAction | .NET Framework 4.7.2 | 20.6446 ns |         - |
|              DirectAccessAction | .NET Framework 4.7.2 |  1.2937 ns |         - |
|        TypeMapperActionWithCast | .NET Framework 4.7.2 | 13.1543 ns |         - |
|      DirectAccessActionWithCast | .NET Framework 4.7.2 |  1.3419 ns |         - |
|   TypeMapperActionWithParameter | .NET Framework 4.7.2 | 19.8571 ns |         - |
| DirectAccessActionWithParameter | .NET Framework 4.7.2 |  1.3532 ns |         - |
