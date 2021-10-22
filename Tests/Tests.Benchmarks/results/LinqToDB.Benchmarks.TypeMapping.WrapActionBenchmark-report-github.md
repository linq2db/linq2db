``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417994 Hz, Resolution=292.5693 ns, Timer=TSC
.NET SDK=5.0.402
  [Host]     : .NET 5.0.11 (5.0.1121.47308), X64 RyuJIT
  Job-ODZCDL : .NET 5.0.11 (5.0.1121.47308), X64 RyuJIT
  Job-PCJJBI : .NET Core 3.1.20 (CoreCLR 4.700.21.47003, CoreFX 4.700.21.47101), X64 RyuJIT
  Job-HHEMGO : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT

Jit=RyuJit  Platform=X64  

```
|                          Method |              Runtime |       Mean | Ratio | Allocated |
|-------------------------------- |--------------------- |-----------:|------:|----------:|
|                TypeMapperAction |             .NET 5.0 |  5.4102 ns |  5.00 |         - |
|              DirectAccessAction |             .NET 5.0 |  1.0164 ns |  0.94 |         - |
|        TypeMapperActionWithCast |             .NET 5.0 |  4.5929 ns |  4.24 |         - |
|      DirectAccessActionWithCast |             .NET 5.0 |  0.5302 ns |  0.49 |         - |
|   TypeMapperActionWithParameter |             .NET 5.0 |  6.1630 ns |  5.69 |         - |
| DirectAccessActionWithParameter |             .NET 5.0 |  1.0678 ns |  0.99 |         - |
|                TypeMapperAction |        .NET Core 3.1 |  5.1412 ns |  4.75 |         - |
|              DirectAccessAction |        .NET Core 3.1 |  1.3300 ns |  1.23 |         - |
|        TypeMapperActionWithCast |        .NET Core 3.1 |  4.0144 ns |  3.71 |         - |
|      DirectAccessActionWithCast |        .NET Core 3.1 |  0.5695 ns |  0.53 |         - |
|   TypeMapperActionWithParameter |        .NET Core 3.1 |  6.4097 ns |  5.92 |         - |
| DirectAccessActionWithParameter |        .NET Core 3.1 |  1.3062 ns |  1.21 |         - |
|                TypeMapperAction | .NET Framework 4.7.2 | 19.3373 ns | 17.85 |         - |
|              DirectAccessAction | .NET Framework 4.7.2 |  1.0834 ns |  1.00 |         - |
|        TypeMapperActionWithCast | .NET Framework 4.7.2 | 13.3095 ns | 12.34 |         - |
|      DirectAccessActionWithCast | .NET Framework 4.7.2 |  1.0796 ns |  1.00 |         - |
|   TypeMapperActionWithParameter | .NET Framework 4.7.2 | 19.3485 ns | 17.88 |         - |
| DirectAccessActionWithParameter | .NET Framework 4.7.2 |  1.0816 ns |  1.00 |         - |
