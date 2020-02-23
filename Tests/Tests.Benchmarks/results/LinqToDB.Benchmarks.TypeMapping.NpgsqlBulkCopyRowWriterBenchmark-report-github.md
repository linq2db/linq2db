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
|       Method |       Runtime |      Mean |    Error |   StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------- |-------------- |----------:|---------:|---------:|------:|--------:|-------:|------:|------:|----------:|
|   TypeMapper |    .NET 4.6.2 | 144.77 ns | 2.019 ns | 0.524 ns |  1.12 |    0.01 | 0.0057 |     - |     - |      24 B |
| DirectAccess |    .NET 4.6.2 | 129.15 ns | 3.030 ns | 0.787 ns |  1.00 |    0.00 | 0.0057 |     - |     - |      24 B |
|   TypeMapper | .NET Core 2.1 |  91.74 ns | 2.158 ns | 0.560 ns |  0.71 |    0.01 | 0.0056 |     - |     - |      24 B |
| DirectAccess | .NET Core 2.1 | 108.18 ns | 9.453 ns | 2.455 ns |  0.84 |    0.02 | 0.0056 |     - |     - |      24 B |
|   TypeMapper | .NET Core 3.1 |  88.59 ns | 0.986 ns | 0.256 ns |  0.69 |    0.00 | 0.0057 |     - |     - |      24 B |
| DirectAccess | .NET Core 3.1 | 103.23 ns | 1.200 ns | 0.066 ns |  0.80 |    0.00 | 0.0057 |     - |     - |      24 B |
