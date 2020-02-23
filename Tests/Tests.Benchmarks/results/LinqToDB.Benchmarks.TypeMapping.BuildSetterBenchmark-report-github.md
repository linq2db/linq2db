``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417995 Hz, Resolution=292.5692 ns, Timer=TSC
  [Host]     : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-FSMYUH : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-TSQXSD : .NET Core 2.1.15 (CoreCLR 4.6.28325.01, CoreFX 4.6.28327.02), X64 RyuJIT
  Job-OUTKHJ : .NET Core 3.1.1 (CoreCLR 4.700.19.60701, CoreFX 4.700.19.60801), X64 RyuJIT

Jit=RyuJit  Platform=X64  MaxIterationCount=5  
MinIterationCount=3  WarmupCount=2  

```
|                  Method |       Runtime |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------ |-------------- |----------:|----------:|----------:|------:|--------:|------:|------:|------:|----------:|
|        TypeMapperAsEnum |    .NET 4.6.2 | 22.810 ns | 0.2805 ns | 0.0728 ns | 16.98 |    0.25 |     - |     - |     - |         - |
|      DirectAccessAsEnum |    .NET 4.6.2 |  1.343 ns | 0.0610 ns | 0.0158 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|   TypeMapperAsKnownEnum |    .NET 4.6.2 |  8.889 ns | 0.0961 ns | 0.0149 ns |  6.63 |    0.08 |     - |     - |     - |         - |
| DirectAccessAsKnownEnum |    .NET 4.6.2 |  1.364 ns | 0.0497 ns | 0.0077 ns |  1.02 |    0.01 |     - |     - |     - |         - |
|      TypeMapperAsString |    .NET 4.6.2 | 11.304 ns | 0.8543 ns | 0.2219 ns |  8.42 |    0.22 |     - |     - |     - |         - |
|    DirectAccessAsString |    .NET 4.6.2 |  2.928 ns | 0.0740 ns | 0.0114 ns |  2.18 |    0.03 |     - |     - |     - |         - |
|        TypeMapperAsEnum | .NET Core 2.1 | 13.094 ns | 0.5695 ns | 0.1479 ns |  9.75 |    0.10 |     - |     - |     - |         - |
|      DirectAccessAsEnum | .NET Core 2.1 |  1.046 ns | 0.0383 ns | 0.0100 ns |  0.78 |    0.01 |     - |     - |     - |         - |
|   TypeMapperAsKnownEnum | .NET Core 2.1 |  2.439 ns | 0.0750 ns | 0.0195 ns |  1.82 |    0.01 |     - |     - |     - |         - |
| DirectAccessAsKnownEnum | .NET Core 2.1 |  1.056 ns | 0.0470 ns | 0.0026 ns |  0.78 |    0.01 |     - |     - |     - |         - |
|      TypeMapperAsString | .NET Core 2.1 |  6.293 ns | 0.9950 ns | 0.2584 ns |  4.69 |    0.18 |     - |     - |     - |         - |
|    DirectAccessAsString | .NET Core 2.1 |  4.580 ns | 0.0686 ns | 0.0106 ns |  3.41 |    0.04 |     - |     - |     - |         - |
|        TypeMapperAsEnum | .NET Core 3.1 | 13.399 ns | 0.5595 ns | 0.1453 ns |  9.98 |    0.12 |     - |     - |     - |         - |
|      DirectAccessAsEnum | .NET Core 3.1 |  1.372 ns | 0.1053 ns | 0.0274 ns |  1.02 |    0.02 |     - |     - |     - |         - |
|   TypeMapperAsKnownEnum | .NET Core 3.1 |  2.975 ns | 0.0391 ns | 0.0061 ns |  2.22 |    0.03 |     - |     - |     - |         - |
| DirectAccessAsKnownEnum | .NET Core 3.1 |  1.341 ns | 0.0274 ns | 0.0042 ns |  1.00 |    0.01 |     - |     - |     - |         - |
|      TypeMapperAsString | .NET Core 3.1 |  4.818 ns | 0.0498 ns | 0.0027 ns |  3.57 |    0.03 |     - |     - |     - |         - |
|    DirectAccessAsString | .NET Core 3.1 |  2.657 ns | 0.0878 ns | 0.0048 ns |  1.97 |    0.02 |     - |     - |     - |         - |
