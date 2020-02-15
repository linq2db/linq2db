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
|       Method |       Runtime |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------- |-------------- |----------:|----------:|----------:|------:|--------:|------:|------:|------:|----------:|
|  BuildAction |    .NET 4.6.2 | 11.138 ns | 1.2400 ns | 0.8202 ns |  8.68 |    0.39 |     - |     - |     - |         - |
| DirectAccess |    .NET 4.6.2 |  1.286 ns | 0.1662 ns | 0.1099 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|  BuildAction | .NET Core 2.1 |  2.309 ns | 0.0970 ns | 0.0641 ns |  1.81 |    0.19 |     - |     - |     - |         - |
| DirectAccess | .NET Core 2.1 |  1.302 ns | 0.0600 ns | 0.0314 ns |  1.00 |    0.10 |     - |     - |     - |         - |
|  BuildAction | .NET Core 3.1 |  1.865 ns | 0.0931 ns | 0.0554 ns |  1.45 |    0.15 |     - |     - |     - |         - |
| DirectAccess | .NET Core 3.1 |  1.187 ns | 0.0814 ns | 0.0539 ns |  0.93 |    0.09 |     - |     - |     - |         - |
