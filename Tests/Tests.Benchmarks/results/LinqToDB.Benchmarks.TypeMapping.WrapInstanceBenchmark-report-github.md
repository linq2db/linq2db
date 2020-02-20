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
|       Method |       Runtime |       Mean |      Error |    StdDev |     Median | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------- |-------------- |-----------:|-----------:|----------:|-----------:|------:|--------:|-------:|------:|------:|----------:|
|   TypeMapper |    .NET 4.6.2 | 62.9100 ns |  6.8297 ns | 4.5175 ns | 61.3822 ns |     ? |       ? | 0.0114 |     - |     - |      48 B |
| DirectAccess |    .NET 4.6.2 |  0.0471 ns |  0.0812 ns | 0.0537 ns |  0.0274 ns |     ? |       ? |      - |     - |     - |         - |
|   TypeMapper | .NET Core 2.1 | 48.8945 ns |  2.0191 ns | 1.3355 ns | 48.5499 ns |     ? |       ? | 0.0114 |     - |     - |      48 B |
| DirectAccess | .NET Core 2.1 |  0.0030 ns |  0.0188 ns | 0.0049 ns |  0.0000 ns |     ? |       ? |      - |     - |     - |         - |
|   TypeMapper | .NET Core 3.1 | 53.1135 ns | 10.1482 ns | 6.7124 ns | 51.3063 ns |     ? |       ? | 0.0114 |     - |     - |      48 B |
| DirectAccess | .NET Core 3.1 |  0.0285 ns |  0.0663 ns | 0.0439 ns |  0.0000 ns |     ? |       ? |      - |     - |     - |         - |
