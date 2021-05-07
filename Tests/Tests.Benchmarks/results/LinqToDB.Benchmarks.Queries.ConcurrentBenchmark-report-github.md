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
|   Method |              Runtime | ThreadCount |      Mean |    Median | Ratio | Allocated |
|--------- |--------------------- |------------ |----------:|----------:|------:|----------:|
|     **Linq** |             **.NET 5.0** |          **16** |  **2.060 ms** |  **2.051 ms** |  **1.29** |    **603 KB** |
| Compiled |             .NET 5.0 |          16 |  2.097 ms |  2.094 ms |  1.32 |    487 KB |
|     Linq |        .NET Core 3.1 |          16 |  2.163 ms |  2.180 ms |  1.43 |    607 KB |
| Compiled |        .NET Core 3.1 |          16 | 16.329 ms | 16.387 ms | 10.18 |    482 KB |
|     Linq | .NET Framework 4.7.2 |          16 |  2.258 ms |  1.983 ms |  1.55 |    696 KB |
| Compiled | .NET Framework 4.7.2 |          16 |  1.524 ms |  1.528 ms |  1.00 |    512 KB |
|          |                      |             |           |           |       |           |
|     **Linq** |             **.NET 5.0** |          **32** |  **2.655 ms** |  **2.642 ms** |  **0.40** |  **1,208 KB** |
| Compiled |             .NET 5.0 |          32 | 16.590 ms | 16.620 ms |  2.44 |    974 KB |
|     Linq |        .NET Core 3.1 |          32 | 16.257 ms | 16.402 ms |  2.43 |  1,215 KB |
| Compiled |        .NET Core 3.1 |          32 | 16.302 ms | 16.260 ms |  2.40 |    964 KB |
|     Linq | .NET Framework 4.7.2 |          32 |  6.628 ms |  6.847 ms |  0.99 |  1,344 KB |
| Compiled | .NET Framework 4.7.2 |          32 |  6.857 ms |  7.057 ms |  1.00 |  1,024 KB |
|          |                      |             |           |           |       |           |
|     **Linq** |             **.NET 5.0** |          **64** | **16.468 ms** | **16.428 ms** |  **3.39** |  **2,410 KB** |
| Compiled |             .NET 5.0 |          64 | 16.640 ms | 16.649 ms |  3.43 |  1,948 KB |
|     Linq |        .NET Core 3.1 |          64 | 16.510 ms | 16.527 ms |  3.40 |  2,430 KB |
| Compiled |        .NET Core 3.1 |          64 | 16.527 ms | 16.484 ms |  3.40 |  1,928 KB |
|     Linq | .NET Framework 4.7.2 |          64 |  7.745 ms |  6.162 ms |  1.66 |  2,624 KB |
| Compiled | .NET Framework 4.7.2 |          64 |  4.833 ms |  5.159 ms |  1.00 |  2,048 KB |
