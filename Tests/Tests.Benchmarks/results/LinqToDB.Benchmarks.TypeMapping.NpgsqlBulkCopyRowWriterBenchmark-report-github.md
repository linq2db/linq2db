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
|       Method |       Runtime |      Mean |    Error |   StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------- |-------------- |----------:|---------:|---------:|------:|--------:|-------:|------:|------:|----------:|
|   TypeMapper |    .NET 4.6.2 | 156.92 ns | 9.490 ns | 6.277 ns |  1.19 |    0.05 | 0.0057 |     - |     - |      24 B |
| DirectAccess |    .NET 4.6.2 | 132.32 ns | 3.264 ns | 2.159 ns |  1.00 |    0.00 | 0.0057 |     - |     - |      24 B |
|   TypeMapper | .NET Core 2.1 |  92.03 ns | 1.648 ns | 0.428 ns |  0.69 |    0.01 | 0.0056 |     - |     - |      24 B |
| DirectAccess | .NET Core 2.1 | 107.32 ns | 2.058 ns | 0.914 ns |  0.81 |    0.02 | 0.0056 |     - |     - |      24 B |
|   TypeMapper | .NET Core 3.1 |  88.92 ns | 1.619 ns | 0.719 ns |  0.67 |    0.01 | 0.0057 |     - |     - |      24 B |
| DirectAccess | .NET Core 3.1 | 107.48 ns | 3.032 ns | 2.005 ns |  0.81 |    0.02 | 0.0057 |     - |     - |      24 B |
