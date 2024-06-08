```

BenchmarkDotNet v0.13.12, Windows 10 (10.0.17763.5696/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK 8.0.204
  [Host]     : .NET 8.0.4 (8.0.424.16909), X64 RyuJIT AVX2
  Job-VZLGGZ : .NET 6.0.29 (6.0.2924.17105), X64 RyuJIT AVX2
  Job-AZKKUX : .NET 8.0.4 (8.0.424.16909), X64 RyuJIT AVX2
  Job-TQCFWV : .NET Framework 4.8 (4.8.4645.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method   | Runtime              | ThreadCount | Mean     | Allocated  |
|--------- |--------------------- |------------ |---------:|-----------:|
| **Linq**     | **.NET 6.0**             | **16**          | **16.82 ms** |  **406.07 KB** |
| Compiled | .NET 6.0             | 16          | 17.41 ms |  286.22 KB |
| Linq     | .NET 8.0             | 16          | 15.51 ms |  360.49 KB |
| Compiled | .NET 8.0             | 16          | 15.51 ms |  284.53 KB |
| Linq     | .NET Framework 4.6.2 | 16          | 15.53 ms |  438.13 KB |
| Compiled | .NET Framework 4.6.2 | 16          | 16.53 ms |  307.75 KB |
|          |                      |             |          |            |
| **Linq**     | **.NET 6.0**             | **32**          | **20.37 ms** |  **813.99 KB** |
| Compiled | .NET 6.0             | 32          | 23.35 ms |   575.8 KB |
| Linq     | .NET 8.0             | 32          | 15.49 ms |  723.97 KB |
| Compiled | .NET 8.0             | 32          | 15.50 ms |  575.07 KB |
| Linq     | .NET Framework 4.6.2 | 32          | 15.52 ms |  880.64 KB |
| Compiled | .NET Framework 4.6.2 | 32          | 22.14 ms |  621.51 KB |
|          |                      |             |          |            |
| **Linq**     | **.NET 6.0**             | **64**          | **17.06 ms** | **1629.13 KB** |
| Compiled | .NET 6.0             | 64          | 18.02 ms | 1140.85 KB |
| Linq     | .NET 8.0             | 64          | 15.48 ms | 1438.39 KB |
| Compiled | .NET 8.0             | 64          | 15.48 ms | 1138.81 KB |
| Linq     | .NET Framework 4.6.2 | 64          | 15.66 ms | 1825.02 KB |
| Compiled | .NET Framework 4.6.2 | 64          | 16.74 ms | 1256.27 KB |
