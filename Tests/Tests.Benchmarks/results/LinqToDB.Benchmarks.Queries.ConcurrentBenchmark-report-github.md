```

BenchmarkDotNet v0.13.12, Windows 10 (10.0.17763.5458/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK 8.0.200
  [Host]     : .NET 8.0.2 (8.0.224.6711), X64 RyuJIT AVX2
  Job-GXDOCB : .NET 6.0.27 (6.0.2724.6912), X64 RyuJIT AVX2
  Job-YDFVLV : .NET 8.0.2 (8.0.224.6711), X64 RyuJIT AVX2
  Job-SBTNYY : .NET Framework 4.8 (4.8.4645.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method   | Runtime              | ThreadCount | Mean     | Allocated  |
|--------- |--------------------- |------------ |---------:|-----------:|
| **Linq**     | **.NET 6.0**             | **16**          | **16.70 ms** |  **381.77 KB** |
| Compiled | .NET 6.0             | 16          | 17.01 ms |  253.72 KB |
| Linq     | .NET 8.0             | 16          | 15.57 ms |  329.24 KB |
| Compiled | .NET 8.0             | 16          | 15.51 ms |  251.55 KB |
| Linq     | .NET Framework 4.6.2 | 16          | 15.52 ms |  410.13 KB |
| Compiled | .NET Framework 4.6.2 | 16          | 16.03 ms |  276.02 KB |
|          |                      |             |          |            |
| **Linq**     | **.NET 6.0**             | **32**          | **21.60 ms** |  **764.62 KB** |
| Compiled | .NET 6.0             | 32          | 23.86 ms |  511.57 KB |
| Linq     | .NET 8.0             | 32          | 15.49 ms |  662.72 KB |
| Compiled | .NET 8.0             | 32          | 15.51 ms |  511.48 KB |
| Linq     | .NET Framework 4.6.2 | 32          | 15.51 ms |  828.52 KB |
| Compiled | .NET Framework 4.6.2 | 32          | 21.97 ms |   555.5 KB |
|          |                      |             |          |            |
| **Linq**     | **.NET 6.0**             | **64**          | **18.00 ms** | **1527.67 KB** |
| Compiled | .NET 6.0             | 64          | 20.89 ms | 1014.94 KB |
| Linq     | .NET 8.0             | 64          | 15.71 ms | 1316.76 KB |
| Compiled | .NET 8.0             | 64          | 15.63 ms | 1012.71 KB |
| Linq     | .NET Framework 4.6.2 | 64          | 15.40 ms | 1683.28 KB |
| Compiled | .NET Framework 4.6.2 | 64          | 16.44 ms | 1121.29 KB |
