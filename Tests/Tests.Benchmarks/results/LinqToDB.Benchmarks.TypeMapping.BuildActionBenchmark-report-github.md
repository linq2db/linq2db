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
|  BuildAction |    .NET 4.6.2 | 10.638 ns | 1.2407 ns | 0.7383 ns |  8.70 |    0.79 |     - |     - |     - |         - |
| DirectAccess |    .NET 4.6.2 |  1.228 ns | 0.0790 ns | 0.0522 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|  BuildAction | .NET Core 2.1 |  1.669 ns | 0.1720 ns | 0.1023 ns |  1.37 |    0.12 |     - |     - |     - |         - |
| DirectAccess | .NET Core 2.1 |  1.198 ns | 0.0585 ns | 0.0306 ns |  0.97 |    0.05 |     - |     - |     - |         - |
|  BuildAction | .NET Core 3.1 |  1.768 ns | 0.0743 ns | 0.0491 ns |  1.44 |    0.07 |     - |     - |     - |         - |
| DirectAccess | .NET Core 3.1 |  1.080 ns | 0.0798 ns | 0.0475 ns |  0.88 |    0.05 |     - |     - |     - |         - |
