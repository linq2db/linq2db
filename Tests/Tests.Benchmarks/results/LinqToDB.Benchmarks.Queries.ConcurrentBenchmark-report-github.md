```

BenchmarkDotNet v0.13.9+228a464e8be6c580ad9408e98f18813f6407fb5a, Windows 10 (10.0.17763.5328/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK 8.0.101
  [Host]     : .NET 7.0.15 (7.0.1523.57226), X64 RyuJIT AVX2
  Job-KJWIMT : .NET 6.0.26 (6.0.2623.60508), X64 RyuJIT AVX2
  Job-GULBRG : .NET 7.0.15 (7.0.1523.57226), X64 RyuJIT AVX2
  Job-LRGNRQ : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-SJROSW : .NET Framework 4.8 (4.8.4645.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method   | Runtime              | ThreadCount | Mean     | Allocated  |
|--------- |--------------------- |------------ |---------:|-----------:|
| **Linq**     | **.NET 6.0**             | **16**          | **15.54 ms** |  **448.04 KB** |
| Compiled | .NET 6.0             | 16          | 16.44 ms |   284.6 KB |
| Linq     | .NET 7.0             | 16          | 15.91 ms |  383.03 KB |
| Compiled | .NET 7.0             | 16          | 16.49 ms |  284.37 KB |
| Linq     | .NET Core 3.1        | 16          | 15.54 ms |  450.63 KB |
| Compiled | .NET Core 3.1        | 16          | 18.33 ms |  283.01 KB |
| Linq     | .NET Framework 4.7.2 | 16          | 15.54 ms |  483.51 KB |
| Compiled | .NET Framework 4.7.2 | 16          | 15.42 ms |     302 KB |
|          |                      |             |          |            |
| **Linq**     | **.NET 6.0**             | **32**          | **20.54 ms** |  **901.34 KB** |
| Compiled | .NET 6.0             | 32          | 24.66 ms |  575.75 KB |
| Linq     | .NET 7.0             | 32          | 23.61 ms |  772.87 KB |
| Compiled | .NET 7.0             | 32          | 25.40 ms |   575.1 KB |
| Linq     | .NET Core 3.1        | 32          | 20.96 ms |  906.19 KB |
| Compiled | .NET Core 3.1        | 32          | 25.29 ms |  572.28 KB |
| Linq     | .NET Framework 4.7.2 | 32          | 15.53 ms |  985.78 KB |
| Compiled | .NET Framework 4.7.2 | 32          | 24.21 ms |  620.76 KB |
|          |                      |             |          |            |
| **Linq**     | **.NET 6.0**             | **64**          | **19.08 ms** | **1800.11 KB** |
| Compiled | .NET 6.0             | 64          | 17.59 ms | 1140.53 KB |
| Linq     | .NET 7.0             | 64          | 19.68 ms | 1538.87 KB |
| Compiled | .NET 7.0             | 64          | 19.44 ms | 1130.84 KB |
| Linq     | .NET Core 3.1        | 64          | 16.98 ms | 1806.05 KB |
| Compiled | .NET Core 3.1        | 64          | 20.53 ms | 1134.12 KB |
| Linq     | .NET Framework 4.7.2 | 64          | 15.51 ms | 2030.93 KB |
| Compiled | .NET Framework 4.7.2 | 64          | 17.27 ms | 1241.78 KB |
