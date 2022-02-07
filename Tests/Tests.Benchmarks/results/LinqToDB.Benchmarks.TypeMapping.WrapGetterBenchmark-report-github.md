``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.16299.125 (1709/FallCreatorsUpdate/Redstone3)
Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge), 1 CPU, 8 logical and 4 physical cores
Frequency=3417994 Hz, Resolution=292.5693 ns, Timer=TSC
.NET SDK=5.0.402
  [Host]     : .NET 5.0.11 (5.0.1121.47308), X64 RyuJIT
  Job-ODZCDL : .NET 5.0.11 (5.0.1121.47308), X64 RyuJIT
  Job-PCJJBI : .NET Core 3.1.20 (CoreCLR 4.700.21.47003, CoreFX 4.700.21.47101), X64 RyuJIT
  Job-HHEMGO : .NET Framework 4.8 (4.8.3928.0), X64 RyuJIT

Jit=RyuJit  Platform=X64  

```
|              Method |              Runtime |       Mean | Ratio | Allocated |
|-------------------- |--------------------- |-----------:|------:|----------:|
|    TypeMapperString |             .NET 5.0 |  5.5683 ns |  5.09 |         - |
|  DirectAccessString |             .NET 5.0 |  0.9847 ns |  0.90 |         - |
|       TypeMapperInt |             .NET 5.0 |  5.6175 ns |  5.24 |         - |
|     DirectAccessInt |             .NET 5.0 |  1.0491 ns |  0.96 |         - |
|      TypeMapperLong |             .NET 5.0 |  5.6735 ns |  5.19 |         - |
|    DirectAccessLong |             .NET 5.0 |  1.0764 ns |  0.98 |         - |
|   TypeMapperBoolean |             .NET 5.0 |  5.5791 ns |  5.17 |         - |
| DirectAccessBoolean |             .NET 5.0 |  1.0441 ns |  0.96 |         - |
|   TypeMapperWrapper |             .NET 5.0 | 11.6084 ns | 10.51 |         - |
| DirectAccessWrapper |             .NET 5.0 |  1.1270 ns |  1.03 |         - |
|      TypeMapperEnum |             .NET 5.0 | 27.5311 ns | 25.70 |      24 B |
|    DirectAccessEnum |             .NET 5.0 |  1.0713 ns |  0.98 |         - |
|   TypeMapperVersion |             .NET 5.0 |  5.5962 ns |  5.12 |         - |
| DirectAccessVersion |             .NET 5.0 |  1.0363 ns |  0.95 |         - |
|    TypeMapperString |        .NET Core 3.1 |  5.5656 ns |  5.09 |         - |
|  DirectAccessString |        .NET Core 3.1 |  1.3221 ns |  1.21 |         - |
|       TypeMapperInt |        .NET Core 3.1 |  5.6946 ns |  5.28 |         - |
|     DirectAccessInt |        .NET Core 3.1 |  1.4216 ns |  1.32 |         - |
|      TypeMapperLong |        .NET Core 3.1 |  5.0298 ns |  4.69 |         - |
|    DirectAccessLong |        .NET Core 3.1 |  1.0468 ns |  0.96 |         - |
|   TypeMapperBoolean |        .NET Core 3.1 |  5.7767 ns |  5.32 |         - |
| DirectAccessBoolean |        .NET Core 3.1 |  1.3571 ns |  1.24 |         - |
|   TypeMapperWrapper |        .NET Core 3.1 | 10.7993 ns |  9.88 |         - |
| DirectAccessWrapper |        .NET Core 3.1 |  1.0451 ns |  0.96 |         - |
|      TypeMapperEnum |        .NET Core 3.1 | 28.4395 ns | 26.18 |      24 B |
|    DirectAccessEnum |        .NET Core 3.1 |  1.0458 ns |  0.96 |         - |
|   TypeMapperVersion |        .NET Core 3.1 |  4.9747 ns |  4.55 |         - |
| DirectAccessVersion |        .NET Core 3.1 |  1.0429 ns |  0.97 |         - |
|    TypeMapperString | .NET Framework 4.7.2 | 19.5529 ns | 17.92 |         - |
|  DirectAccessString | .NET Framework 4.7.2 |  1.0963 ns |  1.00 |         - |
|       TypeMapperInt | .NET Framework 4.7.2 | 19.3256 ns | 17.68 |         - |
|     DirectAccessInt | .NET Framework 4.7.2 |  0.9913 ns |  0.91 |         - |
|      TypeMapperLong | .NET Framework 4.7.2 | 19.2869 ns | 17.90 |         - |
|    DirectAccessLong | .NET Framework 4.7.2 |  1.0880 ns |  1.00 |         - |
|   TypeMapperBoolean | .NET Framework 4.7.2 | 19.7415 ns | 18.14 |         - |
| DirectAccessBoolean | .NET Framework 4.7.2 |  1.0849 ns |  1.00 |         - |
|   TypeMapperWrapper | .NET Framework 4.7.2 | 31.4814 ns | 28.80 |         - |
| DirectAccessWrapper | .NET Framework 4.7.2 |  1.3390 ns |  1.22 |         - |
|      TypeMapperEnum | .NET Framework 4.7.2 | 54.7049 ns | 50.36 |      24 B |
|    DirectAccessEnum | .NET Framework 4.7.2 |  1.0571 ns |  0.97 |         - |
|   TypeMapperVersion | .NET Framework 4.7.2 | 19.2867 ns | 17.76 |         - |
| DirectAccessVersion | .NET Framework 4.7.2 |  1.3505 ns |  1.25 |         - |
