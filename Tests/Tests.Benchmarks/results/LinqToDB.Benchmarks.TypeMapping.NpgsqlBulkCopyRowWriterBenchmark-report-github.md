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
|   TypeMapper |    .NET 4.6.2 | 153.84 ns | 7.456 ns | 4.932 ns |  1.16 |    0.04 | 0.0057 |     - |     - |      24 B |
| DirectAccess |    .NET 4.6.2 | 132.86 ns | 2.984 ns | 1.974 ns |  1.00 |    0.00 | 0.0057 |     - |     - |      24 B |
|   TypeMapper | .NET Core 2.1 |  99.14 ns | 8.640 ns | 5.715 ns |  0.75 |    0.05 | 0.0056 |     - |     - |      24 B |
| DirectAccess | .NET Core 2.1 | 108.74 ns | 3.623 ns | 2.396 ns |  0.82 |    0.02 | 0.0056 |     - |     - |      24 B |
|   TypeMapper | .NET Core 3.1 |  92.70 ns | 2.772 ns | 1.650 ns |  0.70 |    0.02 | 0.0057 |     - |     - |      24 B |
| DirectAccess | .NET Core 3.1 | 110.88 ns | 5.831 ns | 3.857 ns |  0.83 |    0.04 | 0.0057 |     - |     - |      24 B |
