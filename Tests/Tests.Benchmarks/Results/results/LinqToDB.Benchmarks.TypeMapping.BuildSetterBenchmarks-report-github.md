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
|       Method |       Runtime |          Mean |      Error |     StdDev |        Median |     Ratio |   RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------- |-------------- |--------------:|-----------:|-----------:|--------------:|----------:|----------:|-------:|------:|------:|----------:|
|   TypeMapper |    .NET 4.6.2 | 1,343.3123 ns | 48.3119 ns | 31.9553 ns | 1,345.6744 ns | 44,675.75 | 87,754.65 | 0.0534 |     - |     - |     225 B |
| DirectAccess |    .NET 4.6.2 |     0.1019 ns |  0.1009 ns |  0.0667 ns |     0.0962 ns |      1.00 |      0.00 |      - |     - |     - |         - |
|   TypeMapper | .NET Core 2.1 |   378.0173 ns | 31.6486 ns | 20.9336 ns |   369.5413 ns | 12,308.05 | 23,946.42 | 0.0114 |     - |     - |      48 B |
| DirectAccess | .NET Core 2.1 |     0.0208 ns |  0.0424 ns |  0.0280 ns |     0.0079 ns |      0.85 |      1.45 |      - |     - |     - |         - |
|   TypeMapper | .NET Core 3.1 |   193.0044 ns |  3.5589 ns |  0.9242 ns |   193.3275 ns | 11,166.47 | 17,144.81 | 0.0114 |     - |     - |      48 B |
| DirectAccess | .NET Core 3.1 |     0.0056 ns |  0.0371 ns |  0.0096 ns |     0.0010 ns |      0.08 |      0.07 |      - |     - |     - |         - |
