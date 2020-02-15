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
|    BuildFunc |    .NET 4.6.2 | 13.592 ns | 1.1156 ns | 0.5835 ns |  5.34 |    0.34 |     - |     - |     - |         - |
| DirectAccess |    .NET 4.6.2 |  2.563 ns | 0.2386 ns | 0.1420 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|    BuildFunc | .NET Core 2.1 |  6.683 ns | 0.4829 ns | 0.3194 ns |  2.60 |    0.20 |     - |     - |     - |         - |
| DirectAccess | .NET Core 2.1 |  4.175 ns | 0.3524 ns | 0.2331 ns |  1.63 |    0.12 |     - |     - |     - |         - |
|    BuildFunc | .NET Core 3.1 |  5.040 ns | 0.6371 ns | 0.4214 ns |  1.96 |    0.16 |     - |     - |     - |         - |
| DirectAccess | .NET Core 3.1 |  2.387 ns | 0.1381 ns | 0.0914 ns |  0.94 |    0.07 |     - |     - |     - |         - |
