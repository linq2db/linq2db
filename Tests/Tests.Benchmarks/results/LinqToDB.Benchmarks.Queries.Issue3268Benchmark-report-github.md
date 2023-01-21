``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-RNZPMW : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-XCCWXF : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-WSMVMG : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-FMTKFQ : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                        Method |              Runtime |      Mean |    Median | Ratio |   Gen0 | Allocated | Alloc Ratio |
|------------------------------ |--------------------- |----------:|----------:|------:|-------:|----------:|------------:|
|               Update_Nullable |             .NET 6.0 | 310.07 μs | 339.65 μs |  3.11 | 2.4414 |   45.2 KB |        2.67 |
|          Update_Nullable_Full |             .NET 6.0 | 291.26 μs | 302.54 μs |  2.91 | 2.4414 |  47.77 KB |        2.83 |
|      Compiled_Update_Nullable |             .NET 6.0 |  72.46 μs |  77.94 μs |  0.73 | 0.9766 |  16.42 KB |        0.97 |
| Compiled_Update_Nullable_Full |             .NET 6.0 |  80.54 μs |  81.76 μs |  0.80 | 1.0986 |     19 KB |        1.12 |
|                        Update |             .NET 6.0 | 277.24 μs | 291.65 μs |  2.79 | 2.4414 |  45.57 KB |        2.69 |
|                   Update_Full |             .NET 6.0 | 295.50 μs | 314.44 μs |  2.98 | 2.9297 |  48.15 KB |        2.85 |
|               Compiled_Update |             .NET 6.0 |  72.32 μs |  76.45 μs |  0.73 | 0.9766 |  16.41 KB |        0.97 |
|          Compiled_Update_Full |             .NET 6.0 | 115.51 μs | 121.44 μs |  1.16 | 1.0986 |  18.98 KB |        1.12 |
|               Update_Nullable |             .NET 7.0 | 255.03 μs | 279.69 μs |  2.56 | 1.9531 |  32.27 KB |        1.91 |
|          Update_Nullable_Full |             .NET 7.0 | 252.52 μs | 256.67 μs |  2.55 | 1.9531 |  34.51 KB |        2.04 |
|      Compiled_Update_Nullable |             .NET 7.0 |  80.63 μs |  86.74 μs |  0.82 | 0.9766 |  16.36 KB |        0.97 |
| Compiled_Update_Nullable_Full |             .NET 7.0 |  69.24 μs |  68.47 μs |  0.70 | 1.0986 |  18.92 KB |        1.12 |
|                        Update |             .NET 7.0 | 195.78 μs | 203.68 μs |  1.99 | 1.9531 |  32.68 KB |        1.93 |
|                   Update_Full |             .NET 7.0 | 239.58 μs | 252.38 μs |  2.41 | 1.9531 |  34.93 KB |        2.07 |
|               Compiled_Update |             .NET 7.0 |  71.88 μs |  73.02 μs |  0.72 | 0.9766 |  16.34 KB |        0.97 |
|          Compiled_Update_Full |             .NET 7.0 |  77.44 μs |  81.58 μs |  0.78 | 1.0986 |  18.91 KB |        1.12 |
|               Update_Nullable |        .NET Core 3.1 | 376.98 μs | 391.24 μs |  3.78 | 2.4414 |  46.15 KB |        2.73 |
|          Update_Nullable_Full |        .NET Core 3.1 | 435.62 μs | 476.32 μs |  4.35 | 2.9297 |  48.73 KB |        2.88 |
|      Compiled_Update_Nullable |        .NET Core 3.1 | 124.77 μs | 131.74 μs |  1.25 | 0.9766 |  16.34 KB |        0.97 |
| Compiled_Update_Nullable_Full |        .NET Core 3.1 | 120.67 μs | 134.81 μs |  1.22 | 0.9766 |  18.92 KB |        1.12 |
|                        Update |        .NET Core 3.1 | 341.21 μs | 351.68 μs |  3.39 | 2.4414 |  45.09 KB |        2.67 |
|                   Update_Full |        .NET Core 3.1 | 380.21 μs | 396.87 μs |  3.83 | 2.4414 |  47.66 KB |        2.82 |
|               Compiled_Update |        .NET Core 3.1 |  91.84 μs |  97.02 μs |  0.93 | 0.9766 |  16.33 KB |        0.97 |
|          Compiled_Update_Full |        .NET Core 3.1 | 110.41 μs | 114.98 μs |  1.10 | 0.9766 |  18.91 KB |        1.12 |
|               Update_Nullable | .NET Framework 4.7.2 | 443.26 μs | 467.53 μs |  4.45 | 7.8125 |  48.73 KB |        2.88 |
|          Update_Nullable_Full | .NET Framework 4.7.2 | 459.48 μs | 471.35 μs |  4.60 | 8.3008 |  51.79 KB |        3.06 |
|      Compiled_Update_Nullable | .NET Framework 4.7.2 | 103.67 μs | 105.38 μs |  1.03 | 2.6855 |  16.91 KB |        1.00 |
| Compiled_Update_Nullable_Full | .NET Framework 4.7.2 | 131.31 μs | 134.82 μs |  1.32 | 3.1738 |  19.95 KB |        1.18 |
|                        Update | .NET Framework 4.7.2 | 433.84 μs | 445.27 μs |  4.36 | 7.8125 |  49.47 KB |        2.93 |
|                   Update_Full | .NET Framework 4.7.2 | 450.80 μs | 474.41 μs |  4.53 | 8.3008 |  52.52 KB |        3.11 |
|               Compiled_Update | .NET Framework 4.7.2 | 105.51 μs | 105.75 μs |  1.00 | 2.6855 |  16.91 KB |        1.00 |
|          Compiled_Update_Full | .NET Framework 4.7.2 | 132.18 μs | 139.37 μs |  1.32 | 3.1738 |  19.95 KB |        1.18 |
