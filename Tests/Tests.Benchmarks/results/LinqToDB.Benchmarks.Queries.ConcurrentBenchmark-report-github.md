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
|   Method |              Runtime | ThreadCount |      Mean |    Median | Ratio | Allocated |
|--------- |--------------------- |------------ |----------:|----------:|------:|----------:|
|     **Linq** |             **.NET 5.0** |          **16** | **16.179 ms** | **16.385 ms** |  **2.61** |    **743 KB** |
| Compiled |             .NET 5.0 |          16 | 16.222 ms | 16.331 ms |  2.79 |    429 KB |
|     Linq |        .NET Core 3.1 |          16 | 16.164 ms | 16.190 ms |  2.06 |    728 KB |
| Compiled |        .NET Core 3.1 |          16 | 15.863 ms | 15.861 ms |  2.59 |    427 KB |
|     Linq | .NET Framework 4.7.2 |          16 |  8.642 ms |  9.101 ms |  1.22 |    896 KB |
| Compiled | .NET Framework 4.7.2 |          16 |  8.316 ms |  7.906 ms |  1.00 |    512 KB |
|          |                      |             |           |           |       |           |
|     **Linq** |             **.NET 5.0** |          **32** |  **9.508 ms** |  **7.275 ms** |  **5.60** |  **1,483 KB** |
| Compiled |             .NET 5.0 |          32 | 16.139 ms | 16.375 ms | 11.09 |    858 KB |
|     Linq |        .NET Core 3.1 |          32 | 16.283 ms | 16.416 ms | 11.53 |  1,462 KB |
| Compiled |        .NET Core 3.1 |          32 | 16.040 ms | 16.288 ms | 11.21 |    854 KB |
|     Linq | .NET Framework 4.7.2 |          32 |  7.902 ms |  8.416 ms |  5.38 |  1,792 KB |
| Compiled | .NET Framework 4.7.2 |          32 |  1.565 ms |  1.449 ms |  1.00 |  1,024 KB |
|          |                      |             |           |           |       |           |
|     **Linq** |             **.NET 5.0** |          **64** |  **5.931 ms** |  **5.909 ms** |  **0.96** |  **2,966 KB** |
| Compiled |             .NET 5.0 |          64 | 13.753 ms | 16.554 ms |  2.23 |  1,717 KB |
|     Linq |        .NET Core 3.1 |          64 | 16.350 ms | 16.422 ms |  2.69 |  2,918 KB |
| Compiled |        .NET Core 3.1 |          64 | 16.437 ms | 16.537 ms |  2.61 |  1,709 KB |
|     Linq | .NET Framework 4.7.2 |          64 |  9.148 ms |  6.577 ms |  1.49 |  3,584 KB |
| Compiled | .NET Framework 4.7.2 |          64 |  7.141 ms |  6.314 ms |  1.00 |  2,048 KB |
