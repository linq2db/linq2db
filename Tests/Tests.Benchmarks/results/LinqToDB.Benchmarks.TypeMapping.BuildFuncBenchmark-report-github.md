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
|       Method |       Runtime |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------- |-------------- |----------:|----------:|----------:|------:|--------:|------:|------:|------:|----------:|
|    BuildFunc |    .NET 4.6.2 | 13.261 ns | 3.5829 ns | 0.9305 ns |  4.66 |    0.46 |     - |     - |     - |         - |
| DirectAccess |    .NET 4.6.2 |  2.861 ns | 0.9579 ns | 0.2488 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|    BuildFunc | .NET Core 2.1 |  5.851 ns | 1.2640 ns | 0.3283 ns |  2.06 |    0.20 |     - |     - |     - |         - |
| DirectAccess | .NET Core 2.1 |  3.286 ns | 0.1047 ns | 0.0272 ns |  1.16 |    0.10 |     - |     - |     - |         - |
|    BuildFunc | .NET Core 3.1 |  4.009 ns | 0.1046 ns | 0.0162 ns |  1.43 |    0.14 |     - |     - |     - |         - |
| DirectAccess | .NET Core 3.1 |  1.885 ns | 0.0541 ns | 0.0084 ns |  0.67 |    0.06 |     - |     - |     - |         - |
