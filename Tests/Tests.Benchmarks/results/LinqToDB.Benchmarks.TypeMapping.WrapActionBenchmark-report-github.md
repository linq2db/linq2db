``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417995 Hz, Resolution=292.5692 ns, Timer=TSC
  [Host]     : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-FSMYUH : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-TSQXSD : .NET Core 2.1.15 (CoreCLR 4.6.28325.01, CoreFX 4.6.28327.02), X64 RyuJIT
  Job-OUTKHJ : .NET Core 3.1.1 (CoreCLR 4.700.19.60701, CoreFX 4.700.19.60801), X64 RyuJIT

Jit=RyuJit  Platform=X64  MaxIterationCount=5  
MinIterationCount=3  WarmupCount=2  

```
|                          Method |       Runtime |       Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------------------- |-------------- |-----------:|----------:|----------:|------:|--------:|------:|------:|------:|----------:|
|                TypeMapperAction |    .NET 4.6.2 | 13.5992 ns | 0.3704 ns | 0.0962 ns | 12.80 |    0.13 |     - |     - |     - |         - |
|              DirectAccessAction |    .NET 4.6.2 |  1.0626 ns | 0.0358 ns | 0.0020 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|        TypeMapperActionWithCast |    .NET 4.6.2 |  7.2937 ns | 0.5711 ns | 0.1483 ns |  6.89 |    0.11 |     - |     - |     - |         - |
|      DirectAccessActionWithCast |    .NET 4.6.2 |  1.0908 ns | 0.0459 ns | 0.0071 ns |  1.03 |    0.01 |     - |     - |     - |         - |
|   TypeMapperActionWithParameter |    .NET 4.6.2 | 13.6657 ns | 0.2603 ns | 0.0676 ns | 12.88 |    0.06 |     - |     - |     - |         - |
| DirectAccessActionWithParameter |    .NET 4.6.2 |  1.0664 ns | 0.0324 ns | 0.0050 ns |  1.01 |    0.01 |     - |     - |     - |         - |
|                TypeMapperAction | .NET Core 2.1 |  5.6799 ns | 0.0962 ns | 0.0250 ns |  5.35 |    0.02 |     - |     - |     - |         - |
|              DirectAccessAction | .NET Core 2.1 |  1.0721 ns | 0.0536 ns | 0.0029 ns |  1.01 |    0.00 |     - |     - |     - |         - |
|        TypeMapperActionWithCast | .NET Core 2.1 |  5.6430 ns | 0.0718 ns | 0.0111 ns |  5.31 |    0.01 |     - |     - |     - |         - |
|      DirectAccessActionWithCast | .NET Core 2.1 |  1.0881 ns | 0.1210 ns | 0.0314 ns |  1.04 |    0.01 |     - |     - |     - |         - |
|   TypeMapperActionWithParameter | .NET Core 2.1 |  5.8532 ns | 0.5997 ns | 0.1557 ns |  5.53 |    0.21 |     - |     - |     - |         - |
| DirectAccessActionWithParameter | .NET Core 2.1 |  1.0737 ns | 0.0488 ns | 0.0027 ns |  1.01 |    0.00 |     - |     - |     - |         - |
|                TypeMapperAction | .NET Core 3.1 |  5.1541 ns | 0.0830 ns | 0.0128 ns |  4.85 |    0.02 |     - |     - |     - |         - |
|              DirectAccessAction | .NET Core 3.1 |  1.0723 ns | 0.0193 ns | 0.0030 ns |  1.01 |    0.00 |     - |     - |     - |         - |
|        TypeMapperActionWithCast | .NET Core 3.1 |  3.8556 ns | 0.0817 ns | 0.0212 ns |  3.63 |    0.02 |     - |     - |     - |         - |
|      DirectAccessActionWithCast | .NET Core 3.1 |  0.5219 ns | 0.1310 ns | 0.0340 ns |  0.49 |    0.04 |     - |     - |     - |         - |
|   TypeMapperActionWithParameter | .NET Core 3.1 |  5.8099 ns | 0.0487 ns | 0.0027 ns |  5.47 |    0.01 |     - |     - |     - |         - |
| DirectAccessActionWithParameter | .NET Core 3.1 |  1.1341 ns | 0.0200 ns | 0.0031 ns |  1.07 |    0.01 |     - |     - |     - |         - |
