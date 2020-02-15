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
|       Method |       Runtime |          Mean |         Error |        StdDev |    Ratio | RatioSD |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|------------- |-------------- |--------------:|--------------:|--------------:|---------:|--------:|-------:|-------:|------:|----------:|
|   TypeMapper |    .NET 4.6.2 | 14,860.012 ns | 1,160.5905 ns |   690.6488 ns | 8,247.16 |  404.19 | 0.5341 | 0.1373 |     - |    3331 B |
| DirectAccess |    .NET 4.6.2 |      1.803 ns |     0.0963 ns |     0.0573 ns |     1.00 |    0.00 |      - |      - |     - |         - |
|   TypeMapper | .NET Core 2.1 | 16,329.366 ns | 1,831.1299 ns | 1,211.1791 ns | 9,123.56 |  922.84 | 0.3052 | 0.0916 |     - |    1976 B |
| DirectAccess | .NET Core 2.1 |      1.330 ns |     0.0864 ns |     0.0572 ns |     0.73 |    0.05 |      - |      - |     - |         - |
|   TypeMapper | .NET Core 3.1 | 10,288.046 ns | 1,018.5582 ns |   673.7132 ns | 5,662.66 |  378.82 | 0.3052 | 0.0763 |     - |    1936 B |
| DirectAccess | .NET Core 3.1 |      1.621 ns |     0.0640 ns |     0.0335 ns |     0.90 |    0.04 |      - |      - |     - |         - |
