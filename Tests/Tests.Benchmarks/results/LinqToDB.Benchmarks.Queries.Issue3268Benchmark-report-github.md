```

BenchmarkDotNet v0.13.9+228a464e8be6c580ad9408e98f18813f6407fb5a, Windows 10 (10.0.17763.5328/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK 8.0.101
  [Host]     : .NET 7.0.15 (7.0.1523.57226), X64 RyuJIT AVX2
  Job-KJWIMT : .NET 6.0.26 (6.0.2623.60508), X64 RyuJIT AVX2
  Job-GULBRG : .NET 7.0.15 (7.0.1523.57226), X64 RyuJIT AVX2
  Job-LRGNRQ : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-SJROSW : .NET Framework 4.8 (4.8.4645.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method                        | Runtime              | Mean      | Allocated |
|------------------------------ |--------------------- |----------:|----------:|
| Update_Nullable               | .NET 6.0             | 180.23 μs |   46.1 KB |
| Update_Nullable_Full          | .NET 6.0             | 264.76 μs |  48.91 KB |
| Compiled_Update_Nullable      | .NET 6.0             |  60.84 μs |  16.88 KB |
| Compiled_Update_Nullable_Full | .NET 6.0             |  70.73 μs |  19.69 KB |
| Update                        | .NET 6.0             | 238.50 μs |   45.3 KB |
| Update_Full                   | .NET 6.0             | 118.53 μs |  48.12 KB |
| Compiled_Update               | .NET 6.0             |  60.51 μs |  16.88 KB |
| Compiled_Update_Full          | .NET 6.0             |  70.96 μs |  19.69 KB |
| Update_Nullable               | .NET 7.0             | 187.08 μs |  32.59 KB |
| Update_Nullable_Full          | .NET 7.0             | 215.85 μs |   35.7 KB |
| Compiled_Update_Nullable      | .NET 7.0             |  48.64 μs |  16.81 KB |
| Compiled_Update_Nullable_Full | .NET 7.0             |  64.45 μs |  19.61 KB |
| Update                        | .NET 7.0             | 179.28 μs |  32.74 KB |
| Update_Full                   | .NET 7.0             | 204.38 μs |  35.63 KB |
| Compiled_Update               | .NET 7.0             |  25.33 μs |  16.81 KB |
| Compiled_Update_Full          | .NET 7.0             |  63.37 μs |  19.61 KB |
| Update_Nullable               | .NET Core 3.1        | 305.51 μs |   46.2 KB |
| Update_Nullable_Full          | .NET Core 3.1        | 337.66 μs |  51.01 KB |
| Compiled_Update_Nullable      | .NET Core 3.1        |  72.56 μs |  16.78 KB |
| Compiled_Update_Nullable_Full | .NET Core 3.1        |  96.92 μs |  21.59 KB |
| Update                        | .NET Core 3.1        | 133.40 μs |  46.38 KB |
| Update_Full                   | .NET Core 3.1        | 334.12 μs |   51.2 KB |
| Compiled_Update               | .NET Core 3.1        |  79.42 μs |  16.78 KB |
| Compiled_Update_Full          | .NET Core 3.1        |  98.60 μs |  21.59 KB |
| Update_Nullable               | .NET Framework 4.7.2 | 360.96 μs |  49.12 KB |
| Update_Nullable_Full          | .NET Framework 4.7.2 | 418.10 μs |  54.54 KB |
| Compiled_Update_Nullable      | .NET Framework 4.7.2 |  41.60 μs |  17.35 KB |
| Compiled_Update_Nullable_Full | .NET Framework 4.7.2 | 112.07 μs |  22.75 KB |
| Update                        | .NET Framework 4.7.2 | 380.77 μs |  49.09 KB |
| Update_Full                   | .NET Framework 4.7.2 | 425.04 μs |  54.51 KB |
| Compiled_Update               | .NET Framework 4.7.2 |  90.32 μs |  17.33 KB |
| Compiled_Update_Full          | .NET Framework 4.7.2 | 110.09 μs |  22.74 KB |
