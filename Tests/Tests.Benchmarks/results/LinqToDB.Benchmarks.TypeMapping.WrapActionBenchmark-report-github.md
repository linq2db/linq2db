``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417995 Hz, Resolution=292.5692 ns, Timer=TSC
  [Host]     : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-FSMYUH : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-TSQXSD : .NET Core 2.1.17 (CoreCLR 4.6.28619.01, CoreFX 4.6.28619.01), X64 RyuJIT
  Job-OUTKHJ : .NET Core 3.1.3 (CoreCLR 4.700.20.11803, CoreFX 4.700.20.12001), X64 RyuJIT

Jit=RyuJit  Platform=X64  MaxIterationCount=5  
MinIterationCount=3  WarmupCount=2  

```
|                          Method |       Runtime |       Mean | Ratio | Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------------------- |-------------- |-----------:|------:|------:|------:|------:|----------:|
|                TypeMapperAction |    .NET 4.6.2 | 14.0354 ns | 12.93 |     - |     - |     - |         - |
|              DirectAccessAction |    .NET 4.6.2 |  1.0957 ns |  1.00 |     - |     - |     - |         - |
|        TypeMapperActionWithCast |    .NET 4.6.2 |  7.0367 ns |  6.42 |     - |     - |     - |         - |
|      DirectAccessActionWithCast |    .NET 4.6.2 |  1.0950 ns |  1.00 |     - |     - |     - |         - |
|   TypeMapperActionWithParameter |    .NET 4.6.2 | 14.0709 ns | 12.83 |     - |     - |     - |         - |
| DirectAccessActionWithParameter |    .NET 4.6.2 |  1.0861 ns |  0.99 |     - |     - |     - |         - |
|                TypeMapperAction | .NET Core 2.1 |  5.9954 ns |  5.48 |     - |     - |     - |         - |
|              DirectAccessAction | .NET Core 2.1 |  1.1135 ns |  1.02 |     - |     - |     - |         - |
|        TypeMapperActionWithCast | .NET Core 2.1 |  5.7076 ns |  5.21 |     - |     - |     - |         - |
|      DirectAccessActionWithCast | .NET Core 2.1 |  1.1147 ns |  1.02 |     - |     - |     - |         - |
|   TypeMapperActionWithParameter | .NET Core 2.1 |  5.7322 ns |  5.23 |     - |     - |     - |         - |
| DirectAccessActionWithParameter | .NET Core 2.1 |  1.0931 ns |  1.00 |     - |     - |     - |         - |
|                TypeMapperAction | .NET Core 3.1 |  5.2013 ns |  4.75 |     - |     - |     - |         - |
|              DirectAccessAction | .NET Core 3.1 |  1.3488 ns |  1.23 |     - |     - |     - |         - |
|        TypeMapperActionWithCast | .NET Core 3.1 |  4.0832 ns |  3.73 |     - |     - |     - |         - |
|      DirectAccessActionWithCast | .NET Core 3.1 |  0.5467 ns |  0.50 |     - |     - |     - |         - |
|   TypeMapperActionWithParameter | .NET Core 3.1 |  6.2443 ns |  5.70 |     - |     - |     - |         - |
| DirectAccessActionWithParameter | .NET Core 3.1 |  1.3530 ns |  1.23 |     - |     - |     - |         - |
