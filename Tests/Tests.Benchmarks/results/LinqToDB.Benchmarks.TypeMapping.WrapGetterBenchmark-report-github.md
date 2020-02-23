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
|              Method |       Runtime |      Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------- |-------------- |----------:|----------:|----------:|------:|--------:|-------:|------:|------:|----------:|
|    TypeMapperString |    .NET 4.6.2 | 13.344 ns | 0.3337 ns | 0.0867 ns | 12.35 |    0.23 |      - |     - |     - |         - |
|  DirectAccessString |    .NET 4.6.2 |  1.081 ns | 0.0931 ns | 0.0242 ns |  1.00 |    0.00 |      - |     - |     - |         - |
|       TypeMapperInt |    .NET 4.6.2 | 13.780 ns | 1.3207 ns | 0.3430 ns | 12.75 |    0.49 |      - |     - |     - |         - |
|     DirectAccessInt |    .NET 4.6.2 |  1.102 ns | 0.0825 ns | 0.0214 ns |  1.02 |    0.03 |      - |     - |     - |         - |
|      TypeMapperLong |    .NET 4.6.2 | 14.062 ns | 1.9031 ns | 0.4942 ns | 13.01 |    0.57 |      - |     - |     - |         - |
|    DirectAccessLong |    .NET 4.6.2 |  1.068 ns | 0.0535 ns | 0.0083 ns |  0.99 |    0.02 |      - |     - |     - |         - |
|   TypeMapperBoolean |    .NET 4.6.2 | 13.805 ns | 0.6958 ns | 0.1807 ns | 12.77 |    0.39 |      - |     - |     - |         - |
| DirectAccessBoolean |    .NET 4.6.2 |  1.069 ns | 0.0590 ns | 0.0091 ns |  0.99 |    0.02 |      - |     - |     - |         - |
|   TypeMapperWrapper |    .NET 4.6.2 | 19.925 ns | 0.4395 ns | 0.0241 ns | 18.44 |    0.54 |      - |     - |     - |         - |
| DirectAccessWrapper |    .NET 4.6.2 |  1.427 ns | 0.1055 ns | 0.0274 ns |  1.32 |    0.04 |      - |     - |     - |         - |
|      TypeMapperEnum |    .NET 4.6.2 | 49.016 ns | 2.0393 ns | 0.5296 ns | 45.36 |    1.39 | 0.0057 |     - |     - |      24 B |
|    DirectAccessEnum |    .NET 4.6.2 |  1.351 ns | 0.0310 ns | 0.0048 ns |  1.25 |    0.03 |      - |     - |     - |         - |
|   TypeMapperVersion |    .NET 4.6.2 | 13.675 ns | 0.7791 ns | 0.2023 ns | 12.65 |    0.34 |      - |     - |     - |         - |
| DirectAccessVersion |    .NET 4.6.2 |  1.078 ns | 0.0789 ns | 0.0122 ns |  1.00 |    0.03 |      - |     - |     - |         - |
|    TypeMapperString | .NET Core 2.1 |  4.973 ns | 0.1080 ns | 0.0281 ns |  4.60 |    0.12 |      - |     - |     - |         - |
|  DirectAccessString | .NET Core 2.1 |  1.110 ns | 0.2279 ns | 0.0592 ns |  1.03 |    0.07 |      - |     - |     - |         - |
|       TypeMapperInt | .NET Core 2.1 |  5.377 ns | 0.1601 ns | 0.0416 ns |  4.98 |    0.15 |      - |     - |     - |         - |
|     DirectAccessInt | .NET Core 2.1 |  1.368 ns | 0.0623 ns | 0.0034 ns |  1.27 |    0.04 |      - |     - |     - |         - |
|      TypeMapperLong | .NET Core 2.1 |  6.012 ns | 0.2578 ns | 0.0669 ns |  5.56 |    0.11 |      - |     - |     - |         - |
|    DirectAccessLong | .NET Core 2.1 |  1.414 ns | 0.0374 ns | 0.0097 ns |  1.31 |    0.03 |      - |     - |     - |         - |
|   TypeMapperBoolean | .NET Core 2.1 |  5.526 ns | 0.9402 ns | 0.2442 ns |  5.11 |    0.26 |      - |     - |     - |         - |
| DirectAccessBoolean | .NET Core 2.1 |  1.388 ns | 0.0353 ns | 0.0055 ns |  1.28 |    0.03 |      - |     - |     - |         - |
|   TypeMapperWrapper | .NET Core 2.1 | 12.105 ns | 0.8095 ns | 0.2102 ns | 11.20 |    0.37 |      - |     - |     - |         - |
| DirectAccessWrapper | .NET Core 2.1 |  1.081 ns | 0.0335 ns | 0.0052 ns |  1.00 |    0.03 |      - |     - |     - |         - |
|      TypeMapperEnum | .NET Core 2.1 | 33.685 ns | 0.6115 ns | 0.0946 ns | 31.11 |    0.74 | 0.0057 |     - |     - |      24 B |
|    DirectAccessEnum | .NET Core 2.1 |  1.345 ns | 0.0331 ns | 0.0051 ns |  1.24 |    0.03 |      - |     - |     - |         - |
|   TypeMapperVersion | .NET Core 2.1 |  5.757 ns | 0.6423 ns | 0.1668 ns |  5.33 |    0.21 |      - |     - |     - |         - |
| DirectAccessVersion | .NET Core 2.1 |  1.053 ns | 0.0963 ns | 0.0250 ns |  0.97 |    0.02 |      - |     - |     - |         - |
|    TypeMapperString | .NET Core 3.1 |  5.711 ns | 0.8040 ns | 0.2088 ns |  5.28 |    0.16 |      - |     - |     - |         - |
|  DirectAccessString | .NET Core 3.1 |  1.063 ns | 0.0480 ns | 0.0026 ns |  0.98 |    0.03 |      - |     - |     - |         - |
|       TypeMapperInt | .NET Core 3.1 |  5.910 ns | 0.1466 ns | 0.0381 ns |  5.47 |    0.10 |      - |     - |     - |         - |
|     DirectAccessInt | .NET Core 3.1 |  1.066 ns | 0.0413 ns | 0.0107 ns |  0.99 |    0.03 |      - |     - |     - |         - |
|      TypeMapperLong | .NET Core 3.1 |  5.236 ns | 0.1081 ns | 0.0281 ns |  4.84 |    0.12 |      - |     - |     - |         - |
|    DirectAccessLong | .NET Core 3.1 |  1.389 ns | 0.0865 ns | 0.0225 ns |  1.29 |    0.03 |      - |     - |     - |         - |
|   TypeMapperBoolean | .NET Core 3.1 |  5.971 ns | 0.1159 ns | 0.0064 ns |  5.53 |    0.17 |      - |     - |     - |         - |
| DirectAccessBoolean | .NET Core 3.1 |  1.083 ns | 0.0551 ns | 0.0143 ns |  1.00 |    0.01 |      - |     - |     - |         - |
|   TypeMapperWrapper | .NET Core 3.1 | 11.480 ns | 0.4555 ns | 0.1183 ns | 10.62 |    0.21 |      - |     - |     - |         - |
| DirectAccessWrapper | .NET Core 3.1 |  1.121 ns | 0.1671 ns | 0.0434 ns |  1.04 |    0.06 |      - |     - |     - |         - |
|      TypeMapperEnum | .NET Core 3.1 | 29.479 ns | 0.8213 ns | 0.2133 ns | 27.27 |    0.55 | 0.0057 |     - |     - |      24 B |
|    DirectAccessEnum | .NET Core 3.1 |  1.381 ns | 0.0787 ns | 0.0204 ns |  1.28 |    0.04 |      - |     - |     - |         - |
|   TypeMapperVersion | .NET Core 3.1 |  5.110 ns | 0.2685 ns | 0.0697 ns |  4.73 |    0.08 |      - |     - |     - |         - |
| DirectAccessVersion | .NET Core 3.1 |  1.355 ns | 0.0640 ns | 0.0099 ns |  1.25 |    0.02 |      - |     - |     - |         - |
