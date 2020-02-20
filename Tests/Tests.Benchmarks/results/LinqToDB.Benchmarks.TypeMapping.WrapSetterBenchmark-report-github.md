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
|              Method |       Runtime |       Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------- |-------------- |-----------:|----------:|----------:|------:|--------:|------:|------:|------:|----------:|
|    TypeMapperString |    .NET 4.6.2 | 16.1182 ns | 0.6830 ns | 0.4518 ns |  5.41 |    0.15 |     - |     - |     - |         - |
|  DirectAccessString |    .NET 4.6.2 |  2.9876 ns | 0.0888 ns | 0.0529 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|       TypeMapperInt |    .NET 4.6.2 | 14.3691 ns | 0.6327 ns | 0.4185 ns |  4.80 |    0.16 |     - |     - |     - |         - |
|     DirectAccessInt |    .NET 4.6.2 |  1.2570 ns | 0.1700 ns | 0.1125 ns |  0.43 |    0.04 |     - |     - |     - |         - |
|   TypeMapperBoolean |    .NET 4.6.2 | 14.5355 ns | 0.4089 ns | 0.2704 ns |  4.85 |    0.11 |     - |     - |     - |         - |
| DirectAccessBoolean |    .NET 4.6.2 |  1.1003 ns | 0.0482 ns | 0.0287 ns |  0.37 |    0.01 |     - |     - |     - |         - |
|   TypeMapperWrapper |    .NET 4.6.2 | 17.0161 ns | 0.5200 ns | 0.2720 ns |  5.69 |    0.16 |     - |     - |     - |         - |
| DirectAccessWrapper |    .NET 4.6.2 |  3.0994 ns | 0.1043 ns | 0.0690 ns |  1.04 |    0.04 |     - |     - |     - |         - |
|    TypeMapperString | .NET Core 2.1 |  9.9811 ns | 1.0428 ns | 0.6897 ns |  3.36 |    0.25 |     - |     - |     - |         - |
|  DirectAccessString | .NET Core 2.1 |  4.7235 ns | 0.3761 ns | 0.0977 ns |  1.58 |    0.04 |     - |     - |     - |         - |
|       TypeMapperInt | .NET Core 2.1 |  7.6099 ns | 0.4036 ns | 0.2670 ns |  2.55 |    0.13 |     - |     - |     - |         - |
|     DirectAccessInt | .NET Core 2.1 |  2.5193 ns | 0.0721 ns | 0.0257 ns |  0.84 |    0.02 |     - |     - |     - |         - |
|   TypeMapperBoolean | .NET Core 2.1 |  7.5374 ns | 0.1813 ns | 0.1199 ns |  2.52 |    0.06 |     - |     - |     - |         - |
| DirectAccessBoolean | .NET Core 2.1 |  2.8612 ns | 0.2005 ns | 0.1326 ns |  0.95 |    0.04 |     - |     - |     - |         - |
|   TypeMapperWrapper | .NET Core 2.1 |  8.6108 ns | 0.3059 ns | 0.2023 ns |  2.88 |    0.09 |     - |     - |     - |         - |
| DirectAccessWrapper | .NET Core 2.1 |  2.9706 ns | 0.1030 ns | 0.0681 ns |  0.99 |    0.03 |     - |     - |     - |         - |
|    TypeMapperString | .NET Core 3.1 |  8.0799 ns | 0.4705 ns | 0.3112 ns |  2.71 |    0.08 |     - |     - |     - |         - |
|  DirectAccessString | .NET Core 3.1 |  2.8051 ns | 0.1344 ns | 0.0800 ns |  0.94 |    0.03 |     - |     - |     - |         - |
|       TypeMapperInt | .NET Core 3.1 |  6.2578 ns | 0.2480 ns | 0.1641 ns |  2.09 |    0.09 |     - |     - |     - |         - |
|     DirectAccessInt | .NET Core 3.1 |  0.9828 ns | 0.0427 ns | 0.0152 ns |  0.33 |    0.01 |     - |     - |     - |         - |
|   TypeMapperBoolean | .NET Core 3.1 |  6.3530 ns | 0.2151 ns | 0.1423 ns |  2.12 |    0.07 |     - |     - |     - |         - |
| DirectAccessBoolean | .NET Core 3.1 |  1.0589 ns | 0.0562 ns | 0.0201 ns |  0.35 |    0.01 |     - |     - |     - |         - |
|   TypeMapperWrapper | .NET Core 3.1 |  8.6753 ns | 0.2274 ns | 0.1504 ns |  2.91 |    0.07 |     - |     - |     - |         - |
| DirectAccessWrapper | .NET Core 3.1 |  3.0918 ns | 0.1094 ns | 0.0284 ns |  1.03 |    0.03 |     - |     - |     - |         - |
