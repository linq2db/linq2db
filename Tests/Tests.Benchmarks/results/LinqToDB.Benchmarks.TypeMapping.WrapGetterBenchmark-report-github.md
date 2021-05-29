``` ini

BenchmarkDotNet=v0.12.1.1533-nightly, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417995 Hz, Resolution=292.5692 ns, Timer=TSC
  [Host]     : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT
  Job-GUCTZK : .NET 5.0.5 (5.0.521.16609), X64 RyuJIT
  Job-IOHEYN : .NET Core 3.1.14 (CoreCLR 4.700.21.16201, CoreFX 4.700.21.16208), X64 RyuJIT
  Job-FWTWYQ : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT

Jit=RyuJit  Platform=X64  

```
|              Method |              Runtime |      Mean |    Median | Ratio | Allocated |
|-------------------- |--------------------- |----------:|----------:|------:|----------:|
|    TypeMapperString |             .NET 5.0 |  5.696 ns |  5.577 ns |  4.13 |         - |
|  DirectAccessString |             .NET 5.0 |  1.077 ns |  1.065 ns |  0.79 |         - |
|       TypeMapperInt |             .NET 5.0 |  5.596 ns |  5.588 ns |  4.12 |         - |
|     DirectAccessInt |             .NET 5.0 |  1.038 ns |  1.039 ns |  0.74 |         - |
|      TypeMapperLong |             .NET 5.0 |  5.558 ns |  5.560 ns |  4.09 |         - |
|    DirectAccessLong |             .NET 5.0 |  1.069 ns |  1.070 ns |  0.76 |         - |
|   TypeMapperBoolean |             .NET 5.0 |  5.681 ns |  5.647 ns |  4.11 |         - |
| DirectAccessBoolean |             .NET 5.0 |  1.046 ns |  1.049 ns |  0.75 |         - |
|   TypeMapperWrapper |             .NET 5.0 | 11.473 ns | 11.450 ns |  8.45 |         - |
| DirectAccessWrapper |             .NET 5.0 |  1.034 ns |  1.037 ns |  0.74 |         - |
|      TypeMapperEnum |             .NET 5.0 | 27.103 ns | 27.107 ns | 19.58 |      24 B |
|    DirectAccessEnum |             .NET 5.0 |  1.122 ns |  1.126 ns |  0.80 |         - |
|   TypeMapperVersion |             .NET 5.0 |  5.608 ns |  5.604 ns |  4.05 |         - |
| DirectAccessVersion |             .NET 5.0 |  1.034 ns |  1.034 ns |  0.76 |         - |
|    TypeMapperString |        .NET Core 3.1 |  5.461 ns |  5.462 ns |  3.91 |         - |
|  DirectAccessString |        .NET Core 3.1 |  1.006 ns |  1.010 ns |  0.72 |         - |
|       TypeMapperInt |        .NET Core 3.1 |  5.944 ns |  5.813 ns |  4.40 |         - |
|     DirectAccessInt |        .NET Core 3.1 |  1.048 ns |  1.047 ns |  0.77 |         - |
|      TypeMapperLong |        .NET Core 3.1 |  5.187 ns |  5.168 ns |  3.71 |         - |
|    DirectAccessLong |        .NET Core 3.1 |  1.370 ns |  1.378 ns |  0.98 |         - |
|   TypeMapperBoolean |        .NET Core 3.1 |  6.052 ns |  5.939 ns |  4.44 |         - |
| DirectAccessBoolean |        .NET Core 3.1 |  1.063 ns |  1.061 ns |  0.76 |         - |
|   TypeMapperWrapper |        .NET Core 3.1 | 10.963 ns | 10.953 ns |  7.92 |         - |
| DirectAccessWrapper |        .NET Core 3.1 |  1.318 ns |  1.323 ns |  0.94 |         - |
|      TypeMapperEnum |        .NET Core 3.1 | 28.350 ns | 28.334 ns | 20.87 |      24 B |
|    DirectAccessEnum |        .NET Core 3.1 |  1.344 ns |  1.344 ns |  0.98 |         - |
|   TypeMapperVersion |        .NET Core 3.1 |  5.057 ns |  5.073 ns |  3.62 |         - |
| DirectAccessVersion |        .NET Core 3.1 |  1.424 ns |  1.348 ns |  1.02 |         - |
|    TypeMapperString | .NET Framework 4.7.2 | 19.674 ns | 19.692 ns | 14.21 |         - |
|  DirectAccessString | .NET Framework 4.7.2 |  1.388 ns |  1.347 ns |  1.00 |         - |
|       TypeMapperInt | .NET Framework 4.7.2 | 19.709 ns | 19.691 ns | 14.39 |         - |
|     DirectAccessInt | .NET Framework 4.7.2 |  1.367 ns |  1.345 ns |  0.98 |         - |
|      TypeMapperLong | .NET Framework 4.7.2 | 19.598 ns | 19.568 ns | 14.02 |         - |
|    DirectAccessLong | .NET Framework 4.7.2 |  1.309 ns |  1.310 ns |  0.94 |         - |
|   TypeMapperBoolean | .NET Framework 4.7.2 | 20.071 ns | 20.102 ns | 14.53 |         - |
| DirectAccessBoolean | .NET Framework 4.7.2 |  1.334 ns |  1.334 ns |  0.95 |         - |
|   TypeMapperWrapper | .NET Framework 4.7.2 | 31.777 ns | 31.851 ns | 23.20 |         - |
| DirectAccessWrapper | .NET Framework 4.7.2 |  1.100 ns |  1.099 ns |  0.79 |         - |
|      TypeMapperEnum | .NET Framework 4.7.2 | 56.811 ns | 56.699 ns | 41.11 |      24 B |
|    DirectAccessEnum | .NET Framework 4.7.2 |  1.336 ns |  1.332 ns |  0.96 |         - |
|   TypeMapperVersion | .NET Framework 4.7.2 | 19.633 ns | 19.528 ns | 14.46 |         - |
| DirectAccessVersion | .NET Framework 4.7.2 |  1.350 ns |  1.354 ns |  0.98 |         - |
