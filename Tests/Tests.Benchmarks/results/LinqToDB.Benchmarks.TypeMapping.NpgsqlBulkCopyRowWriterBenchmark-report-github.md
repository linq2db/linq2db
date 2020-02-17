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
|       Method |       Runtime |     Mean |    Error |   StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------- |-------------- |---------:|---------:|---------:|------:|--------:|-------:|------:|------:|----------:|
|   TypeMapper |    .NET 4.6.2 | 274.9 ns | 21.34 ns | 14.11 ns |  1.30 |    0.08 | 0.0057 |     - |     - |      24 B |
| DirectAccess |    .NET 4.6.2 | 211.4 ns | 10.22 ns |  6.76 ns |  1.00 |    0.00 | 0.0057 |     - |     - |      24 B |
|   TypeMapper | .NET Core 2.1 | 154.1 ns |  4.93 ns |  3.26 ns |  0.73 |    0.02 | 0.0055 |     - |     - |      24 B |
| DirectAccess | .NET Core 2.1 | 159.3 ns |  7.57 ns |  5.01 ns |  0.75 |    0.04 | 0.0055 |     - |     - |      24 B |
|   TypeMapper | .NET Core 3.1 | 138.6 ns | 11.95 ns |  7.90 ns |  0.66 |    0.05 | 0.0057 |     - |     - |      24 B |
| DirectAccess | .NET Core 3.1 | 164.4 ns |  8.46 ns |  5.60 ns |  0.78 |    0.04 | 0.0057 |     - |     - |      24 B |
