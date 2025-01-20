```

BenchmarkDotNet v0.14.0, Windows 10 (10.0.17763.6766/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4775.0), X64 RyuJIT VectorSize=256
  Job-GEKMDY : .NET 6.0.36 (6.0.3624.51421), X64 RyuJIT AVX2
  Job-WEIMGV : .NET 8.0.12 (8.0.1224.60305), X64 RyuJIT AVX2
  Job-ARZZBJ : .NET 9.0.1 (9.0.124.61010), X64 RyuJIT AVX2
  Job-HBTJES : .NET Framework 4.8 (4.8.4775.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method   | Runtime              | ThreadCount | Mean     | Allocated  |
|--------- |--------------------- |------------ |---------:|-----------:|
| **Linq**     | **.NET 6.0**             | **16**          | **15.94 ms** |   **446.9 KB** |
| Compiled | .NET 6.0             | 16          | 16.36 ms |  335.21 KB |
| Linq     | .NET 8.0             | 16          | 15.52 ms |  398.55 KB |
| Compiled | .NET 8.0             | 16          | 15.51 ms |  334.23 KB |
| Linq     | .NET 9.0             | 16          | 15.50 ms |   400.4 KB |
| Compiled | .NET 9.0             | 16          | 15.51 ms |  335.47 KB |
| Linq     | .NET Framework 4.6.2 | 16          | 15.52 ms |  478.76 KB |
| Compiled | .NET Framework 4.6.2 | 16          | 15.58 ms |   356.5 KB |
|          |                      |             |          |            |
| **Linq**     | **.NET 6.0**             | **32**          | **24.87 ms** |  **900.83 KB** |
| Compiled | .NET 6.0             | 32          | 23.40 ms |  676.32 KB |
| Linq     | .NET 8.0             | 32          | 15.50 ms |  805.05 KB |
| Compiled | .NET 8.0             | 32          | 15.49 ms |  673.97 KB |
| Linq     | .NET 9.0             | 32          | 15.50 ms |  805.32 KB |
| Compiled | .NET 9.0             | 32          | 15.50 ms |  676.61 KB |
| Linq     | .NET Framework 4.6.2 | 32          | 15.52 ms |  968.89 KB |
| Compiled | .NET Framework 4.6.2 | 32          | 20.31 ms |  721.51 KB |
|          |                      |             |          |            |
| **Linq**     | **.NET 6.0**             | **64**          | **18.12 ms** | **1791.06 KB** |
| Compiled | .NET 6.0             | 64          | 19.94 ms | 1345.98 KB |
| Linq     | .NET 8.0             | 64          | 15.48 ms | 1599.26 KB |
| Compiled | .NET 8.0             | 64          | 15.53 ms | 1340.74 KB |
| Linq     | .NET 9.0             | 64          | 15.48 ms |    1602 KB |
| Compiled | .NET 9.0             | 64          | 15.52 ms | 1341.85 KB |
| Linq     | .NET Framework 4.6.2 | 64          | 15.55 ms | 2010.03 KB |
| Compiled | .NET Framework 4.6.2 | 64          | 17.10 ms | 1468.77 KB |
