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
|       Method |       Runtime |       Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------- |-------------- |-----------:|----------:|----------:|------:|--------:|------:|------:|------:|----------:|
|  BuildAction |    .NET 4.6.2 | 14.2497 ns | 1.7692 ns | 0.4595 ns | 11.00 |    0.39 |     - |     - |     - |         - |
| DirectAccess |    .NET 4.6.2 |  1.2955 ns | 0.0898 ns | 0.0233 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|  BuildAction | .NET Core 2.1 |  2.5701 ns | 0.3813 ns | 0.0990 ns |  1.98 |    0.06 |     - |     - |     - |         - |
| DirectAccess | .NET Core 2.1 |  1.4344 ns | 0.1951 ns | 0.0507 ns |  1.11 |    0.04 |     - |     - |     - |         - |
|  BuildAction | .NET Core 3.1 |  1.8008 ns | 0.3693 ns | 0.0959 ns |  1.39 |    0.06 |     - |     - |     - |         - |
| DirectAccess | .NET Core 3.1 |  0.9057 ns | 0.1063 ns | 0.0276 ns |  0.70 |    0.03 |     - |     - |     - |         - |
