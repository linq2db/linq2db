``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417995 Hz, Resolution=292.5692 ns, Timer=TSC
  [Host]     : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-ZXOHUL : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-TAKBNN : .NET Core 2.1.15 (CoreCLR 4.6.28325.01, CoreFX 4.6.28327.02), X64 RyuJIT
  Job-WOIQBX : .NET Core 3.1.1 (CoreCLR 4.700.19.60701, CoreFX 4.700.19.60801), X64 RyuJIT

Jit=RyuJit  Platform=X64  MaxIterationCount=10  
MinIterationCount=5  WarmupCount=2  

```
|                          Method |       Runtime |       Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------------------- |-------------- |-----------:|----------:|----------:|------:|--------:|------:|------:|------:|----------:|
|                TypeMapperAction |    .NET 4.6.2 | 14.3648 ns | 0.5389 ns | 0.3565 ns | 10.18 |    0.25 |     - |     - |     - |         - |
|              DirectAccessAction |    .NET 4.6.2 |  1.4054 ns | 0.0578 ns | 0.0257 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|        TypeMapperActionWithCast |    .NET 4.6.2 |  7.3210 ns | 0.4092 ns | 0.2706 ns |  5.22 |    0.23 |     - |     - |     - |         - |
|      DirectAccessActionWithCast |    .NET 4.6.2 |  1.2570 ns | 0.0549 ns | 0.0363 ns |  0.90 |    0.03 |     - |     - |     - |         - |
|   TypeMapperActionWithParameter |    .NET 4.6.2 | 14.5173 ns | 0.3173 ns | 0.0824 ns | 10.33 |    0.23 |     - |     - |     - |         - |
| DirectAccessActionWithParameter |    .NET 4.6.2 |  1.3728 ns | 0.0548 ns | 0.0142 ns |  0.98 |    0.02 |     - |     - |     - |         - |
|                TypeMapperAction | .NET Core 2.1 |  5.9605 ns | 0.2933 ns | 0.1940 ns |  4.28 |    0.12 |     - |     - |     - |         - |
|              DirectAccessAction | .NET Core 2.1 |  1.0258 ns | 0.0554 ns | 0.0198 ns |  0.73 |    0.02 |     - |     - |     - |         - |
|        TypeMapperActionWithCast | .NET Core 2.1 |  6.4880 ns | 0.3324 ns | 0.2199 ns |  4.64 |    0.06 |     - |     - |     - |         - |
|      DirectAccessActionWithCast | .NET Core 2.1 |  1.0974 ns | 0.0558 ns | 0.0145 ns |  0.78 |    0.02 |     - |     - |     - |         - |
|   TypeMapperActionWithParameter | .NET Core 2.1 |  6.0023 ns | 0.2725 ns | 0.1802 ns |  4.28 |    0.16 |     - |     - |     - |         - |
| DirectAccessActionWithParameter | .NET Core 2.1 |  1.0939 ns | 0.0610 ns | 0.0403 ns |  0.78 |    0.03 |     - |     - |     - |         - |
|                TypeMapperAction | .NET Core 3.1 |  6.3174 ns | 0.2504 ns | 0.1657 ns |  4.52 |    0.19 |     - |     - |     - |         - |
|              DirectAccessAction | .NET Core 3.1 |  1.2581 ns | 0.1795 ns | 0.1187 ns |  0.93 |    0.08 |     - |     - |     - |         - |
|        TypeMapperActionWithCast | .NET Core 3.1 |  4.6420 ns | 0.1289 ns | 0.0767 ns |  3.32 |    0.06 |     - |     - |     - |         - |
|      DirectAccessActionWithCast | .NET Core 3.1 |  0.5491 ns | 0.0550 ns | 0.0327 ns |  0.39 |    0.02 |     - |     - |     - |         - |
|   TypeMapperActionWithParameter | .NET Core 3.1 |  6.1424 ns | 0.2572 ns | 0.1701 ns |  4.38 |    0.15 |     - |     - |     - |         - |
| DirectAccessActionWithParameter | .NET Core 3.1 |  1.1847 ns | 0.1815 ns | 0.1201 ns |  0.79 |    0.03 |     - |     - |     - |         - |
