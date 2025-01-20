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
| Method                        | Runtime              | Mean      | Allocated |
|------------------------------ |--------------------- |----------:|----------:|
| Update_Nullable               | .NET 6.0             | 205.43 μs |  43.26 KB |
| Update_Nullable_Full          | .NET 6.0             | 222.71 μs |  46.13 KB |
| Compiled_Update_Nullable      | .NET 6.0             |  70.76 μs |  21.86 KB |
| Compiled_Update_Nullable_Full | .NET 6.0             |  39.12 μs |  24.73 KB |
| Update                        | .NET 6.0             | 201.13 μs |  44.13 KB |
| Update_Full                   | .NET 6.0             | 226.53 μs |  47.01 KB |
| Compiled_Update               | .NET 6.0             |  66.81 μs |  21.92 KB |
| Compiled_Update_Full          | .NET 6.0             |  40.52 μs |   24.8 KB |
| Update_Nullable               | .NET 8.0             | 112.74 μs |  34.46 KB |
| Update_Nullable_Full          | .NET 8.0             | 128.34 μs |  37.01 KB |
| Compiled_Update_Nullable      | .NET 8.0             |  40.39 μs |  21.83 KB |
| Compiled_Update_Nullable_Full | .NET 8.0             |  55.18 μs |  24.69 KB |
| Update                        | .NET 8.0             | 109.79 μs |  34.49 KB |
| Update_Full                   | .NET 8.0             | 123.99 μs |  37.05 KB |
| Compiled_Update               | .NET 8.0             |  22.16 μs |  21.89 KB |
| Compiled_Update_Full          | .NET 8.0             |  56.61 μs |  24.75 KB |
| Update_Nullable               | .NET 9.0             |  46.44 μs |  34.07 KB |
| Update_Nullable_Full          | .NET 9.0             | 115.94 μs |  37.45 KB |
| Compiled_Update_Nullable      | .NET 9.0             |  45.50 μs |  21.73 KB |
| Compiled_Update_Nullable_Full | .NET 9.0             |  50.28 μs |  24.59 KB |
| Update                        | .NET 9.0             | 100.31 μs |  34.43 KB |
| Update_Full                   | .NET 9.0             | 112.46 μs |  36.98 KB |
| Compiled_Update               | .NET 9.0             |  45.35 μs |   21.8 KB |
| Compiled_Update_Full          | .NET 9.0             |  51.26 μs |  24.66 KB |
| Update_Nullable               | .NET Framework 4.6.2 | 142.97 μs |  48.52 KB |
| Update_Nullable_Full          | .NET Framework 4.6.2 | 363.00 μs |  53.99 KB |
| Compiled_Update_Nullable      | .NET Framework 4.6.2 |  91.56 μs |  22.75 KB |
| Compiled_Update_Nullable_Full | .NET Framework 4.6.2 | 125.54 μs |  28.22 KB |
| Update                        | .NET Framework 4.6.2 | 317.53 μs |  47.81 KB |
| Update_Full                   | .NET Framework 4.6.2 | 351.88 μs |  53.27 KB |
| Compiled_Update               | .NET Framework 4.6.2 |  99.69 μs |  22.82 KB |
| Compiled_Update_Full          | .NET Framework 4.6.2 | 109.14 μs |  28.29 KB |
