```

BenchmarkDotNet v0.13.9+228a464e8be6c580ad9408e98f18813f6407fb5a, Windows 10 (10.0.17763.4644/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK 7.0.401
  [Host]     : .NET 7.0.11 (7.0.1123.42427), X64 RyuJIT AVX2
  Job-DAXXNM : .NET 6.0.22 (6.0.2223.42425), X64 RyuJIT AVX2
  Job-SLTPYD : .NET 7.0.11 (7.0.1123.42427), X64 RyuJIT AVX2
  Job-YOWJJJ : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-OZLLFF : .NET Framework 4.8 (4.8.4645.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method                        | Runtime              | Mean      | Allocated |
|------------------------------ |--------------------- |----------:|----------:|
| Update_Nullable               | .NET 6.0             | 110.82 μs |  47.66 KB |
| Update_Nullable_Full          | .NET 6.0             | 263.42 μs |  50.35 KB |
| Compiled_Update_Nullable      | .NET 6.0             |  60.50 μs |  16.88 KB |
| Compiled_Update_Nullable_Full | .NET 6.0             |  70.44 μs |  19.56 KB |
| Update                        | .NET 6.0             | 243.56 μs |  46.09 KB |
| Update_Full                   | .NET 6.0             | 263.14 μs |  48.77 KB |
| Compiled_Update               | .NET 6.0             |  40.02 μs |  16.88 KB |
| Compiled_Update_Full          | .NET 6.0             |  69.23 μs |  19.56 KB |
| Update_Nullable               | .NET 7.0             | 187.40 μs |  32.51 KB |
| Update_Nullable_Full          | .NET 7.0             | 204.00 μs |  35.02 KB |
| Compiled_Update_Nullable      | .NET 7.0             |  57.04 μs |  16.81 KB |
| Compiled_Update_Nullable_Full | .NET 7.0             |  61.97 μs |  19.48 KB |
| Update                        | .NET 7.0             | 181.73 μs |  32.79 KB |
| Update_Full                   | .NET 7.0             | 207.70 μs |  35.16 KB |
| Compiled_Update               | .NET 7.0             |  56.18 μs |  16.81 KB |
| Compiled_Update_Full          | .NET 7.0             |  64.16 μs |  19.48 KB |
| Update_Nullable               | .NET Core 3.1        | 303.79 μs |  45.32 KB |
| Update_Nullable_Full          | .NET Core 3.1        | 328.07 μs |  48.01 KB |
| Compiled_Update_Nullable      | .NET Core 3.1        |  80.70 μs |  16.78 KB |
| Compiled_Update_Nullable_Full | .NET Core 3.1        |  41.13 μs |  19.47 KB |
| Update                        | .NET Core 3.1        | 301.16 μs |  45.99 KB |
| Update_Full                   | .NET Core 3.1        | 323.46 μs |  48.68 KB |
| Compiled_Update               | .NET Core 3.1        |  78.28 μs |  16.78 KB |
| Compiled_Update_Full          | .NET Core 3.1        |  91.00 μs |  19.47 KB |
| Update_Nullable               | .NET Framework 4.7.2 | 400.03 μs |  51.77 KB |
| Update_Nullable_Full          | .NET Framework 4.7.2 | 301.20 μs |   54.9 KB |
| Compiled_Update_Nullable      | .NET Framework 4.7.2 |  70.98 μs |  17.35 KB |
| Compiled_Update_Nullable_Full | .NET Framework 4.7.2 | 107.42 μs |  20.48 KB |
| Update                        | .NET Framework 4.7.2 | 351.68 μs |  50.97 KB |
| Update_Full                   | .NET Framework 4.7.2 | 294.61 μs |  54.11 KB |
| Compiled_Update               | .NET Framework 4.7.2 |  88.19 μs |  17.33 KB |
| Compiled_Update_Full          | .NET Framework 4.7.2 | 104.65 μs |  20.47 KB |
