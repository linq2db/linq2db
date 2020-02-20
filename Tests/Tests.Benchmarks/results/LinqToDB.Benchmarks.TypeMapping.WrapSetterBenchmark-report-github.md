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
|              Method |       Runtime |           Mean |          Error |         StdDev |     Ratio |  RatioSD |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|-------------------- |-------------- |---------------:|---------------:|---------------:|----------:|---------:|-------:|-------:|------:|----------:|
|    TypeMapperString |    .NET 4.6.2 | 143,735.110 ns |  1,594.2935 ns |    414.0330 ns | 46,428.16 | 2,369.78 | 1.9531 | 0.2441 |     - |    9106 B |
|  DirectAccessString |    .NET 4.6.2 |       3.199 ns |      0.2366 ns |      0.1565 ns |      1.00 |     0.00 |      - |      - |     - |         - |
|       TypeMapperInt |    .NET 4.6.2 | 133,293.691 ns |  1,734.2699 ns |    450.3844 ns | 43,061.60 | 2,338.10 | 1.9531 | 0.2441 |     - |    9156 B |
|     DirectAccessInt |    .NET 4.6.2 |       1.056 ns |      0.0560 ns |      0.0145 ns |      0.34 |     0.02 |      - |      - |     - |         - |
|   TypeMapperBoolean |    .NET 4.6.2 | 135,995.884 ns |  2,202.2437 ns |    977.8099 ns | 43,196.70 | 2,159.56 | 1.9531 | 0.2441 |     - |    9153 B |
| DirectAccessBoolean |    .NET 4.6.2 |       1.094 ns |      0.0528 ns |      0.0137 ns |      0.35 |     0.02 |      - |      - |     - |         - |
|   TypeMapperWrapper |    .NET 4.6.2 | 245,255.516 ns |  1,420.4638 ns |    368.8899 ns | 79,227.37 | 4,194.95 | 2.1973 | 0.4883 |     - |    9390 B |
| DirectAccessWrapper |    .NET 4.6.2 |       3.005 ns |      0.1245 ns |      0.0741 ns |      0.95 |     0.05 |      - |      - |     - |         - |
|    TypeMapperString | .NET Core 2.1 | 114,028.978 ns |  4,762.1490 ns |  3,149.8667 ns | 35,726.93 | 2,100.78 | 1.5869 | 0.3662 |     - |    6905 B |
|  DirectAccessString | .NET Core 2.1 |       4.682 ns |      0.1285 ns |      0.0334 ns |      1.51 |     0.08 |      - |      - |     - |         - |
|       TypeMapperInt | .NET Core 2.1 | 105,798.473 ns |    915.5814 ns |    237.7736 ns | 34,177.62 | 1,823.58 | 1.5869 | 0.3662 |     - |    7488 B |
|     DirectAccessInt | .NET Core 2.1 |       2.786 ns |      0.0904 ns |      0.0538 ns |      0.88 |     0.04 |      - |      - |     - |         - |
|   TypeMapperBoolean | .NET Core 2.1 | 111,756.085 ns |  5,891.8402 ns |  3,897.0875 ns | 35,006.60 | 2,026.47 | 1.5869 | 0.3662 |     - |    7065 B |
| DirectAccessBoolean | .NET Core 2.1 |       2.883 ns |      0.2050 ns |      0.1356 ns |      0.90 |     0.03 |      - |      - |     - |         - |
|   TypeMapperWrapper | .NET Core 2.1 | 200,326.119 ns |  3,556.5486 ns |    923.6244 ns | 64,703.85 | 3,220.17 | 1.7090 | 0.4883 |     - |    7481 B |
| DirectAccessWrapper | .NET Core 2.1 |       3.293 ns |      0.0827 ns |      0.0215 ns |      1.06 |     0.05 |      - |      - |     - |         - |
|    TypeMapperString | .NET Core 3.1 | 106,311.117 ns |  1,907.3757 ns |    997.5944 ns | 33,522.55 | 1,727.56 | 1.5869 | 0.3662 |     - |    6729 B |
|  DirectAccessString | .NET Core 3.1 |       3.305 ns |      0.3406 ns |      0.2253 ns |      1.04 |     0.10 |      - |      - |     - |         - |
|       TypeMapperInt | .NET Core 3.1 |  97,066.928 ns |  1,453.5101 ns |    645.3675 ns | 30,843.18 | 1,786.00 | 1.5869 | 0.3662 |     - |    6777 B |
|     DirectAccessInt | .NET Core 3.1 |       1.345 ns |      0.0298 ns |      0.0077 ns |      0.43 |     0.02 |      - |      - |     - |         - |
|   TypeMapperBoolean | .NET Core 3.1 | 101,315.067 ns |  1,670.3816 ns |    595.6742 ns | 32,406.81 | 1,779.06 | 1.5869 | 0.3662 |     - |    6889 B |
| DirectAccessBoolean | .NET Core 3.1 |       1.373 ns |      0.0360 ns |      0.0093 ns |      0.44 |     0.02 |      - |      - |     - |         - |
|   TypeMapperWrapper | .NET Core 3.1 | 207,558.920 ns | 17,286.0505 ns | 11,433.6521 ns | 64,969.12 | 3,826.91 | 1.7090 | 0.2441 |     - |    7305 B |
| DirectAccessWrapper | .NET Core 3.1 |       2.695 ns |      0.0796 ns |      0.0284 ns |      0.86 |     0.05 |      - |      - |     - |         - |
