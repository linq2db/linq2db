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
| Method                        | Runtime              | Mean      | Allocated |
|------------------------------ |--------------------- |----------:|----------:|
| Update_Nullable               | .NET 6.0             | 178.86 μs |  37.59 KB |
| Update_Nullable_Full          | .NET 6.0             | 197.04 μs |  40.41 KB |
| Compiled_Update_Nullable      | .NET 6.0             |  52.08 μs |  16.27 KB |
| Compiled_Update_Nullable_Full | .NET 6.0             |  65.26 μs |  19.09 KB |
| Update                        | .NET 6.0             | 174.14 μs |  37.23 KB |
| Update_Full                   | .NET 6.0             | 194.25 μs |  40.05 KB |
| Compiled_Update               | .NET 6.0             |  53.54 μs |  16.25 KB |
| Compiled_Update_Full          | .NET 6.0             |  66.53 μs |  19.08 KB |
| Update_Nullable               | .NET 8.0             |  94.37 μs |  29.32 KB |
| Update_Nullable_Full          | .NET 8.0             | 114.96 μs |  32.35 KB |
| Compiled_Update_Nullable      | .NET 8.0             |  34.26 μs |  16.23 KB |
| Compiled_Update_Nullable_Full | .NET 8.0             |  42.19 μs |  19.05 KB |
| Update                        | .NET 8.0             |  94.43 μs |  29.73 KB |
| Update_Full                   | .NET 8.0             | 108.30 μs |   32.1 KB |
| Compiled_Update               | .NET 8.0             |  37.46 μs |  16.22 KB |
| Compiled_Update_Full          | .NET 8.0             |  41.39 μs |  19.03 KB |
| Update_Nullable               | .NET Framework 4.6.2 | 244.41 μs |  43.78 KB |
| Update_Nullable_Full          | .NET Framework 4.6.2 | 311.34 μs |   49.2 KB |
| Compiled_Update_Nullable      | .NET Framework 4.6.2 |  72.28 μs |  17.38 KB |
| Compiled_Update_Nullable_Full | .NET Framework 4.6.2 |  98.96 μs |   22.8 KB |
| Update                        | .NET Framework 4.6.2 | 284.73 μs |   44.5 KB |
| Update_Full                   | .NET Framework 4.6.2 | 290.76 μs |  48.42 KB |
| Compiled_Update               | .NET Framework 4.6.2 |  79.05 μs |  17.36 KB |
| Compiled_Update_Full          | .NET Framework 4.6.2 |  87.84 μs |  22.79 KB |
