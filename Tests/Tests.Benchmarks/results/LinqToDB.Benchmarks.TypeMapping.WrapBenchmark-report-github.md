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
|                      Method |       Runtime |             Mean |           Error |         StdDev |           Median |        Ratio |    RatioSD |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|---------------------------- |-------------- |-----------------:|----------------:|---------------:|-----------------:|-------------:|-----------:|-------:|-------:|------:|----------:|
|            TypeMapperString |    .NET 4.6.2 |    32,342.093 ns |   4,051.7151 ns |  2,679.9586 ns |    33,254.280 ns |    22,151.18 |   3,197.57 | 0.6714 | 0.1831 |     - |    4382 B |
|          DirectAccessString |    .NET 4.6.2 |         1.475 ns |       0.2194 ns |      0.1451 ns |         1.479 ns |         1.00 |       0.00 |      - |      - |     - |         - |
|   TypeMapperWrappedInstance |    .NET 4.6.2 | 1,602,064.210 ns | 114,336.8937 ns | 75,626.7759 ns | 1,615,330.166 ns | 1,097,399.03 | 147,064.14 | 9.7656 | 1.9531 |     - |   64542 B |
| DirectAccessWrappedInstance |    .NET 4.6.2 |         1.194 ns |       0.9124 ns |      0.6035 ns |         1.467 ns |         0.83 |       0.43 |      - |      - |     - |         - |
|     TypeMapperGetEnumerator |    .NET 4.6.2 |    25,365.174 ns |   1,099.3381 ns |    727.1441 ns |    25,458.787 ns |    17,367.73 |   2,050.65 | 0.5493 | 0.1526 |     - |    3411 B |
|   DirectAccessGetEnumerator |    .NET 4.6.2 |       218.457 ns |      13.2225 ns |      7.8685 ns |       216.713 ns |       149.44 |      20.15 | 0.0134 |      - |     - |      56 B |
|            TypeMapperString | .NET Core 2.1 |    28,512.294 ns |   2,005.8150 ns |  1,193.6283 ns |    28,216.524 ns |    19,469.87 |   2,376.31 | 0.4578 | 0.1526 |     - |    2976 B |
|          DirectAccessString | .NET Core 2.1 |         3.652 ns |       0.3789 ns |      0.2506 ns |         3.698 ns |         2.50 |       0.35 |      - |      - |     - |         - |
|   TypeMapperWrappedInstance | .NET Core 2.1 |   933,218.004 ns |  43,427.2795 ns | 28,724.4565 ns |   928,305.013 ns |   639,711.26 |  84,308.07 | 2.9297 | 0.9766 |     - |   24521 B |
| DirectAccessWrappedInstance | .NET Core 2.1 |         1.166 ns |       0.2298 ns |      0.1368 ns |         1.170 ns |         0.79 |       0.10 |      - |      - |     - |         - |
|     TypeMapperGetEnumerator | .NET Core 2.1 |    15,362.072 ns |     863.0960 ns |    513.6146 ns |    15,439.658 ns |    10,485.12 |   1,174.06 | 0.3357 | 0.0916 |     - |    2120 B |
|   DirectAccessGetEnumerator | .NET Core 2.1 |       208.476 ns |       9.6204 ns |      6.3633 ns |       209.392 ns |       142.61 |      15.80 | 0.0074 |      - |     - |      32 B |
|            TypeMapperString | .NET Core 3.1 |    15,496.097 ns |   1,383.3052 ns |    823.1827 ns |    15,202.430 ns |    10,593.10 |   1,395.41 | 0.4578 | 0.1678 |     - |    2904 B |
|          DirectAccessString | .NET Core 3.1 |         1.650 ns |       0.2050 ns |      0.1356 ns |         1.604 ns |         1.13 |       0.19 |      - |      - |     - |         - |
|   TypeMapperWrappedInstance | .NET Core 3.1 |   800,490.614 ns |  65,079.0008 ns | 38,727.4695 ns |   790,121.360 ns |   544,915.84 |  46,014.78 | 2.9297 | 0.9766 |     - |   24049 B |
| DirectAccessWrappedInstance | .NET Core 3.1 |         1.136 ns |       0.2099 ns |      0.1388 ns |         1.124 ns |         0.78 |       0.14 |      - |      - |     - |         - |
|     TypeMapperGetEnumerator | .NET Core 3.1 |    10,051.892 ns |     284.9953 ns |    169.5961 ns |    10,039.447 ns |     6,866.89 |     806.63 | 0.3357 | 0.0916 |     - |    2080 B |
|   DirectAccessGetEnumerator | .NET Core 3.1 |       149.275 ns |      10.1630 ns |      6.7222 ns |       147.695 ns |       102.18 |      12.28 | 0.0076 |      - |     - |      32 B |
