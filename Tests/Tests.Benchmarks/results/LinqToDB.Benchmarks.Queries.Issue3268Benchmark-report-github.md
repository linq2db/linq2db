``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-WUZRIO : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-EMBONI : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-HZWTXS : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-VIGHHX : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                        Method |              Runtime |      Mean |    Median | Ratio |    Gen0 | Allocated | Alloc Ratio |
|------------------------------ |--------------------- |----------:|----------:|------:|--------:|----------:|------------:|
|               Update_Nullable |             .NET 6.0 | 250.30 μs | 304.34 μs |  2.08 |  2.9297 |  51.32 KB |        2.54 |
|          Update_Nullable_Full |             .NET 6.0 | 328.13 μs | 328.18 μs |  3.08 |  3.4180 |  57.06 KB |        2.83 |
|      Compiled_Update_Nullable |             .NET 6.0 |  64.37 μs |  65.15 μs |  0.61 |  1.0986 |  19.43 KB |        0.96 |
| Compiled_Update_Nullable_Full |             .NET 6.0 |  75.14 μs |  75.17 μs |  0.71 |  1.4648 |  23.98 KB |        1.19 |
|                        Update |             .NET 6.0 | 304.34 μs | 304.34 μs |  2.86 |  2.9297 |  52.17 KB |        2.58 |
|                   Update_Full |             .NET 6.0 | 332.37 μs | 332.30 μs |  3.12 |  3.4180 |   57.9 KB |        2.87 |
|               Compiled_Update |             .NET 6.0 |  64.40 μs |  64.36 μs |  0.61 |  1.0986 |  19.41 KB |        0.96 |
|          Compiled_Update_Full |             .NET 6.0 |  77.80 μs |  77.79 μs |  0.73 |  1.4648 |  23.96 KB |        1.19 |
|               Update_Nullable |             .NET 7.0 | 251.93 μs | 251.78 μs |  2.37 |  2.4414 |  42.79 KB |        2.12 |
|          Update_Nullable_Full |             .NET 7.0 | 274.83 μs | 274.81 μs |  2.58 |  2.9297 |  48.29 KB |        2.39 |
|      Compiled_Update_Nullable |             .NET 7.0 |  60.43 μs |  60.44 μs |  0.57 |  1.0986 |  19.36 KB |        0.96 |
| Compiled_Update_Nullable_Full |             .NET 7.0 |  69.17 μs |  69.13 μs |  0.65 |  1.3428 |   23.9 KB |        1.18 |
|                        Update |             .NET 7.0 | 241.77 μs | 241.76 μs |  2.27 |  2.4414 |  42.54 KB |        2.11 |
|                   Update_Full |             .NET 7.0 | 268.44 μs | 270.58 μs |  2.51 |  2.9297 |  48.26 KB |        2.39 |
|               Compiled_Update |             .NET 7.0 |  54.88 μs |  54.64 μs |  0.52 |  1.1597 |  19.35 KB |        0.96 |
|          Compiled_Update_Full |             .NET 7.0 |  69.74 μs |  69.71 μs |  0.66 |  1.3428 |  23.88 KB |        1.18 |
|               Update_Nullable |        .NET Core 3.1 | 383.52 μs | 383.17 μs |  3.60 |  2.9297 |  53.86 KB |        2.67 |
|          Update_Nullable_Full |        .NET Core 3.1 | 413.79 μs | 414.77 μs |  3.88 |  3.4180 |  59.59 KB |        2.95 |
|      Compiled_Update_Nullable |        .NET Core 3.1 |  89.36 μs |  89.62 μs |  0.84 |  1.0986 |  19.35 KB |        0.96 |
| Compiled_Update_Nullable_Full |        .NET Core 3.1 | 102.66 μs | 102.52 μs |  0.97 |  1.4038 |   23.9 KB |        1.18 |
|                        Update |        .NET Core 3.1 | 382.53 μs | 379.47 μs |  3.60 |  2.9297 |  54.03 KB |        2.68 |
|                   Update_Full |        .NET Core 3.1 | 394.74 μs | 403.23 μs |  3.48 |  2.9297 |  59.76 KB |        2.96 |
|               Compiled_Update |        .NET Core 3.1 |  85.97 μs |  85.95 μs |  0.81 |  1.0986 |  19.33 KB |        0.96 |
|          Compiled_Update_Full |        .NET Core 3.1 | 104.07 μs | 104.09 μs |  0.98 |  1.3428 |  23.88 KB |        1.18 |
|               Update_Nullable | .NET Framework 4.7.2 | 534.80 μs | 536.83 μs |  5.03 | 10.2539 |  63.74 KB |        3.16 |
|          Update_Nullable_Full | .NET Framework 4.7.2 | 573.77 μs | 573.75 μs |  5.39 | 10.7422 |     70 KB |        3.47 |
|      Compiled_Update_Nullable | .NET Framework 4.7.2 | 109.49 μs | 109.79 μs |  1.03 |  3.1738 |  20.19 KB |        1.00 |
| Compiled_Update_Nullable_Full | .NET Framework 4.7.2 | 124.71 μs | 124.78 μs |  1.17 |  3.9063 |  25.21 KB |        1.25 |
|                        Update | .NET Framework 4.7.2 | 532.42 μs | 532.33 μs |  5.01 |  9.7656 |  64.11 KB |        3.18 |
|                   Update_Full | .NET Framework 4.7.2 | 565.25 μs | 564.65 μs |  5.31 | 10.7422 |  70.35 KB |        3.49 |
|               Compiled_Update | .NET Framework 4.7.2 | 106.38 μs | 106.37 μs |  1.00 |  3.1738 |  20.19 KB |        1.00 |
|          Compiled_Update_Full | .NET Framework 4.7.2 | 124.33 μs | 124.33 μs |  1.17 |  3.9063 |  25.21 KB |        1.25 |
