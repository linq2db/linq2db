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
|                TypeMapperAction |    .NET 4.6.2 | 13.4855 ns | 0.3079 ns | 0.0800 ns | 12.58 |    0.11 |     - |     - |     - |         - |
|              DirectAccessAction |    .NET 4.6.2 |  1.0720 ns | 0.0329 ns | 0.0085 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|        TypeMapperActionWithCast |    .NET 4.6.2 |  7.0541 ns | 0.3864 ns | 0.1003 ns |  6.58 |    0.08 |     - |     - |     - |         - |
|      DirectAccessActionWithCast |    .NET 4.6.2 |  1.1146 ns | 0.0417 ns | 0.0108 ns |  1.04 |    0.01 |     - |     - |     - |         - |
|   TypeMapperActionWithParameter |    .NET 4.6.2 | 13.5041 ns | 0.2877 ns | 0.0747 ns | 12.60 |    0.12 |     - |     - |     - |         - |
| DirectAccessActionWithParameter |    .NET 4.6.2 |  1.0624 ns | 0.0402 ns | 0.0062 ns |  0.99 |    0.01 |     - |     - |     - |         - |
|                TypeMapperAction | .NET Core 2.1 |  5.8097 ns | 0.6057 ns | 0.1573 ns |  5.42 |    0.14 |     - |     - |     - |         - |
|              DirectAccessAction | .NET Core 2.1 |  1.0693 ns | 0.0467 ns | 0.0072 ns |  1.00 |    0.01 |     - |     - |     - |         - |
|        TypeMapperActionWithCast | .NET Core 2.1 |  5.7446 ns | 0.5030 ns | 0.1306 ns |  5.36 |    0.11 |     - |     - |     - |         - |
|      DirectAccessActionWithCast | .NET Core 2.1 |  1.0763 ns | 0.0534 ns | 0.0083 ns |  1.01 |    0.01 |     - |     - |     - |         - |
|   TypeMapperActionWithParameter | .NET Core 2.1 |  5.6961 ns | 0.4692 ns | 0.1218 ns |  5.31 |    0.16 |     - |     - |     - |         - |
| DirectAccessActionWithParameter | .NET Core 2.1 |  1.0667 ns | 0.0362 ns | 0.0094 ns |  1.00 |    0.01 |     - |     - |     - |         - |
|                TypeMapperAction | .NET Core 3.1 |  5.1341 ns | 0.1338 ns | 0.0073 ns |  4.80 |    0.05 |     - |     - |     - |         - |
|              DirectAccessAction | .NET Core 3.1 |  1.0789 ns | 0.0325 ns | 0.0050 ns |  1.01 |    0.01 |     - |     - |     - |         - |
|        TypeMapperActionWithCast | .NET Core 3.1 |  3.8421 ns | 0.1157 ns | 0.0301 ns |  3.58 |    0.05 |     - |     - |     - |         - |
|      DirectAccessActionWithCast | .NET Core 3.1 |  0.5288 ns | 0.0328 ns | 0.0085 ns |  0.49 |    0.01 |     - |     - |     - |         - |
|   TypeMapperActionWithParameter | .NET Core 3.1 |  6.0232 ns | 0.6562 ns | 0.1704 ns |  5.62 |    0.20 |     - |     - |     - |         - |
| DirectAccessActionWithParameter | .NET Core 3.1 |  1.1216 ns | 0.0426 ns | 0.0111 ns |  1.05 |    0.01 |     - |     - |     - |         - |
