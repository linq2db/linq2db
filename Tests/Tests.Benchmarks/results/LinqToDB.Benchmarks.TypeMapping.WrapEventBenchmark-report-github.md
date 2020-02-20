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
|        TypeMapperEmpty |    .NET 4.6.2 |         9.162 ns |         0.2358 ns |       0.1560 ns |         6.80 |       0.20 |      - |      - |     - |         - |
|      DirectAccessEmpty |    .NET 4.6.2 |         1.352 ns |         0.0583 ns |       0.0151 ns |         1.00 |       0.00 |      - |      - |     - |         - |
|   TypeMapperSubscribed |    .NET 4.6.2 | 1,495,043.147 ns |   330,448.8677 ns | 218,571.4659 ns |   974,538.47 |  58,945.49 | 7.8125 | 1.9531 |     - |   55657 B |
| DirectAccessSubscribed |    .NET 4.6.2 |        73.936 ns |         3.4218 ns |       2.2633 ns |        54.81 |       2.14 | 0.0305 |      - |     - |     128 B |
|        TypeMapperEmpty | .NET Core 2.1 |         8.752 ns |         0.4228 ns |       0.2796 ns |         6.60 |       0.19 |      - |      - |     - |         - |
|      DirectAccessEmpty | .NET Core 2.1 |         1.400 ns |         0.0552 ns |       0.0245 ns |         1.03 |       0.01 |      - |      - |     - |         - |
|   TypeMapperSubscribed | .NET Core 2.1 | 2,279,081.697 ns | 1,154,626.1332 ns | 763,713.6972 ns | 1,214,858.35 | 274,757.66 | 2.9297 | 0.9766 |     - |   16774 B |
| DirectAccessSubscribed | .NET Core 2.1 |        74.180 ns |         3.4375 ns |       2.2737 ns |        54.08 |       1.51 | 0.0304 |      - |     - |     128 B |
|        TypeMapperEmpty | .NET Core 3.1 |         8.580 ns |         0.1044 ns |       0.0271 ns |         6.35 |       0.06 |      - |      - |     - |         - |
|      DirectAccessEmpty | .NET Core 3.1 |         1.114 ns |         0.0503 ns |       0.0179 ns |         0.83 |       0.02 |      - |      - |     - |         - |
|   TypeMapperSubscribed | .NET Core 3.1 | 2,213,308.430 ns | 1,297,057.4089 ns | 857,923.1673 ns | 1,107,836.39 | 272,948.36 | 3.9063 | 0.9766 |     - |   16382 B |
| DirectAccessSubscribed | .NET Core 3.1 |        74.735 ns |         4.1228 ns |       2.7270 ns |        55.58 |       2.37 | 0.0305 |      - |     - |     128 B |
