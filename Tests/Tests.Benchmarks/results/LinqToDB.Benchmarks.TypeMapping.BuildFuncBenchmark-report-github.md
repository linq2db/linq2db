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
|    BuildFunc |    .NET 4.6.2 | 13.298 ns | 0.6094 ns | 0.3626 ns |  5.75 |    0.11 |     - |     - |     - |         - |
| DirectAccess |    .NET 4.6.2 |  2.323 ns | 0.1214 ns | 0.0539 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|    BuildFunc | .NET Core 2.1 |  7.914 ns | 1.3925 ns | 0.9211 ns |  3.24 |    0.23 |     - |     - |     - |         - |
| DirectAccess | .NET Core 2.1 |  5.751 ns | 0.5038 ns | 0.3333 ns |  2.44 |    0.15 |     - |     - |     - |         - |
|    BuildFunc | .NET Core 3.1 |  6.948 ns | 0.4695 ns | 0.3105 ns |  3.00 |    0.14 |     - |     - |     - |         - |
| DirectAccess | .NET Core 3.1 |  3.384 ns | 0.5470 ns | 0.3618 ns |  1.48 |    0.15 |     - |     - |     - |         - |
