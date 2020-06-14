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
|              Method |       Runtime |      Mean | Ratio | Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------- |-------------- |----------:|------:|------:|------:|------:|----------:|
|    TypeMapperString |    .NET 4.6.2 | 15.524 ns |  5.19 |     - |     - |     - |         - |
|  DirectAccessString |    .NET 4.6.2 |  3.003 ns |  1.00 |     - |     - |     - |         - |
|       TypeMapperInt |    .NET 4.6.2 | 13.648 ns |  4.54 |     - |     - |     - |         - |
|     DirectAccessInt |    .NET 4.6.2 |  1.125 ns |  0.37 |     - |     - |     - |         - |
|   TypeMapperBoolean |    .NET 4.6.2 | 14.213 ns |  4.79 |     - |     - |     - |         - |
| DirectAccessBoolean |    .NET 4.6.2 |  1.163 ns |  0.40 |     - |     - |     - |         - |
|   TypeMapperWrapper |    .NET 4.6.2 | 16.550 ns |  5.49 |     - |     - |     - |         - |
| DirectAccessWrapper |    .NET 4.6.2 |  2.971 ns |  0.99 |     - |     - |     - |         - |
|    TypeMapperString | .NET Core 2.1 |  9.247 ns |  3.08 |     - |     - |     - |         - |
|  DirectAccessString | .NET Core 2.1 |  4.578 ns |  1.52 |     - |     - |     - |         - |
|       TypeMapperInt | .NET Core 2.1 |  7.621 ns |  2.50 |     - |     - |     - |         - |
|     DirectAccessInt | .NET Core 2.1 |  2.409 ns |  0.80 |     - |     - |     - |         - |
|   TypeMapperBoolean | .NET Core 2.1 |  7.652 ns |  2.54 |     - |     - |     - |         - |
| DirectAccessBoolean | .NET Core 2.1 |  2.687 ns |  0.89 |     - |     - |     - |         - |
|   TypeMapperWrapper | .NET Core 2.1 |  8.469 ns |  2.82 |     - |     - |     - |         - |
| DirectAccessWrapper | .NET Core 2.1 |  2.981 ns |  0.99 |     - |     - |     - |         - |
|    TypeMapperString | .NET Core 3.1 |  7.789 ns |  2.59 |     - |     - |     - |         - |
|  DirectAccessString | .NET Core 3.1 |  3.217 ns |  1.07 |     - |     - |     - |         - |
|       TypeMapperInt | .NET Core 3.1 |  6.042 ns |  2.03 |     - |     - |     - |         - |
|     DirectAccessInt | .NET Core 3.1 |  1.423 ns |  0.46 |     - |     - |     - |         - |
|   TypeMapperBoolean | .NET Core 3.1 |  5.771 ns |  1.93 |     - |     - |     - |         - |
| DirectAccessBoolean | .NET Core 3.1 |  1.370 ns |  0.46 |     - |     - |     - |         - |
|   TypeMapperWrapper | .NET Core 3.1 |  9.180 ns |  3.05 |     - |     - |     - |         - |
| DirectAccessWrapper | .NET Core 3.1 |  2.948 ns |  0.98 |     - |     - |     - |         - |
