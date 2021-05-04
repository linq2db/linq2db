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
|              Method |              Runtime |       Mean |     Median | Ratio | Allocated |
|-------------------- |--------------------- |-----------:|-----------:|------:|----------:|
|    TypeMapperString |             .NET 5.0 |  5.6903 ns |  5.5064 ns |  5.07 |         - |
|  DirectAccessString |             .NET 5.0 |  1.1777 ns |  1.1771 ns |  1.03 |         - |
|       TypeMapperInt |             .NET 5.0 |  5.6903 ns |  5.7193 ns |  4.97 |         - |
|     DirectAccessInt |             .NET 5.0 |  1.1048 ns |  1.0893 ns |  0.96 |         - |
|      TypeMapperLong |             .NET 5.0 |  5.7120 ns |  5.5904 ns |  5.03 |         - |
|    DirectAccessLong |             .NET 5.0 |  1.0199 ns |  1.0146 ns |  0.89 |         - |
|   TypeMapperBoolean |             .NET 5.0 |  5.3830 ns |  5.3847 ns |  4.70 |         - |
| DirectAccessBoolean |             .NET 5.0 |  1.0472 ns |  1.0507 ns |  0.91 |         - |
|   TypeMapperWrapper |             .NET 5.0 | 11.8808 ns | 11.7710 ns | 10.19 |         - |
| DirectAccessWrapper |             .NET 5.0 |  1.1083 ns |  1.1115 ns |  0.96 |         - |
|      TypeMapperEnum |             .NET 5.0 | 27.7396 ns | 27.7299 ns | 24.24 |      24 B |
|    DirectAccessEnum |             .NET 5.0 |  1.0677 ns |  1.0725 ns |  0.93 |         - |
|   TypeMapperVersion |             .NET 5.0 |  5.8933 ns |  5.9063 ns |  5.09 |         - |
| DirectAccessVersion |             .NET 5.0 |  1.0874 ns |  1.0867 ns |  0.95 |         - |
|    TypeMapperString |        .NET Core 3.1 |  5.3541 ns |  5.3227 ns |  4.64 |         - |
|  DirectAccessString |        .NET Core 3.1 |  1.2921 ns |  1.2728 ns |  1.12 |         - |
|       TypeMapperInt |        .NET Core 3.1 |  6.0621 ns |  6.0015 ns |  5.40 |         - |
|     DirectAccessInt |        .NET Core 3.1 |  1.0783 ns |  1.0835 ns |  0.94 |         - |
|      TypeMapperLong |        .NET Core 3.1 |  5.1483 ns |  5.1373 ns |  4.49 |         - |
|    DirectAccessLong |        .NET Core 3.1 |  1.3993 ns |  1.3936 ns |  1.22 |         - |
|   TypeMapperBoolean |        .NET Core 3.1 |  5.6977 ns |  5.6756 ns |  4.97 |         - |
| DirectAccessBoolean |        .NET Core 3.1 |  1.8980 ns |  1.6826 ns |  1.56 |         - |
|   TypeMapperWrapper |        .NET Core 3.1 | 11.3609 ns | 11.4225 ns |  9.89 |         - |
| DirectAccessWrapper |        .NET Core 3.1 |  0.9958 ns |  0.9986 ns |  0.86 |         - |
|      TypeMapperEnum |        .NET Core 3.1 | 30.2036 ns | 29.9287 ns | 26.98 |      24 B |
|    DirectAccessEnum |        .NET Core 3.1 |  1.2768 ns |  1.2771 ns |  1.10 |         - |
|   TypeMapperVersion |        .NET Core 3.1 |  5.3282 ns |  5.3612 ns |  4.63 |         - |
| DirectAccessVersion |        .NET Core 3.1 |  1.3378 ns |  1.2615 ns |  1.08 |         - |
|    TypeMapperString | .NET Framework 4.7.2 | 21.5824 ns | 21.8370 ns | 18.84 |         - |
|  DirectAccessString | .NET Framework 4.7.2 |  1.1501 ns |  1.1397 ns |  1.00 |         - |
|       TypeMapperInt | .NET Framework 4.7.2 | 19.9631 ns | 19.8262 ns | 17.48 |         - |
|     DirectAccessInt | .NET Framework 4.7.2 |  1.1476 ns |  1.1103 ns |  1.06 |         - |
|      TypeMapperLong | .NET Framework 4.7.2 | 19.9085 ns | 19.7676 ns | 17.45 |         - |
|    DirectAccessLong | .NET Framework 4.7.2 |  1.0640 ns |  1.0632 ns |  0.93 |         - |
|   TypeMapperBoolean | .NET Framework 4.7.2 | 19.7044 ns | 19.5568 ns | 17.30 |         - |
| DirectAccessBoolean | .NET Framework 4.7.2 |  1.0505 ns |  1.0515 ns |  0.92 |         - |
|   TypeMapperWrapper | .NET Framework 4.7.2 | 33.7418 ns | 33.8056 ns | 29.44 |         - |
| DirectAccessWrapper | .NET Framework 4.7.2 |  1.2039 ns |  1.2066 ns |  1.05 |         - |
|      TypeMapperEnum | .NET Framework 4.7.2 | 59.7919 ns | 59.0720 ns | 52.61 |      24 B |
|    DirectAccessEnum | .NET Framework 4.7.2 |  1.0373 ns |  1.0387 ns |  0.90 |         - |
|   TypeMapperVersion | .NET Framework 4.7.2 | 20.2471 ns | 20.4047 ns | 17.62 |         - |
| DirectAccessVersion | .NET Framework 4.7.2 |  1.1070 ns |  1.0829 ns |  0.93 |         - |
