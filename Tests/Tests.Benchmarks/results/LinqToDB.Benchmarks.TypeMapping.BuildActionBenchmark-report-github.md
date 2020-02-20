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
|       Method |       Runtime |     Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------- |-------------- |---------:|----------:|----------:|------:|--------:|------:|------:|------:|----------:|
|  BuildAction |    .NET 4.6.2 | 8.432 ns | 0.1801 ns | 0.1072 ns |  7.43 |    0.26 |     - |     - |     - |         - |
| DirectAccess |    .NET 4.6.2 | 1.134 ns | 0.0573 ns | 0.0379 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|  BuildAction | .NET Core 2.1 | 1.607 ns | 0.0677 ns | 0.0354 ns |  1.42 |    0.07 |     - |     - |     - |         - |
| DirectAccess | .NET Core 2.1 | 1.161 ns | 0.1093 ns | 0.0723 ns |  1.02 |    0.04 |     - |     - |     - |         - |
|  BuildAction | .NET Core 3.1 | 1.627 ns | 0.0418 ns | 0.0108 ns |  1.46 |    0.05 |     - |     - |     - |         - |
| DirectAccess | .NET Core 3.1 | 1.091 ns | 0.0573 ns | 0.0300 ns |  0.96 |    0.03 |     - |     - |     - |         - |
