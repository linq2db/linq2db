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
|       Method |       Runtime |       Mean |     Error |    StdDev |     Median | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------- |-------------- |-----------:|----------:|----------:|-----------:|------:|--------:|------:|------:|------:|----------:|
|    BuildFunc |    .NET 4.6.2 | 16.0641 ns | 0.5817 ns | 0.3462 ns | 16.0774 ns |     ? |       ? |     - |     - |     - |         - |
| DirectAccess |    .NET 4.6.2 |  0.0084 ns | 0.0400 ns | 0.0265 ns |  0.0000 ns |     ? |       ? |     - |     - |     - |         - |
|    BuildFunc | .NET Core 2.1 |  5.3894 ns | 0.8090 ns | 0.5351 ns |  5.3527 ns |     ? |       ? |     - |     - |     - |         - |
| DirectAccess | .NET Core 2.1 |  1.9833 ns | 0.5842 ns | 0.3864 ns |  1.8868 ns |     ? |       ? |     - |     - |     - |         - |
|    BuildFunc | .NET Core 3.1 |  4.6313 ns | 0.7320 ns | 0.4842 ns |  4.6223 ns |     ? |       ? |     - |     - |     - |         - |
| DirectAccess | .NET Core 3.1 |  0.3018 ns | 0.1929 ns | 0.1276 ns |  0.2882 ns |     ? |       ? |     - |     - |     - |         - |
