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
|                 Method |       Runtime |             Mean |           Error |          StdDev |        Ratio |    RatioSD |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|----------------------- |-------------- |-----------------:|----------------:|----------------:|-------------:|-----------:|-------:|-------:|------:|----------:|
|        TypeMapperEmpty |    .NET 4.6.2 |        14.065 ns |       0.8153 ns |       0.5392 ns |         7.84 |       0.64 |      - |      - |     - |         - |
|      DirectAccessEmpty |    .NET 4.6.2 |         1.802 ns |       0.1843 ns |       0.1219 ns |         1.00 |       0.00 |      - |      - |     - |         - |
|   TypeMapperSubscribed |    .NET 4.6.2 | 2,512,745.845 ns | 559,917.9290 ns | 370,351.0421 ns | 1,396,059.81 | 198,749.01 | 7.8125 | 1.9531 |     - |   56526 B |
| DirectAccessSubscribed |    .NET 4.6.2 |       104.629 ns |       5.9406 ns |       3.9293 ns |        58.33 |       5.00 | 0.0305 |      - |     - |     128 B |
|        TypeMapperEmpty | .NET Core 2.1 |        13.304 ns |       0.8777 ns |       0.5805 ns |         7.41 |       0.58 |      - |      - |     - |         - |
|      DirectAccessEmpty | .NET Core 2.1 |         1.853 ns |       0.3460 ns |       0.2288 ns |         1.03 |       0.12 |      - |      - |     - |         - |
|   TypeMapperSubscribed | .NET Core 2.1 | 2,027,246.659 ns | 709,707.9146 ns | 469,427.8432 ns | 1,124,172.68 | 250,341.20 | 1.9531 |      - |     - |   17613 B |
| DirectAccessSubscribed | .NET Core 2.1 |        91.254 ns |       6.2258 ns |       4.1180 ns |        50.82 |       3.91 | 0.0304 |      - |     - |     128 B |
|        TypeMapperEmpty | .NET Core 3.1 |        10.971 ns |       0.6107 ns |       0.4040 ns |         6.12 |       0.60 |      - |      - |     - |         - |
|      DirectAccessEmpty | .NET Core 3.1 |         1.354 ns |       0.1434 ns |       0.0948 ns |         0.76 |       0.09 |      - |      - |     - |         - |
|   TypeMapperSubscribed | .NET Core 3.1 | 2,153,559.746 ns | 746,800.4985 ns | 493,962.2908 ns | 1,194,764.08 | 262,701.30 | 1.9531 |      - |     - |   17203 B |
| DirectAccessSubscribed | .NET Core 3.1 |       122.529 ns |       5.3028 ns |       3.5075 ns |        68.24 |       4.66 | 0.0305 |      - |     - |     128 B |
