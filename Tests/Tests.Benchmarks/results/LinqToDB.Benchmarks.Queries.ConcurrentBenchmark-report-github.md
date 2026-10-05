```

BenchmarkDotNet v0.15.2, Windows 10 (10.0.17763.7553/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X 3.39GHz, 2 CPU, 32 logical and 16 physical cores
.NET SDK 9.0.302
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-FTOCRB : .NET 8.0.18 (8.0.1825.31117), X64 RyuJIT AVX2
  Job-DHTNJT : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-QIENBV : .NET Framework 4.8 (4.8.4795.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method   | Runtime              | ThreadCount | Mean     | Allocated  |
|--------- |--------------------- |------------ |---------:|-----------:|
| **Linq**     | **.NET 8.0**             | **16**          | **15.38 ms** |  **353.36 KB** |
| Compiled | .NET 8.0             | 16          | 15.52 ms |  290.03 KB |
| Linq     | .NET 9.0             | 16          | 15.63 ms |  356.32 KB |
| Compiled | .NET 9.0             | 16          | 15.52 ms |  289.62 KB |
| Linq     | .NET Framework 4.6.2 | 16          | 15.45 ms |  436.25 KB |
| Compiled | .NET Framework 4.6.2 | 16          | 15.42 ms |  307.75 KB |
|          |                      |             |          |            |
| **Linq**     | **.NET 8.0**             | **32**          | **15.49 ms** |  **714.74 KB** |
| Compiled | .NET 8.0             | 32          | 15.51 ms |   583.4 KB |
| Linq     | .NET 9.0             | 32          | 15.49 ms |  714.58 KB |
| Compiled | .NET 9.0             | 32          | 15.58 ms |  586.66 KB |
| Linq     | .NET Framework 4.6.2 | 32          | 15.53 ms |  870.51 KB |
| Compiled | .NET Framework 4.6.2 | 32          | 22.29 ms |  624.01 KB |
|          |                      |             |          |            |
| **Linq**     | **.NET 8.0**             | **64**          | **15.50 ms** | **1419.09 KB** |
| Compiled | .NET 8.0             | 64          | 15.48 ms |    1163 KB |
| Linq     | .NET 9.0             | 64          | 15.56 ms | 1424.44 KB |
| Compiled | .NET 9.0             | 64          | 15.50 ms |  1163.3 KB |
| Linq     | .NET Framework 4.6.2 | 64          | 15.50 ms | 1776.78 KB |
| Compiled | .NET Framework 4.6.2 | 64          | 17.35 ms | 1260.27 KB |
