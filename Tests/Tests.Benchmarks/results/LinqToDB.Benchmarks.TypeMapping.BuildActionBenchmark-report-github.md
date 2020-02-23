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
|       Method |       Runtime |     Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------- |-------------- |---------:|----------:|----------:|------:|--------:|------:|------:|------:|----------:|
|  BuildAction |    .NET 4.6.2 | 8.737 ns | 1.2825 ns | 0.3331 ns |  6.90 |    0.28 |     - |     - |     - |         - |
| DirectAccess |    .NET 4.6.2 | 1.267 ns | 0.1889 ns | 0.0490 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|  BuildAction | .NET Core 2.1 | 1.999 ns | 1.3476 ns | 0.3500 ns |  1.58 |    0.32 |     - |     - |     - |         - |
| DirectAccess | .NET Core 2.1 | 1.712 ns | 0.5730 ns | 0.1488 ns |  1.35 |    0.13 |     - |     - |     - |         - |
|  BuildAction | .NET Core 3.1 | 1.791 ns | 0.3027 ns | 0.0786 ns |  1.41 |    0.07 |     - |     - |     - |         - |
| DirectAccess | .NET Core 3.1 | 1.129 ns | 0.1713 ns | 0.0445 ns |  0.89 |    0.04 |     - |     - |     - |         - |
