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
|       Method |       Runtime |         Mean |       Error |     StdDev |    Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------- |-------------- |-------------:|------------:|-----------:|---------:|--------:|-------:|------:|------:|----------:|
|   TypeMapper |    .NET 4.6.2 | 1,361.582 ns | 104.6867 ns | 69.2438 ns | 1,152.55 |   89.37 | 0.0420 |     - |     - |     177 B |
| DirectAccess |    .NET 4.6.2 |     1.184 ns |   0.0600 ns |  0.0397 ns |     1.00 |    0.00 |      - |     - |     - |         - |
|   TypeMapper | .NET Core 2.1 |   637.564 ns |  81.4295 ns | 53.8605 ns |   538.27 |   36.62 | 0.0296 |     - |     - |     128 B |
| DirectAccess | .NET Core 2.1 |     1.834 ns |   0.3230 ns |  0.2136 ns |     1.55 |    0.21 |      - |     - |     - |         - |
|   TypeMapper | .NET Core 3.1 |   330.723 ns |  19.3825 ns | 12.8203 ns |   279.56 |   11.95 | 0.0114 |     - |     - |      48 B |
| DirectAccess | .NET Core 3.1 |     1.878 ns |   0.1637 ns |  0.1083 ns |     1.59 |    0.08 |      - |     - |     - |         - |
