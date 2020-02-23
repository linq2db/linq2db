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
|    TypeMapperString |    .NET 4.6.2 | 15.502 ns | 0.5665 ns | 0.1471 ns |  5.26 |    0.06 |     - |     - |     - |         - |
|  DirectAccessString |    .NET 4.6.2 |  2.953 ns | 0.0659 ns | 0.0102 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|       TypeMapperInt |    .NET 4.6.2 | 14.184 ns | 0.5916 ns | 0.1536 ns |  4.81 |    0.05 |     - |     - |     - |         - |
|     DirectAccessInt |    .NET 4.6.2 |  1.063 ns | 0.0391 ns | 0.0061 ns |  0.36 |    0.00 |     - |     - |     - |         - |
|   TypeMapperBoolean |    .NET 4.6.2 | 14.330 ns | 2.7163 ns | 0.7054 ns |  4.75 |    0.04 |     - |     - |     - |         - |
| DirectAccessBoolean |    .NET 4.6.2 |  1.066 ns | 0.0545 ns | 0.0142 ns |  0.36 |    0.00 |     - |     - |     - |         - |
|   TypeMapperWrapper |    .NET 4.6.2 | 16.693 ns | 0.4397 ns | 0.1142 ns |  5.64 |    0.06 |     - |     - |     - |         - |
| DirectAccessWrapper |    .NET 4.6.2 |  2.956 ns | 0.0490 ns | 0.0076 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|    TypeMapperString | .NET Core 2.1 |  9.231 ns | 0.2226 ns | 0.0578 ns |  3.13 |    0.02 |     - |     - |     - |         - |
|  DirectAccessString | .NET Core 2.1 |  4.599 ns | 0.0721 ns | 0.0112 ns |  1.56 |    0.01 |     - |     - |     - |         - |
|       TypeMapperInt | .NET Core 2.1 |  7.533 ns | 0.7817 ns | 0.2030 ns |  2.57 |    0.05 |     - |     - |     - |         - |
|     DirectAccessInt | .NET Core 2.1 |  2.463 ns | 0.0751 ns | 0.0195 ns |  0.83 |    0.01 |     - |     - |     - |         - |
|   TypeMapperBoolean | .NET Core 2.1 |  7.378 ns | 0.2567 ns | 0.0667 ns |  2.50 |    0.02 |     - |     - |     - |         - |
| DirectAccessBoolean | .NET Core 2.1 |  2.772 ns | 0.1044 ns | 0.0271 ns |  0.94 |    0.01 |     - |     - |     - |         - |
|   TypeMapperWrapper | .NET Core 2.1 |  8.471 ns | 0.2048 ns | 0.0532 ns |  2.87 |    0.03 |     - |     - |     - |         - |
| DirectAccessWrapper | .NET Core 2.1 |  2.961 ns | 0.0638 ns | 0.0166 ns |  1.00 |    0.01 |     - |     - |     - |         - |
|    TypeMapperString | .NET Core 3.1 |  8.290 ns | 0.3719 ns | 0.0966 ns |  2.80 |    0.04 |     - |     - |     - |         - |
|  DirectAccessString | .NET Core 3.1 |  2.973 ns | 0.0591 ns | 0.0092 ns |  1.01 |    0.00 |     - |     - |     - |         - |
|       TypeMapperInt | .NET Core 3.1 |  6.073 ns | 0.3901 ns | 0.1013 ns |  2.05 |    0.04 |     - |     - |     - |         - |
|     DirectAccessInt | .NET Core 3.1 |  1.374 ns | 0.0332 ns | 0.0086 ns |  0.46 |    0.00 |     - |     - |     - |         - |
|   TypeMapperBoolean | .NET Core 3.1 |  6.076 ns | 0.2094 ns | 0.0544 ns |  2.06 |    0.02 |     - |     - |     - |         - |
| DirectAccessBoolean | .NET Core 3.1 |  1.362 ns | 0.0530 ns | 0.0082 ns |  0.46 |    0.00 |     - |     - |     - |         - |
|   TypeMapperWrapper | .NET Core 3.1 |  8.603 ns | 0.1413 ns | 0.0219 ns |  2.91 |    0.01 |     - |     - |     - |         - |
| DirectAccessWrapper | .NET Core 3.1 |  2.704 ns | 0.2321 ns | 0.0603 ns |  0.92 |    0.02 |     - |     - |     - |         - |
