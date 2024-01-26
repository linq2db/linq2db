```

BenchmarkDotNet v0.13.9+228a464e8be6c580ad9408e98f18813f6407fb5a, Windows 10 (10.0.17763.4644/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK 7.0.401
  [Host]     : .NET 7.0.11 (7.0.1123.42427), X64 RyuJIT AVX2
  Job-DAXXNM : .NET 6.0.22 (6.0.2223.42425), X64 RyuJIT AVX2
  Job-SLTPYD : .NET 7.0.11 (7.0.1123.42427), X64 RyuJIT AVX2
  Job-YOWJJJ : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-OZLLFF : .NET Framework 4.8 (4.8.4645.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method   | Runtime              | ThreadCount | Mean     | Allocated  |
|--------- |--------------------- |------------ |---------:|-----------:|
| **Linq**     | **.NET 6.0**             | **16**          | **15.69 ms** |  **449.69 KB** |
| Compiled | .NET 6.0             | 16          | 16.10 ms |  282.59 KB |
| Linq     | .NET 7.0             | 16          | 15.72 ms |  383.85 KB |
| Compiled | .NET 7.0             | 16          | 15.84 ms |  283.63 KB |
| Linq     | .NET Core 3.1        | 16          | 15.94 ms |  448.52 KB |
| Compiled | .NET Core 3.1        | 16          | 16.45 ms |  283.41 KB |
| Linq     | .NET Framework 4.7.2 | 16          | 15.55 ms |  482.63 KB |
| Compiled | .NET Framework 4.7.2 | 16          | 15.62 ms |     303 KB |
|          |                      |             |          |            |
| **Linq**     | **.NET 6.0**             | **32**          | **23.00 ms** |  **902.72 KB** |
| Compiled | .NET 6.0             | 32          | 23.61 ms |  571.96 KB |
| Linq     | .NET 7.0             | 32          | 20.81 ms |   770.8 KB |
| Compiled | .NET 7.0             | 32          | 24.66 ms |   572.8 KB |
| Linq     | .NET Core 3.1        | 32          | 21.12 ms |  903.35 KB |
| Compiled | .NET Core 3.1        | 32          | 23.83 ms |   571.7 KB |
| Linq     | .NET Framework 4.7.2 | 32          | 15.43 ms |  983.01 KB |
| Compiled | .NET Framework 4.7.2 | 32          | 21.63 ms |  618.51 KB |
|          |                      |             |          |            |
| **Linq**     | **.NET 6.0**             | **64**          | **16.64 ms** |  **1800.1 KB** |
| Compiled | .NET 6.0             | 64          | 16.58 ms | 1136.54 KB |
| Linq     | .NET 7.0             | 64          | 17.95 ms | 1540.06 KB |
| Compiled | .NET 7.0             | 64          | 18.66 ms | 1135.59 KB |
| Linq     | .NET Core 3.1        | 64          | 16.36 ms | 1802.74 KB |
| Compiled | .NET Core 3.1        | 64          | 18.96 ms |  1132.2 KB |
| Linq     | .NET Framework 4.7.2 | 64          | 15.53 ms | 2019.79 KB |
| Compiled | .NET Framework 4.7.2 | 64          | 17.26 ms | 1240.02 KB |
