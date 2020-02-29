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
|       Method |       Runtime |       Mean |     Error |    StdDev |     Ratio |  RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------- |-------------- |-----------:|----------:|----------:|----------:|---------:|-------:|------:|------:|----------:|
|   TypeMapper |    .NET 4.6.2 | 49.1951 ns | 0.9743 ns | 0.1508 ns | 2,309.883 | 1,552.23 | 0.0076 |     - |     - |      32 B |
| DirectAccess |    .NET 4.6.2 |  0.0271 ns | 0.0767 ns | 0.0119 ns |     1.000 |     0.00 |      - |     - |     - |         - |
|   TypeMapper | .NET Core 2.1 | 42.3628 ns | 1.7904 ns | 0.4650 ns | 1,990.038 | 1,331.69 | 0.0076 |     - |     - |      32 B |
| DirectAccess | .NET Core 2.1 |  0.0030 ns | 0.0225 ns | 0.0035 ns |     0.089 |     0.11 |      - |     - |     - |         - |
|   TypeMapper | .NET Core 3.1 | 42.4807 ns | 3.1147 ns | 0.8089 ns | 1,991.349 | 1,315.70 | 0.0076 |     - |     - |      32 B |
| DirectAccess | .NET Core 3.1 |  0.0000 ns | 0.0000 ns | 0.0000 ns |     0.000 |     0.00 |      - |     - |     - |         - |
