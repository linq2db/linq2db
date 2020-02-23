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
|    BuildFunc |    .NET 4.6.2 | 18.994 ns | 4.7231 ns | 1.2266 ns |  5.40 |    0.60 |     - |     - |     - |         - |
| DirectAccess |    .NET 4.6.2 |  3.536 ns | 0.9640 ns | 0.2504 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|    BuildFunc | .NET Core 2.1 | 10.041 ns | 2.5927 ns | 0.6733 ns |  2.85 |    0.29 |     - |     - |     - |         - |
| DirectAccess | .NET Core 2.1 |  3.896 ns | 1.1827 ns | 0.3071 ns |  1.10 |    0.09 |     - |     - |     - |         - |
|    BuildFunc | .NET Core 3.1 |  5.534 ns | 1.3119 ns | 0.3407 ns |  1.57 |    0.18 |     - |     - |     - |         - |
| DirectAccess | .NET Core 3.1 |  3.192 ns | 2.1670 ns | 0.5628 ns |  0.91 |    0.18 |     - |     - |     - |         - |
