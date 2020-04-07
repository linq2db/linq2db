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
|                  Method |       Runtime |      Mean | Ratio | Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------ |-------------- |----------:|------:|------:|------:|------:|----------:|
|        TypeMapperAsEnum |    .NET 4.6.2 | 22.821 ns | 17.18 |     - |     - |     - |         - |
|      DirectAccessAsEnum |    .NET 4.6.2 |  1.328 ns |  1.00 |     - |     - |     - |         - |
|   TypeMapperAsKnownEnum |    .NET 4.6.2 |  8.930 ns |  6.72 |     - |     - |     - |         - |
| DirectAccessAsKnownEnum |    .NET 4.6.2 |  1.322 ns |  1.00 |     - |     - |     - |         - |
|      TypeMapperAsString |    .NET 4.6.2 | 11.091 ns |  8.35 |     - |     - |     - |         - |
|    DirectAccessAsString |    .NET 4.6.2 |  2.983 ns |  2.25 |     - |     - |     - |         - |
|        TypeMapperAsEnum | .NET Core 2.1 | 13.877 ns | 10.66 |     - |     - |     - |         - |
|      DirectAccessAsEnum | .NET Core 2.1 |  1.088 ns |  0.82 |     - |     - |     - |         - |
|   TypeMapperAsKnownEnum | .NET Core 2.1 |  2.405 ns |  1.81 |     - |     - |     - |         - |
| DirectAccessAsKnownEnum | .NET Core 2.1 |  1.105 ns |  0.83 |     - |     - |     - |         - |
|      TypeMapperAsString | .NET Core 2.1 |  5.951 ns |  4.48 |     - |     - |     - |         - |
|    DirectAccessAsString | .NET Core 2.1 |  4.571 ns |  3.45 |     - |     - |     - |         - |
|        TypeMapperAsEnum | .NET Core 3.1 | 13.263 ns |  9.99 |     - |     - |     - |         - |
|      DirectAccessAsEnum | .NET Core 3.1 |  1.373 ns |  1.03 |     - |     - |     - |         - |
|   TypeMapperAsKnownEnum | .NET Core 3.1 |  2.986 ns |  2.25 |     - |     - |     - |         - |
| DirectAccessAsKnownEnum | .NET Core 3.1 |  1.357 ns |  1.02 |     - |     - |     - |         - |
|      TypeMapperAsString | .NET Core 3.1 |  4.944 ns |  3.73 |     - |     - |     - |         - |
|    DirectAccessAsString | .NET Core 3.1 |  2.996 ns |  2.26 |     - |     - |     - |         - |
