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
| **Linq**     | **.NET 8.0**             | **16**          | **15.51 ms** |  **385.01 KB** |
| Compiled | .NET 8.0             | 16          | 15.53 ms |  282.87 KB |
| Linq     | .NET 9.0             | 16          | 15.51 ms |  384.69 KB |
| Compiled | .NET 9.0             | 16          | 15.54 ms |  283.28 KB |
| Linq     | .NET Framework 4.6.2 | 16          | 15.54 ms |   482.5 KB |
| Compiled | .NET Framework 4.6.2 | 16          | 15.54 ms |  305.25 KB |
|          |                      |             |          |            |
| **Linq**     | **.NET 8.0**             | **32**          | **15.53 ms** |  **772.97 KB** |
| Compiled | .NET 8.0             | 32          | 15.51 ms |  573.21 KB |
| Linq     | .NET 9.0             | 32          | 15.52 ms |  778.96 KB |
| Compiled | .NET 9.0             | 32          | 15.43 ms |     570 KB |
| Linq     | .NET Framework 4.6.2 | 32          | 15.52 ms |  979.52 KB |
| Compiled | .NET Framework 4.6.2 | 32          | 21.31 ms |  622.77 KB |
|          |                      |             |          |            |
| **Linq**     | **.NET 8.0**             | **64**          | **15.53 ms** | **1541.73 KB** |
| Compiled | .NET 8.0             | 64          | 15.56 ms | 1134.37 KB |
| Linq     | .NET 9.0             | 64          | 15.56 ms | 1544.43 KB |
| Compiled | .NET 9.0             | 64          | 15.51 ms | 1133.59 KB |
| Linq     | .NET Framework 4.6.2 | 64          | 15.40 ms | 2011.53 KB |
| Compiled | .NET Framework 4.6.2 | 64          | 18.77 ms | 1247.53 KB |
