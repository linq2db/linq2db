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
|       Method |       Runtime |          Mean |      Error |     StdDev |        Median | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------- |-------------- |--------------:|-----------:|-----------:|--------------:|------:|--------:|-------:|------:|------:|----------:|
|   TypeMapper |    .NET 4.6.2 | 1,182.3690 ns | 65.5098 ns | 38.9838 ns | 1,178.3632 ns |     ? |       ? | 0.0420 |     - |     - |     177 B |
| DirectAccess |    .NET 4.6.2 |     0.0426 ns |  0.0618 ns |  0.0409 ns |     0.0409 ns |     ? |       ? |      - |     - |     - |         - |
|   TypeMapper | .NET Core 2.1 |   548.3177 ns | 27.5837 ns | 16.4146 ns |   547.4336 ns |     ? |       ? | 0.0296 |     - |     - |     128 B |
| DirectAccess | .NET Core 2.1 |     0.0155 ns |  0.0310 ns |  0.0205 ns |     0.0106 ns |     ? |       ? |      - |     - |     - |         - |
|   TypeMapper | .NET Core 3.1 |   210.2274 ns | 20.1326 ns | 13.3165 ns |   212.5500 ns |     ? |       ? | 0.0114 |     - |     - |      48 B |
| DirectAccess | .NET Core 3.1 |     0.0407 ns |  0.0567 ns |  0.0375 ns |     0.0316 ns |     ? |       ? |      - |     - |     - |         - |
