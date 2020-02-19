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
|                TypeMapperAction |    .NET 4.6.2 | 14.0833 ns | 0.5680 ns | 0.3380 ns | 10.33 |    0.35 |     - |     - |     - |         - |
|              DirectAccessAction |    .NET 4.6.2 |  1.3650 ns | 0.0538 ns | 0.0281 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|        TypeMapperActionWithCast |    .NET 4.6.2 |  7.1808 ns | 0.1083 ns | 0.0281 ns |  5.25 |    0.15 |     - |     - |     - |         - |
|      DirectAccessActionWithCast |    .NET 4.6.2 |  1.3725 ns | 0.0569 ns | 0.0148 ns |  1.00 |    0.02 |     - |     - |     - |         - |
|   TypeMapperActionWithParameter |    .NET 4.6.2 | 14.2920 ns | 0.4306 ns | 0.2848 ns | 10.48 |    0.24 |     - |     - |     - |         - |
| DirectAccessActionWithParameter |    .NET 4.6.2 |  1.5169 ns | 0.1921 ns | 0.1271 ns |  1.13 |    0.09 |     - |     - |     - |         - |
|                TypeMapperAction | .NET Core 2.1 |  5.7972 ns | 0.4146 ns | 0.1077 ns |  4.24 |    0.17 |     - |     - |     - |         - |
|              DirectAccessAction | .NET Core 2.1 |  1.0760 ns | 0.0508 ns | 0.0226 ns |  0.79 |    0.03 |     - |     - |     - |         - |
|        TypeMapperActionWithCast | .NET Core 2.1 |  5.7651 ns | 0.0804 ns | 0.0209 ns |  4.22 |    0.11 |     - |     - |     - |         - |
|      DirectAccessActionWithCast | .NET Core 2.1 |  1.0476 ns | 0.0415 ns | 0.0148 ns |  0.77 |    0.02 |     - |     - |     - |         - |
|   TypeMapperActionWithParameter | .NET Core 2.1 |  6.0296 ns | 0.2050 ns | 0.1220 ns |  4.42 |    0.17 |     - |     - |     - |         - |
| DirectAccessActionWithParameter | .NET Core 2.1 |  0.9756 ns | 0.0228 ns | 0.0059 ns |  0.71 |    0.02 |     - |     - |     - |         - |
|                TypeMapperAction | .NET Core 3.1 |  5.8364 ns | 0.1284 ns | 0.0672 ns |  4.28 |    0.11 |     - |     - |     - |         - |
|              DirectAccessAction | .NET Core 3.1 |  1.0841 ns | 0.0357 ns | 0.0093 ns |  0.79 |    0.03 |     - |     - |     - |         - |
|        TypeMapperActionWithCast | .NET Core 3.1 |  4.7919 ns | 0.1176 ns | 0.0615 ns |  3.51 |    0.06 |     - |     - |     - |         - |
|      DirectAccessActionWithCast | .NET Core 3.1 |  0.5964 ns | 0.1192 ns | 0.0788 ns |  0.42 |    0.04 |     - |     - |     - |         - |
|   TypeMapperActionWithParameter | .NET Core 3.1 |  6.1480 ns | 0.3867 ns | 0.1004 ns |  4.50 |    0.15 |     - |     - |     - |         - |
| DirectAccessActionWithParameter | .NET Core 3.1 |  1.1471 ns | 0.0465 ns | 0.0166 ns |  0.84 |    0.03 |     - |     - |     - |         - |
