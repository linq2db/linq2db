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
|                  Method |       Runtime |         Mean |       Error |      StdDev |    Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------ |-------------- |-------------:|------------:|------------:|---------:|--------:|-------:|------:|------:|----------:|
|        TypeMapperAsEnum |    .NET 4.6.2 | 1,657.314 ns | 184.6735 ns | 122.1501 ns | 1,435.28 |  115.51 | 0.0534 |     - |     - |     225 B |
|      DirectAccessAsEnum |    .NET 4.6.2 |     1.181 ns |   0.0571 ns |   0.0148 ns |     1.00 |    0.00 |      - |     - |     - |         - |
|   TypeMapperAsKnownEnum |    .NET 4.6.2 |    11.884 ns |   1.5885 ns |   1.0507 ns |     9.54 |    0.86 |      - |     - |     - |         - |
| DirectAccessAsKnownEnum |    .NET 4.6.2 |     1.256 ns |   0.1592 ns |   0.1053 ns |     1.12 |    0.07 |      - |     - |     - |         - |
|      TypeMapperAsString |    .NET 4.6.2 |    13.044 ns |   1.2615 ns |   0.8344 ns |    11.35 |    0.64 |      - |     - |     - |         - |
|    DirectAccessAsString |    .NET 4.6.2 |     3.597 ns |   0.1717 ns |   0.1135 ns |     3.02 |    0.12 |      - |     - |     - |         - |
|        TypeMapperAsEnum | .NET Core 2.1 |   376.343 ns |  25.7352 ns |  13.4600 ns |   319.48 |   15.10 | 0.0114 |     - |     - |      48 B |
|      DirectAccessAsEnum | .NET Core 2.1 |     1.546 ns |   0.2417 ns |   0.1599 ns |     1.39 |    0.07 |      - |     - |     - |         - |
|   TypeMapperAsKnownEnum | .NET Core 2.1 |     2.737 ns |   0.1349 ns |   0.0892 ns |     2.34 |    0.04 |      - |     - |     - |         - |
| DirectAccessAsKnownEnum | .NET Core 2.1 |     1.061 ns |   0.0745 ns |   0.0443 ns |     0.89 |    0.05 |      - |     - |     - |         - |
|      TypeMapperAsString | .NET Core 2.1 |     6.191 ns |   0.1908 ns |   0.1262 ns |     5.24 |    0.13 |      - |     - |     - |         - |
|    DirectAccessAsString | .NET Core 2.1 |     5.140 ns |   0.4707 ns |   0.3113 ns |     4.49 |    0.32 |      - |     - |     - |         - |
|        TypeMapperAsEnum | .NET Core 3.1 |   196.711 ns |   8.9484 ns |   5.9188 ns |   167.02 |    4.64 | 0.0114 |     - |     - |      48 B |
|      DirectAccessAsEnum | .NET Core 3.1 |     1.213 ns |   0.0510 ns |   0.0226 ns |     1.03 |    0.02 |      - |     - |     - |         - |
|   TypeMapperAsKnownEnum | .NET Core 3.1 |     2.884 ns |   0.0420 ns |   0.0109 ns |     2.44 |    0.03 |      - |     - |     - |         - |
| DirectAccessAsKnownEnum | .NET Core 3.1 |     1.105 ns |   0.0578 ns |   0.0344 ns |     0.94 |    0.03 |      - |     - |     - |         - |
|      TypeMapperAsString | .NET Core 3.1 |     4.618 ns |   0.1615 ns |   0.1069 ns |     3.95 |    0.10 |      - |     - |     - |         - |
|    DirectAccessAsString | .NET Core 3.1 |     3.095 ns |   0.0840 ns |   0.0500 ns |     2.62 |    0.03 |      - |     - |     - |         - |
