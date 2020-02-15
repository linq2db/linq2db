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
|  BuildAction |    .NET 4.6.2 | 12.855 ns | 0.9734 ns | 0.6438 ns | 10.21 |    0.71 |     - |     - |     - |         - |
| DirectAccess |    .NET 4.6.2 |  1.262 ns | 0.0606 ns | 0.0317 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|  BuildAction | .NET Core 2.1 |  2.371 ns | 0.1252 ns | 0.0828 ns |  1.89 |    0.05 |     - |     - |     - |         - |
| DirectAccess | .NET Core 2.1 |  1.276 ns | 0.0747 ns | 0.0494 ns |  1.00 |    0.03 |     - |     - |     - |         - |
|  BuildAction | .NET Core 3.1 |  1.919 ns | 0.1529 ns | 0.1011 ns |  1.51 |    0.10 |     - |     - |     - |         - |
| DirectAccess | .NET Core 3.1 |  1.071 ns | 0.0805 ns | 0.0533 ns |  0.85 |    0.05 |     - |     - |     - |         - |
