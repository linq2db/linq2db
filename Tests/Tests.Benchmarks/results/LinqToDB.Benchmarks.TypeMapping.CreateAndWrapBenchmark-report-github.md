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
|       Method |       Runtime |           Mean |          Error |         StdDev |     Ratio |   RatioSD |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|------------- |-------------- |---------------:|---------------:|---------------:|----------:|----------:|-------:|-------:|------:|----------:|
|   TypeMapper |    .NET 4.6.2 | 461,294.514 ns | 34,516.9369 ns | 22,830.8166 ns | 84,896.76 | 10,384.34 | 4.3945 | 0.9766 |     - |   18992 B |
| DirectAccess |    .NET 4.6.2 |       5.493 ns |      0.8918 ns |      0.5899 ns |      1.00 |      0.00 | 0.0057 |      - |     - |      24 B |
|   TypeMapper | .NET Core 2.1 | 231,831.720 ns |  6,488.1145 ns |  4,291.4861 ns | 42,628.68 |  4,488.25 | 1.4648 | 0.4883 |     - |    6737 B |
| DirectAccess | .NET Core 2.1 |       6.407 ns |      0.6039 ns |      0.3994 ns |      1.18 |      0.15 | 0.0057 |      - |     - |      24 B |
|   TypeMapper | .NET Core 3.1 | 230,223.639 ns | 26,098.3478 ns | 17,262.4411 ns | 42,354.22 |  5,601.44 | 1.4648 | 0.4883 |     - |    6600 B |
| DirectAccess | .NET Core 3.1 |       5.163 ns |      0.3460 ns |      0.0899 ns |      0.99 |      0.06 | 0.0057 |      - |     - |      24 B |
