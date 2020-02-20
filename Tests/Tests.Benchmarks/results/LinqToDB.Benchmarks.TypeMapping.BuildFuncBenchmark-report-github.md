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
|    BuildFunc |    .NET 4.6.2 | 11.969 ns | 0.7799 ns | 0.5158 ns |  5.04 |    0.35 |     - |     - |     - |         - |
| DirectAccess |    .NET 4.6.2 |  2.379 ns | 0.1995 ns | 0.1319 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|    BuildFunc | .NET Core 2.1 |  7.633 ns | 1.0665 ns | 0.7054 ns |  3.21 |    0.26 |     - |     - |     - |         - |
| DirectAccess | .NET Core 2.1 |  3.802 ns | 0.6659 ns | 0.4405 ns |  1.60 |    0.22 |     - |     - |     - |         - |
|    BuildFunc | .NET Core 3.1 |  4.989 ns | 0.2764 ns | 0.1829 ns |  2.10 |    0.15 |     - |     - |     - |         - |
| DirectAccess | .NET Core 3.1 |  2.410 ns | 0.3146 ns | 0.2081 ns |  1.02 |    0.11 |     - |     - |     - |         - |
