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
|                  Method |       Runtime |         Mean |      Error |     StdDev |    Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------ |-------------- |-------------:|-----------:|-----------:|---------:|--------:|-------:|------:|------:|----------:|
|        TypeMapperAsEnum |    .NET 4.6.2 | 1,288.477 ns | 44.3418 ns | 29.3294 ns | 1,052.57 |   57.55 | 0.0534 |     - |     - |     225 B |
|      DirectAccessAsEnum |    .NET 4.6.2 |     1.227 ns |  0.0934 ns |  0.0618 ns |     1.00 |    0.00 |      - |     - |     - |         - |
|   TypeMapperAsKnownEnum |    .NET 4.6.2 |     9.368 ns |  0.5094 ns |  0.3370 ns |     7.66 |    0.50 |      - |     - |     - |         - |
| DirectAccessAsKnownEnum |    .NET 4.6.2 |     1.097 ns |  0.0469 ns |  0.0122 ns |     0.89 |    0.03 |      - |     - |     - |         - |
|      TypeMapperAsString |    .NET 4.6.2 |    10.928 ns |  0.4073 ns |  0.2694 ns |     8.93 |    0.56 |      - |     - |     - |         - |
|    DirectAccessAsString |    .NET 4.6.2 |     3.281 ns |  0.0718 ns |  0.0187 ns |     2.65 |    0.08 |      - |     - |     - |         - |
|        TypeMapperAsEnum | .NET Core 2.1 |   367.462 ns | 47.0708 ns | 31.1344 ns |   299.74 |   23.32 | 0.0114 |     - |     - |      48 B |
|      DirectAccessAsEnum | .NET Core 2.1 |     1.030 ns |  0.0371 ns |  0.0096 ns |     0.83 |    0.03 |      - |     - |     - |         - |
|   TypeMapperAsKnownEnum | .NET Core 2.1 |     3.099 ns |  0.1058 ns |  0.0700 ns |     2.53 |    0.17 |      - |     - |     - |         - |
| DirectAccessAsKnownEnum | .NET Core 2.1 |     1.092 ns |  0.0534 ns |  0.0237 ns |     0.88 |    0.04 |      - |     - |     - |         - |
|      TypeMapperAsString | .NET Core 2.1 |     6.427 ns |  0.2756 ns |  0.1823 ns |     5.25 |    0.32 |      - |     - |     - |         - |
|    DirectAccessAsString | .NET Core 2.1 |     4.711 ns |  0.2523 ns |  0.1669 ns |     3.85 |    0.28 |      - |     - |     - |         - |
|        TypeMapperAsEnum | .NET Core 3.1 |   197.198 ns |  3.8673 ns |  2.0227 ns |   157.63 |    4.84 | 0.0114 |     - |     - |      48 B |
|      DirectAccessAsEnum | .NET Core 3.1 |     1.374 ns |  0.0685 ns |  0.0178 ns |     1.11 |    0.02 |      - |     - |     - |         - |
|   TypeMapperAsKnownEnum | .NET Core 3.1 |     2.496 ns |  0.0735 ns |  0.0262 ns |     2.02 |    0.05 |      - |     - |     - |         - |
| DirectAccessAsKnownEnum | .NET Core 3.1 |     1.522 ns |  0.1435 ns |  0.0949 ns |     1.24 |    0.09 |      - |     - |     - |         - |
|      TypeMapperAsString | .NET Core 3.1 |     4.942 ns |  0.1399 ns |  0.0833 ns |     3.99 |    0.16 |      - |     - |     - |         - |
|    DirectAccessAsString | .NET Core 3.1 |     2.744 ns |  0.0823 ns |  0.0214 ns |     2.22 |    0.07 |      - |     - |     - |         - |
