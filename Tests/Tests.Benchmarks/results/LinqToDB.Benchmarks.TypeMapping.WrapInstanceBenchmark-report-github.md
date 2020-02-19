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
|       Method |       Runtime |       Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------- |-------------- |-----------:|----------:|----------:|------:|--------:|-------:|------:|------:|----------:|
|   TypeMapper |    .NET 4.6.2 | 56.9878 ns | 1.2233 ns | 0.3177 ns |     ? |       ? | 0.0114 |     - |     - |      48 B |
| DirectAccess |    .NET 4.6.2 |  0.0000 ns | 0.0000 ns | 0.0000 ns |     ? |       ? |      - |     - |     - |         - |
|   TypeMapper | .NET Core 2.1 | 49.5689 ns | 1.0320 ns | 0.3680 ns |     ? |       ? | 0.0114 |     - |     - |      48 B |
| DirectAccess | .NET Core 2.1 |  0.0000 ns | 0.0000 ns | 0.0000 ns |     ? |       ? |      - |     - |     - |         - |
|   TypeMapper | .NET Core 3.1 | 46.5452 ns | 0.7986 ns | 0.2074 ns |     ? |       ? | 0.0114 |     - |     - |      48 B |
| DirectAccess | .NET Core 3.1 |  0.0136 ns | 0.0440 ns | 0.0114 ns |     ? |       ? |      - |     - |     - |         - |
