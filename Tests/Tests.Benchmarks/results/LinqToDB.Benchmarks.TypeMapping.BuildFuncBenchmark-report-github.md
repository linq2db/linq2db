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
|    BuildFunc |    .NET 4.6.2 | 10.985 ns | 0.5437 ns | 0.2844 ns |  5.85 |    0.22 |     - |     - |     - |         - |
| DirectAccess |    .NET 4.6.2 |  1.880 ns | 0.1253 ns | 0.0746 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|    BuildFunc | .NET Core 2.1 |  5.909 ns | 0.3062 ns | 0.2025 ns |  3.15 |    0.16 |     - |     - |     - |         - |
| DirectAccess | .NET Core 2.1 |  3.570 ns | 0.1578 ns | 0.1044 ns |  1.91 |    0.10 |     - |     - |     - |         - |
|    BuildFunc | .NET Core 3.1 |  4.499 ns | 0.3030 ns | 0.2004 ns |  2.39 |    0.12 |     - |     - |     - |         - |
| DirectAccess | .NET Core 3.1 |  1.906 ns | 0.1077 ns | 0.0280 ns |  1.01 |    0.04 |     - |     - |     - |         - |
