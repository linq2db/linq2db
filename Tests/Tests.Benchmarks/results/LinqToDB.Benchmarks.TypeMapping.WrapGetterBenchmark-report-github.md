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
|              Method |       Runtime |      Mean | Ratio |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------- |-------------- |----------:|------:|-------:|------:|------:|----------:|
|    TypeMapperString |    .NET 4.6.2 | 13.218 ns |  9.47 |      - |     - |     - |         - |
|  DirectAccessString |    .NET 4.6.2 |  1.401 ns |  1.00 |      - |     - |     - |         - |
|       TypeMapperInt |    .NET 4.6.2 | 13.719 ns |  9.79 |      - |     - |     - |         - |
|     DirectAccessInt |    .NET 4.6.2 |  1.396 ns |  1.00 |      - |     - |     - |         - |
|      TypeMapperLong |    .NET 4.6.2 | 13.534 ns |  9.66 |      - |     - |     - |         - |
|    DirectAccessLong |    .NET 4.6.2 |  1.338 ns |  0.96 |      - |     - |     - |         - |
|   TypeMapperBoolean |    .NET 4.6.2 | 14.420 ns | 10.30 |      - |     - |     - |         - |
| DirectAccessBoolean |    .NET 4.6.2 |  1.352 ns |  0.97 |      - |     - |     - |         - |
|   TypeMapperWrapper |    .NET 4.6.2 | 19.752 ns | 14.09 |      - |     - |     - |         - |
| DirectAccessWrapper |    .NET 4.6.2 |  1.076 ns |  0.77 |      - |     - |     - |         - |
|      TypeMapperEnum |    .NET 4.6.2 | 45.411 ns | 32.42 | 0.0057 |     - |     - |      24 B |
|    DirectAccessEnum |    .NET 4.6.2 |  1.345 ns |  0.96 |      - |     - |     - |         - |
|   TypeMapperVersion |    .NET 4.6.2 | 13.599 ns |  9.71 |      - |     - |     - |         - |
| DirectAccessVersion |    .NET 4.6.2 |  1.399 ns |  1.00 |      - |     - |     - |         - |
|    TypeMapperString | .NET Core 2.1 |  5.103 ns |  3.64 |      - |     - |     - |         - |
|  DirectAccessString | .NET Core 2.1 |  1.099 ns |  0.78 |      - |     - |     - |         - |
|       TypeMapperInt | .NET Core 2.1 |  5.325 ns |  3.80 |      - |     - |     - |         - |
|     DirectAccessInt | .NET Core 2.1 |  1.177 ns |  0.84 |      - |     - |     - |         - |
|      TypeMapperLong | .NET Core 2.1 |  5.816 ns |  4.15 |      - |     - |     - |         - |
|    DirectAccessLong | .NET Core 2.1 |  1.083 ns |  0.78 |      - |     - |     - |         - |
|   TypeMapperBoolean | .NET Core 2.1 |  5.181 ns |  3.70 |      - |     - |     - |         - |
| DirectAccessBoolean | .NET Core 2.1 |  1.089 ns |  0.78 |      - |     - |     - |         - |
|   TypeMapperWrapper | .NET Core 2.1 | 11.821 ns |  8.47 |      - |     - |     - |         - |
| DirectAccessWrapper | .NET Core 2.1 |  1.076 ns |  0.77 |      - |     - |     - |         - |
|      TypeMapperEnum | .NET Core 2.1 | 32.995 ns | 23.54 | 0.0057 |     - |     - |      24 B |
|    DirectAccessEnum | .NET Core 2.1 |  1.074 ns |  0.77 |      - |     - |     - |         - |
|   TypeMapperVersion | .NET Core 2.1 |  5.636 ns |  4.04 |      - |     - |     - |         - |
| DirectAccessVersion | .NET Core 2.1 |  1.149 ns |  0.82 |      - |     - |     - |         - |
|    TypeMapperString | .NET Core 3.1 |  5.605 ns |  4.00 |      - |     - |     - |         - |
|  DirectAccessString | .NET Core 3.1 |  1.272 ns |  0.91 |      - |     - |     - |         - |
|       TypeMapperInt | .NET Core 3.1 |  5.843 ns |  4.17 |      - |     - |     - |         - |
|     DirectAccessInt | .NET Core 3.1 |  1.367 ns |  0.98 |      - |     - |     - |         - |
|      TypeMapperLong | .NET Core 3.1 |  5.255 ns |  3.75 |      - |     - |     - |         - |
|    DirectAccessLong | .NET Core 3.1 |  1.138 ns |  0.81 |      - |     - |     - |         - |
|   TypeMapperBoolean | .NET Core 3.1 |  5.876 ns |  4.19 |      - |     - |     - |         - |
| DirectAccessBoolean | .NET Core 3.1 |  1.288 ns |  0.92 |      - |     - |     - |         - |
|   TypeMapperWrapper | .NET Core 3.1 | 11.337 ns |  8.09 |      - |     - |     - |         - |
| DirectAccessWrapper | .NET Core 3.1 |  1.055 ns |  0.76 |      - |     - |     - |         - |
|      TypeMapperEnum | .NET Core 3.1 | 30.420 ns | 21.72 | 0.0057 |     - |     - |      24 B |
|    DirectAccessEnum | .NET Core 3.1 |  1.393 ns |  0.99 |      - |     - |     - |         - |
|   TypeMapperVersion | .NET Core 3.1 |  5.160 ns |  3.68 |      - |     - |     - |         - |
| DirectAccessVersion | .NET Core 3.1 |  1.283 ns |  0.92 |      - |     - |     - |         - |
