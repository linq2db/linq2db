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
|       Method |       Runtime |      Mean |     Error |   StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------- |-------------- |----------:|----------:|---------:|------:|--------:|-------:|------:|------:|----------:|
|   TypeMapper |    .NET 4.6.2 | 147.79 ns |  3.384 ns | 0.879 ns |  1.13 |    0.01 | 0.0057 |     - |     - |      24 B |
| DirectAccess |    .NET 4.6.2 | 131.16 ns |  6.468 ns | 1.680 ns |  1.00 |    0.00 | 0.0057 |     - |     - |      24 B |
|   TypeMapper | .NET Core 2.1 |  92.83 ns |  1.484 ns | 0.385 ns |  0.71 |    0.01 | 0.0056 |     - |     - |      24 B |
| DirectAccess | .NET Core 2.1 | 108.12 ns |  1.919 ns | 0.297 ns |  0.83 |    0.01 | 0.0056 |     - |     - |      24 B |
|   TypeMapper | .NET Core 3.1 |  96.88 ns | 13.830 ns | 3.592 ns |  0.74 |    0.03 | 0.0057 |     - |     - |      24 B |
| DirectAccess | .NET Core 3.1 | 107.76 ns |  4.037 ns | 1.049 ns |  0.82 |    0.02 | 0.0057 |     - |     - |      24 B |
