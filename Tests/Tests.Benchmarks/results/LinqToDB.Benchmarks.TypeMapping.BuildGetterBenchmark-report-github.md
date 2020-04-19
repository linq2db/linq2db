``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417995 Hz, Resolution=292.5692 ns, Timer=TSC
  [Host]     : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-FSMYUH : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-TSQXSD : .NET Core 2.1.17 (CoreCLR 4.6.28619.01, CoreFX 4.6.28619.01), X64 RyuJIT
  Job-OUTKHJ : .NET Core 3.1.3 (CoreCLR 4.700.20.11803, CoreFX 4.700.20.12001), X64 RyuJIT

Jit=RyuJit  Platform=X64  MaxIterationCount=5  
MinIterationCount=3  WarmupCount=2  

```
|                  Method |       Runtime |      Mean | Ratio |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------ |-------------- |----------:|------:|-------:|------:|------:|----------:|
|        TypeMapperAsEnum |    .NET 4.6.2 | 37.831 ns | 27.88 | 0.0057 |     - |     - |      24 B |
|      DirectAccessAsEnum |    .NET 4.6.2 |  1.362 ns |  1.00 |      - |     - |     - |         - |
|      TypeMapperAsObject |    .NET 4.6.2 | 45.095 ns | 33.01 | 0.0114 |     - |     - |      48 B |
|    DirectAccessAsObject |    .NET 4.6.2 |  4.915 ns |  3.57 | 0.0057 |     - |     - |      24 B |
|     TypeMapperAsDecimal |    .NET 4.6.2 | 10.475 ns |  7.71 |      - |     - |     - |         - |
|   DirectAccessAsDecimal |    .NET 4.6.2 |  1.440 ns |  1.06 |      - |     - |     - |         - |
|     TypeMapperAsBoolean |    .NET 4.6.2 |  8.607 ns |  6.32 |      - |     - |     - |         - |
|   DirectAccessAsBoolean |    .NET 4.6.2 |  1.083 ns |  0.80 |      - |     - |     - |         - |
|      TypeMapperAsString |    .NET 4.6.2 |  8.965 ns |  6.48 |      - |     - |     - |         - |
|    DirectAccessAsString |    .NET 4.6.2 |  1.439 ns |  1.07 |      - |     - |     - |         - |
|         TypeMapperAsInt |    .NET 4.6.2 |  9.498 ns |  6.98 |      - |     - |     - |         - |
|       DirectAccessAsInt |    .NET 4.6.2 |  1.099 ns |  0.81 |      - |     - |     - |         - |
|        TypeMapperAsBool |    .NET 4.6.2 |  9.036 ns |  6.65 |      - |     - |     - |         - |
|      DirectAccessAsBool |    .NET 4.6.2 |  1.122 ns |  0.81 |      - |     - |     - |         - |
|   TypeMapperAsKnownEnum |    .NET 4.6.2 |  8.750 ns |  6.42 |      - |     - |     - |         - |
| DirectAccessAsKnownEnum |    .NET 4.6.2 |  1.274 ns |  0.94 |      - |     - |     - |         - |
|        TypeMapperAsEnum | .NET Core 2.1 | 30.885 ns | 22.77 | 0.0057 |     - |     - |      24 B |
|      DirectAccessAsEnum | .NET Core 2.1 |  1.360 ns |  1.00 |      - |     - |     - |         - |
|      TypeMapperAsObject | .NET Core 2.1 | 37.336 ns | 27.41 | 0.0114 |     - |     - |      48 B |
|    DirectAccessAsObject | .NET Core 2.1 |  4.792 ns |  3.51 | 0.0057 |     - |     - |      24 B |
|     TypeMapperAsDecimal | .NET Core 2.1 |  4.126 ns |  3.03 |      - |     - |     - |         - |
|   DirectAccessAsDecimal | .NET Core 2.1 |  1.491 ns |  1.10 |      - |     - |     - |         - |
|     TypeMapperAsBoolean | .NET Core 2.1 |  2.965 ns |  2.18 |      - |     - |     - |         - |
|   DirectAccessAsBoolean | .NET Core 2.1 |  1.366 ns |  1.00 |      - |     - |     - |         - |
|      TypeMapperAsString | .NET Core 2.1 |  2.987 ns |  2.18 |      - |     - |     - |         - |
|    DirectAccessAsString | .NET Core 2.1 |  1.307 ns |  0.96 |      - |     - |     - |         - |
|         TypeMapperAsInt | .NET Core 2.1 |  3.003 ns |  2.20 |      - |     - |     - |         - |
|       DirectAccessAsInt | .NET Core 2.1 |  1.372 ns |  1.01 |      - |     - |     - |         - |
|        TypeMapperAsBool | .NET Core 2.1 |  2.984 ns |  2.19 |      - |     - |     - |         - |
|      DirectAccessAsBool | .NET Core 2.1 |  1.325 ns |  0.97 |      - |     - |     - |         - |
|   TypeMapperAsKnownEnum | .NET Core 2.1 |  3.062 ns |  2.25 |      - |     - |     - |         - |
| DirectAccessAsKnownEnum | .NET Core 2.1 |  1.424 ns |  1.04 |      - |     - |     - |         - |
|        TypeMapperAsEnum | .NET Core 3.1 | 27.829 ns | 20.43 | 0.0057 |     - |     - |      24 B |
|      DirectAccessAsEnum | .NET Core 3.1 |  1.358 ns |  1.00 |      - |     - |     - |         - |
|      TypeMapperAsObject | .NET Core 3.1 | 33.109 ns | 24.39 | 0.0114 |     - |     - |      48 B |
|    DirectAccessAsObject | .NET Core 3.1 |  4.777 ns |  3.50 | 0.0057 |     - |     - |      24 B |
|     TypeMapperAsDecimal | .NET Core 3.1 |  4.241 ns |  3.08 |      - |     - |     - |         - |
|   DirectAccessAsDecimal | .NET Core 3.1 |  1.504 ns |  1.09 |      - |     - |     - |         - |
|     TypeMapperAsBoolean | .NET Core 3.1 |  2.415 ns |  1.78 |      - |     - |     - |         - |
|   DirectAccessAsBoolean | .NET Core 3.1 |  1.319 ns |  0.97 |      - |     - |     - |         - |
|      TypeMapperAsString | .NET Core 3.1 |  2.600 ns |  1.94 |      - |     - |     - |         - |
|    DirectAccessAsString | .NET Core 3.1 |  1.364 ns |  1.01 |      - |     - |     - |         - |
|         TypeMapperAsInt | .NET Core 3.1 |  2.619 ns |  1.89 |      - |     - |     - |         - |
|       DirectAccessAsInt | .NET Core 3.1 |  1.326 ns |  0.97 |      - |     - |     - |         - |
|        TypeMapperAsBool | .NET Core 3.1 |  2.450 ns |  1.81 |      - |     - |     - |         - |
|      DirectAccessAsBool | .NET Core 3.1 |  1.363 ns |  1.01 |      - |     - |     - |         - |
|   TypeMapperAsKnownEnum | .NET Core 3.1 |  2.481 ns |  1.83 |      - |     - |     - |         - |
| DirectAccessAsKnownEnum | .NET Core 3.1 |  1.360 ns |  1.00 |      - |     - |     - |         - |
