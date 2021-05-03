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
|     **Linq** |             **.NET 5.0** |          **16** |  **2.246 ms** |  **1.921 ms** |  **1.21** |    **682 KB** |
| Compiled |             .NET 5.0 |          16 |  1.994 ms |  1.979 ms |  0.71 |    536 KB |
|     Linq |        .NET Core 3.1 |          16 |  2.055 ms |  2.061 ms |  0.25 |    656 KB |
| Compiled |        .NET Core 3.1 |          16 |  1.979 ms |  1.976 ms |  0.26 |    531 KB |
|     Linq | .NET Framework 4.7.2 |          16 |  9.400 ms |  9.764 ms |  5.38 |    768 KB |
| Compiled | .NET Framework 4.7.2 |          16 |  3.565 ms |  1.720 ms |  1.00 |    640 KB |
|          |                      |             |           |           |       |           |
|     **Linq** |             **.NET 5.0** |          **32** |  **6.320 ms** |  **3.971 ms** |  **0.72** |  **1,336 KB** |
| Compiled |             .NET 5.0 |          32 | 16.207 ms | 16.248 ms |  2.50 |  1,073 KB |
|     Linq |        .NET Core 3.1 |          32 | 15.914 ms | 16.328 ms |  2.09 |  1,310 KB |
| Compiled |        .NET Core 3.1 |          32 | 16.188 ms | 16.220 ms |  2.49 |  1,063 KB |
|     Linq | .NET Framework 4.7.2 |          32 |  9.465 ms |  9.896 ms |  1.27 |  1,536 KB |
| Compiled | .NET Framework 4.7.2 |          32 |  7.801 ms |  8.387 ms |  1.00 |  1,280 KB |
|          |                      |             |           |           |       |           |
|     **Linq** |             **.NET 5.0** |          **64** |  **4.609 ms** |  **4.592 ms** |  **0.59** |  **2,660 KB** |
| Compiled |             .NET 5.0 |          64 |  3.201 ms |  3.206 ms |  0.42 |  2,145 KB |
|     Linq |        .NET Core 3.1 |          64 | 12.482 ms | 16.548 ms |  1.50 |  2,616 KB |
| Compiled |        .NET Core 3.1 |          64 | 16.439 ms | 16.565 ms |  2.16 |  2,125 KB |
|     Linq | .NET Framework 4.7.2 |          64 |  8.681 ms |  6.609 ms |  1.20 |  3,072 KB |
| Compiled | .NET Framework 4.7.2 |          64 |  7.758 ms |  7.970 ms |  1.00 |  2,560 KB |
