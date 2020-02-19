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
|                      Method |       Runtime |       Mean |     Error |    StdDev |  Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------------------- |-------------- |-----------:|----------:|----------:|-------:|--------:|-------:|------:|------:|----------:|
|            TypeMapperString |    .NET 4.6.2 |  14.410 ns | 0.4590 ns | 0.3036 ns |  10.95 |    0.54 |      - |     - |     - |         - |
|          DirectAccessString |    .NET 4.6.2 |   1.324 ns | 0.1135 ns | 0.0594 ns |   1.00 |    0.00 |      - |     - |     - |         - |
|   TypeMapperWrappedInstance |    .NET 4.6.2 |  71.036 ns | 3.0906 ns | 2.0442 ns |  53.29 |    3.36 | 0.0114 |     - |     - |      48 B |
| DirectAccessWrappedInstance |    .NET 4.6.2 |   1.375 ns | 0.1151 ns | 0.0685 ns |   1.04 |    0.06 |      - |     - |     - |         - |
|     TypeMapperGetEnumerator |    .NET 4.6.2 | 154.666 ns | 3.0641 ns | 1.0927 ns | 116.50 |    6.55 | 0.0134 |     - |     - |      56 B |
|   DirectAccessGetEnumerator |    .NET 4.6.2 | 132.434 ns | 2.6537 ns | 1.5792 ns | 100.18 |    3.58 | 0.0134 |     - |     - |      56 B |
|            TypeMapperString | .NET Core 2.1 |   7.625 ns | 0.2666 ns | 0.1764 ns |   5.79 |    0.31 |      - |     - |     - |         - |
|          DirectAccessString | .NET Core 2.1 |   2.597 ns | 0.2025 ns | 0.1205 ns |   1.98 |    0.14 |      - |     - |     - |         - |
|   TypeMapperWrappedInstance | .NET Core 2.1 |  52.904 ns | 1.0051 ns | 0.6648 ns |  40.10 |    1.95 | 0.0114 |     - |     - |      48 B |
| DirectAccessWrappedInstance | .NET Core 2.1 |   1.356 ns | 0.1063 ns | 0.0472 ns |   1.02 |    0.06 |      - |     - |     - |         - |
|     TypeMapperGetEnumerator | .NET Core 2.1 | 167.252 ns | 2.9601 ns | 1.0556 ns | 125.96 |    6.57 | 0.0074 |     - |     - |      32 B |
|   DirectAccessGetEnumerator | .NET Core 2.1 | 173.455 ns | 8.3952 ns | 5.5529 ns | 129.87 |    6.80 | 0.0074 |     - |     - |      32 B |
|            TypeMapperString | .NET Core 3.1 |   5.717 ns | 0.1520 ns | 0.0395 ns |   4.35 |    0.22 |      - |     - |     - |         - |
|          DirectAccessString | .NET Core 3.1 |   1.069 ns | 0.0644 ns | 0.0167 ns |   0.81 |    0.04 |      - |     - |     - |         - |
|   TypeMapperWrappedInstance | .NET Core 3.1 |  55.964 ns | 1.6618 ns | 1.0992 ns |  42.32 |    1.76 | 0.0114 |     - |     - |      48 B |
| DirectAccessWrappedInstance | .NET Core 3.1 |   1.566 ns | 0.2960 ns | 0.1958 ns |   1.14 |    0.16 |      - |     - |     - |         - |
|     TypeMapperGetEnumerator | .NET Core 3.1 | 129.809 ns | 8.2881 ns | 5.4821 ns |  99.43 |    5.40 | 0.0076 |     - |     - |      32 B |
|   DirectAccessGetEnumerator | .NET Core 3.1 | 123.024 ns | 1.1010 ns | 0.2859 ns |  93.60 |    4.81 | 0.0076 |     - |     - |      32 B |
