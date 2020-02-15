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
|       Method |       Runtime |           Mean |          Error |         StdDev |     Ratio |  RatioSD |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|------------- |-------------- |---------------:|---------------:|---------------:|----------:|---------:|-------:|-------:|------:|----------:|
|   TypeMapper |    .NET 4.6.2 | 491,188.428 ns | 37,165.9928 ns | 24,583.0031 ns | 94,381.08 | 3,701.99 | 4.3945 | 0.9766 |     - |   18992 B |
| DirectAccess |    .NET 4.6.2 |       5.399 ns |      0.1909 ns |      0.0496 ns |      1.00 |     0.00 | 0.0057 |      - |     - |      24 B |
|   TypeMapper | .NET Core 2.1 | 257,274.881 ns | 10,509.4072 ns |  6,951.3222 ns | 46,988.60 |   637.24 | 1.4648 | 0.4883 |     - |    6737 B |
| DirectAccess | .NET Core 2.1 |       6.887 ns |      0.5228 ns |      0.3458 ns |      1.29 |     0.05 | 0.0057 |      - |     - |      24 B |
|   TypeMapper | .NET Core 3.1 | 230,633.457 ns | 18,716.2720 ns | 12,379.6551 ns | 42,040.41 | 2,059.74 | 1.4648 | 0.4883 |     - |    6600 B |
| DirectAccess | .NET Core 3.1 |       4.677 ns |      0.1763 ns |      0.1049 ns |      0.86 |     0.03 | 0.0057 |      - |     - |      24 B |
