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
| Update_Nullable               | .NET 8.0             | 122.93 μs |  63.66 KB |
| Update_Nullable_Full          | .NET 8.0             | 153.17 μs |  66.41 KB |
| Compiled_Update_Nullable      | .NET 8.0             |  34.55 μs |  51.02 KB |
| Compiled_Update_Nullable_Full | .NET 8.0             |  82.57 μs |  53.78 KB |
| Update                        | .NET 8.0             |  66.25 μs |   63.8 KB |
| Update_Full                   | .NET 8.0             | 159.41 μs |   66.1 KB |
| Compiled_Update               | .NET 8.0             |  76.55 μs |  51.02 KB |
| Compiled_Update_Full          | .NET 8.0             |  67.94 μs |  53.78 KB |
| Update_Nullable               | .NET 9.0             | 151.81 μs |   62.7 KB |
| Update_Nullable_Full          | .NET 9.0             |  74.34 μs |  65.23 KB |
| Compiled_Update_Nullable      | .NET 9.0             |  70.82 μs |  50.05 KB |
| Compiled_Update_Nullable_Full | .NET 9.0             |  76.32 μs |  52.88 KB |
| Update                        | .NET 9.0             |  69.19 μs |   62.6 KB |
| Update_Full                   | .NET 9.0             | 121.48 μs |  65.52 KB |
| Compiled_Update               | .NET 9.0             |  70.14 μs |  50.05 KB |
| Compiled_Update_Full          | .NET 9.0             |  57.30 μs |  52.88 KB |
| Update_Nullable               | .NET Framework 4.6.2 | 173.05 μs |  85.24 KB |
| Update_Nullable_Full          | .NET Framework 4.6.2 | 412.23 μs |  88.14 KB |
| Compiled_Update_Nullable      | .NET Framework 4.6.2 | 111.75 μs |  60.57 KB |
| Compiled_Update_Nullable_Full | .NET Framework 4.6.2 | 148.85 μs |  63.47 KB |
| Update                        | .NET Framework 4.6.2 | 388.41 μs |  85.57 KB |
| Update_Full                   | .NET Framework 4.6.2 | 410.65 μs |  88.47 KB |
| Compiled_Update               | .NET Framework 4.6.2 | 149.23 μs |  60.55 KB |
| Compiled_Update_Full          | .NET Framework 4.6.2 | 169.96 μs |  63.45 KB |
