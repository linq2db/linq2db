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
|   TypeMapper |    .NET 4.6.2 | 2,075.298 ns | 160.3433 ns | 95.4177 ns | 1,281.45 |  108.40 | 0.0534 |     - |     - |     225 B |
| DirectAccess |    .NET 4.6.2 |     1.621 ns |   0.1593 ns |  0.1053 ns |     1.00 |    0.00 |      - |     - |     - |         - |
|   TypeMapper | .NET Core 2.1 |   476.280 ns |  32.1161 ns | 21.2428 ns |   294.97 |   23.58 | 0.0114 |     - |     - |      48 B |
| DirectAccess | .NET Core 2.1 |     1.552 ns |   0.0661 ns |  0.0393 ns |     0.96 |    0.08 |      - |     - |     - |         - |
|   TypeMapper | .NET Core 3.1 |   231.066 ns |  16.6771 ns | 11.0308 ns |   143.00 |   10.10 | 0.0114 |     - |     - |      48 B |
| DirectAccess | .NET Core 3.1 |     1.262 ns |   0.0965 ns |  0.0638 ns |     0.78 |    0.06 |      - |     - |     - |         - |
