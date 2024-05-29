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
| Method                        | Runtime              | Mean      | Allocated |
|------------------------------ |--------------------- |----------:|----------:|
| Update_Nullable               | .NET 6.0             | 204.72 μs |  40.12 KB |
| Update_Nullable_Full          | .NET 6.0             | 224.93 μs |  42.96 KB |
| Compiled_Update_Nullable      | .NET 6.0             |  78.33 μs |   19.2 KB |
| Compiled_Update_Nullable_Full | .NET 6.0             |  41.02 μs |  22.05 KB |
| Update                        | .NET 6.0             | 203.23 μs |  41.12 KB |
| Update_Full                   | .NET 6.0             | 224.04 μs |  43.96 KB |
| Compiled_Update               | .NET 6.0             |  81.30 μs |  19.19 KB |
| Compiled_Update_Full          | .NET 6.0             |  92.01 μs |  22.03 KB |
| Update_Nullable               | .NET 8.0             | 117.69 μs |  32.04 KB |
| Update_Nullable_Full          | .NET 8.0             | 136.71 μs |  35.49 KB |
| Compiled_Update_Nullable      | .NET 8.0             |  48.23 μs |  19.17 KB |
| Compiled_Update_Nullable_Full | .NET 8.0             |  54.76 μs |     22 KB |
| Update                        | .NET 8.0             | 117.58 μs |  32.34 KB |
| Update_Full                   | .NET 8.0             | 137.84 μs |  35.35 KB |
| Compiled_Update               | .NET 8.0             |  51.90 μs |  19.16 KB |
| Compiled_Update_Full          | .NET 8.0             |  58.72 μs |  21.98 KB |
| Update_Nullable               | .NET Framework 4.6.2 | 297.09 μs |  45.92 KB |
| Update_Nullable_Full          | .NET Framework 4.6.2 | 352.28 μs |  51.36 KB |
| Compiled_Update_Nullable      | .NET Framework 4.6.2 | 101.23 μs |  20.33 KB |
| Compiled_Update_Nullable_Full | .NET Framework 4.6.2 | 125.32 μs |  25.76 KB |
| Update                        | .NET Framework 4.6.2 | 319.91 μs |  47.01 KB |
| Update_Full                   | .NET Framework 4.6.2 | 355.82 μs |  52.45 KB |
| Compiled_Update               | .NET Framework 4.6.2 |  91.98 μs |  20.31 KB |
| Compiled_Update_Full          | .NET Framework 4.6.2 | 123.90 μs |  25.75 KB |
