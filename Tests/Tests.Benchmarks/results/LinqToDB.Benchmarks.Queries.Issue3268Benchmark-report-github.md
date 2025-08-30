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
| Method                        | Runtime              | Mean      | Allocated |
|------------------------------ |--------------------- |----------:|----------:|
| Update_Nullable               | .NET 8.0             |  85.60 μs |  32.77 KB |
| Update_Nullable_Full          | .NET 8.0             |  63.23 μs |   35.7 KB |
| Compiled_Update_Nullable      | .NET 8.0             |  31.84 μs |  16.81 KB |
| Compiled_Update_Nullable_Full | .NET 8.0             |  35.34 μs |  19.81 KB |
| Update                        | .NET 8.0             | 125.72 μs |  32.93 KB |
| Update_Full                   | .NET 8.0             | 141.99 μs |  35.85 KB |
| Compiled_Update               | .NET 8.0             |  34.12 μs |  16.81 KB |
| Compiled_Update_Full          | .NET 8.0             |  39.57 μs |  19.81 KB |
| Update_Nullable               | .NET 9.0             | 117.52 μs |  32.79 KB |
| Update_Nullable_Full          | .NET 9.0             | 132.21 μs |   36.1 KB |
| Compiled_Update_Nullable      | .NET 9.0             |  32.57 μs |  16.72 KB |
| Compiled_Update_Nullable_Full | .NET 9.0             |  31.60 μs |  19.72 KB |
| Update                        | .NET 9.0             | 114.01 μs |  32.71 KB |
| Update_Full                   | .NET 9.0             | 132.72 μs |  35.38 KB |
| Compiled_Update               | .NET 9.0             |  15.94 μs |  16.72 KB |
| Compiled_Update_Full          | .NET 9.0             |  33.51 μs |  19.72 KB |
| Update_Nullable               | .NET Framework 4.6.2 | 265.72 μs |  49.13 KB |
| Update_Nullable_Full          | .NET Framework 4.6.2 | 200.04 μs |  54.74 KB |
| Compiled_Update_Nullable      | .NET Framework 4.6.2 |  75.44 μs |  17.39 KB |
| Compiled_Update_Nullable_Full | .NET Framework 4.6.2 | 114.24 μs |     23 KB |
| Update                        | .NET Framework 4.6.2 | 173.15 μs |  49.47 KB |
| Update_Full                   | .NET Framework 4.6.2 | 418.79 μs |  55.08 KB |
| Compiled_Update               | .NET Framework 4.6.2 |  90.54 μs |  17.38 KB |
| Compiled_Update_Full          | .NET Framework 4.6.2 | 112.90 μs |  22.99 KB |
