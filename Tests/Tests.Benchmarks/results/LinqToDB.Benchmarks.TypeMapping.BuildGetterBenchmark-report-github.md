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
|       Method |       Runtime |         Mean |      Error |     StdDev |    Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------- |-------------- |-------------:|-----------:|-----------:|---------:|--------:|-------:|------:|------:|----------:|
|   TypeMapper |    .NET 4.6.2 | 1,320.881 ns | 93.8970 ns | 62.1070 ns | 1,113.70 |  146.17 | 0.0420 |     - |     - |     177 B |
| DirectAccess |    .NET 4.6.2 |     1.204 ns |  0.2461 ns |  0.1628 ns |     1.00 |    0.00 |      - |     - |     - |         - |
|   TypeMapper | .NET Core 2.1 |   544.303 ns | 40.9935 ns | 27.1147 ns |   459.41 |   63.92 | 0.0296 |     - |     - |     128 B |
| DirectAccess | .NET Core 2.1 |     1.421 ns |  0.0977 ns |  0.0646 ns |     1.20 |    0.17 |      - |     - |     - |         - |
|   TypeMapper | .NET Core 3.1 |   196.722 ns |  3.6474 ns |  1.9077 ns |   171.70 |   20.73 | 0.0114 |     - |     - |      48 B |
| DirectAccess | .NET Core 3.1 |     1.437 ns |  0.2412 ns |  0.0626 ns |     1.34 |    0.06 |      - |     - |     - |         - |
