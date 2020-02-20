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
|                 Method |       Runtime |             Mean |             Error |          StdDev |        Ratio |    RatioSD |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|----------------------- |-------------- |-----------------:|------------------:|----------------:|-------------:|-----------:|-------:|-------:|------:|----------:|
|        TypeMapperEmpty |    .NET 4.6.2 |         9.052 ns |         0.2398 ns |       0.1586 ns |         6.50 |       0.23 |      - |      - |     - |         - |
|      DirectAccessEmpty |    .NET 4.6.2 |         1.395 ns |         0.0550 ns |       0.0327 ns |         1.00 |       0.00 |      - |      - |     - |         - |
|   TypeMapperSubscribed |    .NET 4.6.2 | 1,580,225.978 ns |   292,176.1823 ns | 193,256.4542 ns | 1,108,619.09 | 112,959.20 | 7.8125 | 1.9531 |     - |   55657 B |
| DirectAccessSubscribed |    .NET 4.6.2 |        70.958 ns |         3.6400 ns |       2.1661 ns |        50.91 |       1.97 | 0.0305 |      - |     - |     128 B |
|        TypeMapperEmpty | .NET Core 2.1 |         9.032 ns |         0.4669 ns |       0.3088 ns |         6.52 |       0.29 |      - |      - |     - |         - |
|      DirectAccessEmpty | .NET Core 2.1 |         1.363 ns |         0.0460 ns |       0.0119 ns |         0.99 |       0.03 |      - |      - |     - |         - |
|   TypeMapperSubscribed | .NET Core 2.1 | 2,295,605.227 ns | 1,176,183.5645 ns | 777,972.6032 ns | 1,548,197.35 | 487,156.74 | 2.9297 | 0.9766 |     - |   16774 B |
| DirectAccessSubscribed | .NET Core 2.1 |        81.372 ns |         7.3608 ns |       4.8687 ns |        59.16 |       2.53 | 0.0304 |      - |     - |     128 B |
|        TypeMapperEmpty | .NET Core 3.1 |         9.305 ns |         0.1934 ns |       0.0690 ns |         6.71 |       0.18 |      - |      - |     - |         - |
|      DirectAccessEmpty | .NET Core 3.1 |         1.131 ns |         0.0635 ns |       0.0378 ns |         0.81 |       0.03 |      - |      - |     - |         - |
|   TypeMapperSubscribed | .NET Core 3.1 | 2,314,030.085 ns | 1,382,477.8924 ns | 914,423.5282 ns | 1,537,548.46 | 562,176.01 | 2.9297 | 0.9766 |     - |   16384 B |
| DirectAccessSubscribed | .NET Core 3.1 |        72.738 ns |         3.2662 ns |       2.1604 ns |        52.41 |       2.38 | 0.0305 |      - |     - |     128 B |
