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
|                  Method |       Runtime |         Mean |       Error |     StdDev |    Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------ |-------------- |-------------:|------------:|-----------:|---------:|--------:|-------:|------:|------:|----------:|
|        TypeMapperAsEnum |    .NET 4.6.2 | 2,363.907 ns | 146.9142 ns | 97.1746 ns | 1,528.09 |   90.48 | 0.0534 |     - |     - |     225 B |
|      DirectAccessAsEnum |    .NET 4.6.2 |     1.550 ns |   0.1233 ns |  0.0816 ns |     1.00 |    0.00 |      - |     - |     - |         - |
|   TypeMapperAsKnownEnum |    .NET 4.6.2 |    16.096 ns |   1.6214 ns |  1.0725 ns |    10.39 |    0.60 |      - |     - |     - |         - |
| DirectAccessAsKnownEnum |    .NET 4.6.2 |     1.304 ns |   0.1321 ns |  0.0874 ns |     0.84 |    0.04 |      - |     - |     - |         - |
|      TypeMapperAsString |    .NET 4.6.2 |    12.945 ns |   0.3641 ns |  0.2408 ns |     8.37 |    0.47 |      - |     - |     - |         - |
|    DirectAccessAsString |    .NET 4.6.2 |     3.611 ns |   0.1039 ns |  0.0687 ns |     2.33 |    0.10 |      - |     - |     - |         - |
|        TypeMapperAsEnum | .NET Core 2.1 |   462.193 ns |  59.8644 ns | 39.5966 ns |   299.47 |   36.41 | 0.0114 |     - |     - |      48 B |
|      DirectAccessAsEnum | .NET Core 2.1 |     1.847 ns |   0.3482 ns |  0.2303 ns |     1.19 |    0.14 |      - |     - |     - |         - |
|   TypeMapperAsKnownEnum | .NET Core 2.1 |     4.121 ns |   0.8837 ns |  0.5845 ns |     2.66 |    0.33 |      - |     - |     - |         - |
| DirectAccessAsKnownEnum | .NET Core 2.1 |     2.100 ns |   0.1910 ns |  0.1263 ns |     1.36 |    0.13 |      - |     - |     - |         - |
|      TypeMapperAsString | .NET Core 2.1 |     9.260 ns |   0.6355 ns |  0.4204 ns |     5.98 |    0.32 |      - |     - |     - |         - |
|    DirectAccessAsString | .NET Core 2.1 |     6.600 ns |   0.2846 ns |  0.1883 ns |     4.27 |    0.23 |      - |     - |     - |         - |
|        TypeMapperAsEnum | .NET Core 3.1 |   317.986 ns |  13.1327 ns |  8.6865 ns |   205.80 |   14.93 | 0.0114 |     - |     - |      48 B |
|      DirectAccessAsEnum | .NET Core 3.1 |     1.725 ns |   0.0761 ns |  0.0338 ns |     1.11 |    0.06 |      - |     - |     - |         - |
|   TypeMapperAsKnownEnum | .NET Core 3.1 |     4.325 ns |   0.3520 ns |  0.2328 ns |     2.79 |    0.14 |      - |     - |     - |         - |
| DirectAccessAsKnownEnum | .NET Core 3.1 |     1.623 ns |   0.0639 ns |  0.0334 ns |     1.05 |    0.06 |      - |     - |     - |         - |
|      TypeMapperAsString | .NET Core 3.1 |     6.356 ns |   0.4558 ns |  0.3015 ns |     4.11 |    0.34 |      - |     - |     - |         - |
|    DirectAccessAsString | .NET Core 3.1 |     3.466 ns |   0.1561 ns |  0.0929 ns |     2.24 |    0.16 |      - |     - |     - |         - |
