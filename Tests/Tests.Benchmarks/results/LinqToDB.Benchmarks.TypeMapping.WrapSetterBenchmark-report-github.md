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
|              Method |       Runtime |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------- |-------------- |----------:|----------:|----------:|------:|--------:|------:|------:|------:|----------:|
|    TypeMapperString |    .NET 4.6.2 | 15.944 ns | 2.1415 ns | 0.5561 ns |  4.97 |    0.21 |     - |     - |     - |         - |
|  DirectAccessString |    .NET 4.6.2 |  3.207 ns | 0.1888 ns | 0.0490 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|       TypeMapperInt |    .NET 4.6.2 | 14.219 ns | 1.1407 ns | 0.2962 ns |  4.43 |    0.11 |     - |     - |     - |         - |
|     DirectAccessInt |    .NET 4.6.2 |  1.354 ns | 0.0485 ns | 0.0027 ns |  0.42 |    0.01 |     - |     - |     - |         - |
|   TypeMapperBoolean |    .NET 4.6.2 | 14.144 ns | 1.3887 ns | 0.3606 ns |  4.41 |    0.13 |     - |     - |     - |         - |
| DirectAccessBoolean |    .NET 4.6.2 |  1.361 ns | 0.0111 ns | 0.0006 ns |  0.42 |    0.01 |     - |     - |     - |         - |
|   TypeMapperWrapper |    .NET 4.6.2 | 16.761 ns | 1.2894 ns | 0.3349 ns |  5.23 |    0.13 |     - |     - |     - |         - |
| DirectAccessWrapper |    .NET 4.6.2 |  3.222 ns | 0.0537 ns | 0.0083 ns |  1.00 |    0.02 |     - |     - |     - |         - |
|    TypeMapperString | .NET Core 2.1 |  9.252 ns | 0.7492 ns | 0.1946 ns |  2.89 |    0.08 |     - |     - |     - |         - |
|  DirectAccessString | .NET Core 2.1 |  4.621 ns | 0.0752 ns | 0.0116 ns |  1.44 |    0.03 |     - |     - |     - |         - |
|       TypeMapperInt | .NET Core 2.1 |  7.286 ns | 0.1662 ns | 0.0257 ns |  2.27 |    0.04 |     - |     - |     - |         - |
|     DirectAccessInt | .NET Core 2.1 |  2.449 ns | 0.0676 ns | 0.0176 ns |  0.76 |    0.01 |     - |     - |     - |         - |
|   TypeMapperBoolean | .NET Core 2.1 |  7.269 ns | 0.1409 ns | 0.0218 ns |  2.26 |    0.04 |     - |     - |     - |         - |
| DirectAccessBoolean | .NET Core 2.1 |  2.788 ns | 0.1111 ns | 0.0289 ns |  0.87 |    0.01 |     - |     - |     - |         - |
|   TypeMapperWrapper | .NET Core 2.1 |  8.850 ns | 1.0298 ns | 0.2674 ns |  2.76 |    0.09 |     - |     - |     - |         - |
| DirectAccessWrapper | .NET Core 2.1 |  3.018 ns | 0.4719 ns | 0.1226 ns |  0.94 |    0.03 |     - |     - |     - |         - |
|    TypeMapperString | .NET Core 3.1 |  8.009 ns | 0.1945 ns | 0.0505 ns |  2.50 |    0.02 |     - |     - |     - |         - |
|  DirectAccessString | .NET Core 3.1 |  2.988 ns | 0.1065 ns | 0.0277 ns |  0.93 |    0.02 |     - |     - |     - |         - |
|       TypeMapperInt | .NET Core 3.1 |  6.019 ns | 0.0502 ns | 0.0027 ns |  1.87 |    0.04 |     - |     - |     - |         - |
|     DirectAccessInt | .NET Core 3.1 |  1.350 ns | 0.0512 ns | 0.0079 ns |  0.42 |    0.01 |     - |     - |     - |         - |
|   TypeMapperBoolean | .NET Core 3.1 |  6.106 ns | 0.8156 ns | 0.2118 ns |  1.90 |    0.08 |     - |     - |     - |         - |
| DirectAccessBoolean | .NET Core 3.1 |  1.329 ns | 0.0617 ns | 0.0095 ns |  0.41 |    0.01 |     - |     - |     - |         - |
|   TypeMapperWrapper | .NET Core 3.1 |  8.619 ns | 0.2630 ns | 0.0683 ns |  2.69 |    0.05 |     - |     - |     - |         - |
| DirectAccessWrapper | .NET Core 3.1 |  2.691 ns | 0.1426 ns | 0.0370 ns |  0.84 |    0.02 |     - |     - |     - |         - |
