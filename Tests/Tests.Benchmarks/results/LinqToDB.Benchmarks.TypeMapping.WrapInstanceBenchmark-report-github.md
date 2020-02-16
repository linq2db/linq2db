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
|       Method |       Runtime |              Mean |             Error |          StdDev |            Median | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------- |-------------- |------------------:|------------------:|----------------:|------------------:|------:|--------:|-------:|------:|------:|----------:|
|   TypeMapper |    .NET 4.6.2 | 2,542,309.1600 ns | 1,137,943.0211 ns | 752,678.8515 ns | 2,628,880.3800 ns |     ? |       ? |      - |     - |     - |   73728 B |
| DirectAccess |    .NET 4.6.2 |         0.1158 ns |         0.1730 ns |       0.1144 ns |         0.0796 ns |     ? |       ? |      - |     - |     - |         - |
|   TypeMapper | .NET Core 2.1 |   912,096.4237 ns |    35,623.3154 ns |  21,198.8636 ns |   912,142.1376 ns |     ? |       ? | 1.9531 |     - |     - |   17999 B |
| DirectAccess | .NET Core 2.1 |         0.0830 ns |         0.1402 ns |       0.0927 ns |         0.0500 ns |     ? |       ? |      - |     - |     - |         - |
|   TypeMapper | .NET Core 3.1 |   872,226.9584 ns |    58,833.3920 ns |  38,914.6461 ns |   875,193.2785 ns |     ? |       ? | 1.9531 |     - |     - |   17589 B |
| DirectAccess | .NET Core 3.1 |         0.0856 ns |         0.1923 ns |       0.1272 ns |         0.0000 ns |     ? |       ? |      - |     - |     - |         - |
