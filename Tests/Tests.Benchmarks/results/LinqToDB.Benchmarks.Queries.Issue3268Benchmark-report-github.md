``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-TEPEZT : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-ISYUTK : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-SMHCKK : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-DHDWVI : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                        Method |              Runtime |      Mean |    Median | Ratio |   Gen0 | Allocated | Alloc Ratio |
|------------------------------ |--------------------- |----------:|----------:|------:|-------:|----------:|------------:|
|               Update_Nullable |             .NET 6.0 | 118.13 μs | 118.00 μs |  1.69 | 2.6855 |   47.6 KB |        2.82 |
|          Update_Nullable_Full |             .NET 6.0 | 278.71 μs | 279.14 μs |  3.91 | 2.9297 |  50.18 KB |        2.97 |
|      Compiled_Update_Nullable |             .NET 6.0 |  64.35 μs |  64.78 μs |  0.90 | 0.9766 |  16.42 KB |        0.97 |
| Compiled_Update_Nullable_Full |             .NET 6.0 |  76.06 μs |  75.74 μs |  1.05 | 1.1597 |     19 KB |        1.12 |
|                        Update |             .NET 6.0 | 246.79 μs | 247.14 μs |  3.41 | 2.4414 |  44.54 KB |        2.63 |
|                   Update_Full |             .NET 6.0 | 280.76 μs | 279.69 μs |  3.78 | 2.4414 |  47.12 KB |        2.79 |
|               Compiled_Update |             .NET 6.0 |  59.81 μs |  63.60 μs |  0.74 | 0.9766 |  16.41 KB |        0.97 |
|          Compiled_Update_Full |             .NET 6.0 |  72.93 μs |  73.32 μs |  1.01 | 1.0986 |  18.98 KB |        1.12 |
|               Update_Nullable |             .NET 7.0 | 195.98 μs | 196.37 μs |  2.57 | 1.9531 |  31.95 KB |        1.89 |
|          Update_Nullable_Full |             .NET 7.0 | 215.23 μs | 213.94 μs |  2.97 | 1.9531 |  34.51 KB |        2.04 |
|      Compiled_Update_Nullable |             .NET 7.0 |  60.07 μs |  60.22 μs |  0.83 | 0.9766 |  16.36 KB |        0.97 |
| Compiled_Update_Nullable_Full |             .NET 7.0 |  69.02 μs |  68.50 μs |  0.91 | 1.0986 |  18.92 KB |        1.12 |
|                        Update |             .NET 7.0 | 192.92 μs | 191.11 μs |  2.53 | 1.9531 |  32.32 KB |        1.91 |
|                   Update_Full |             .NET 7.0 |  97.54 μs |  97.69 μs |  1.43 | 1.9531 |   35.4 KB |        2.09 |
|               Compiled_Update |             .NET 7.0 |  52.07 μs |  58.30 μs |  0.65 | 0.9766 |  16.34 KB |        0.97 |
|          Compiled_Update_Full |             .NET 7.0 |  67.85 μs |  68.62 μs |  0.87 | 1.0986 |  18.91 KB |        1.12 |
|               Update_Nullable |        .NET Core 3.1 | 320.05 μs | 320.43 μs |  4.42 | 2.9297 |  48.02 KB |        2.84 |
|          Update_Nullable_Full |        .NET Core 3.1 | 348.00 μs | 349.35 μs |  4.88 | 2.9297 |   50.6 KB |        2.99 |
|      Compiled_Update_Nullable |        .NET Core 3.1 |  85.21 μs |  85.26 μs |  1.18 | 0.9766 |  16.34 KB |        0.97 |
| Compiled_Update_Nullable_Full |        .NET Core 3.1 |  96.69 μs |  96.92 μs |  1.34 | 1.0986 |  18.92 KB |        1.12 |
|                        Update |        .NET Core 3.1 | 308.95 μs | 309.90 μs |  4.26 | 2.4414 |  44.73 KB |        2.65 |
|                   Update_Full |        .NET Core 3.1 | 224.39 μs | 153.18 μs |  2.84 | 2.6855 |   47.3 KB |        2.80 |
|               Compiled_Update |        .NET Core 3.1 |  83.82 μs |  84.06 μs |  1.16 | 0.9766 |  16.33 KB |        0.97 |
|          Compiled_Update_Full |        .NET Core 3.1 |  84.84 μs |  96.38 μs |  1.06 | 1.0986 |  18.91 KB |        1.12 |
|               Update_Nullable | .NET Framework 4.7.2 | 399.35 μs | 398.66 μs |  5.50 | 7.8125 |  49.12 KB |        2.90 |
|          Update_Nullable_Full | .NET Framework 4.7.2 | 427.98 μs | 427.66 μs |  6.15 | 8.3008 |  52.16 KB |        3.08 |
|      Compiled_Update_Nullable | .NET Framework 4.7.2 |  96.79 μs |  98.10 μs |  1.31 | 2.6855 |  16.91 KB |        1.00 |
| Compiled_Update_Nullable_Full | .NET Framework 4.7.2 | 113.40 μs | 113.23 μs |  1.56 | 3.1738 |  19.95 KB |        1.18 |
|                        Update | .NET Framework 4.7.2 | 395.31 μs | 394.43 μs |  5.44 | 7.8125 |  49.84 KB |        2.95 |
|                   Update_Full | .NET Framework 4.7.2 | 429.54 μs | 429.12 μs |  5.93 | 8.3008 |  52.89 KB |        3.13 |
|               Compiled_Update | .NET Framework 4.7.2 |  85.67 μs |  95.55 μs |  1.00 | 2.6855 |  16.91 KB |        1.00 |
|          Compiled_Update_Full | .NET Framework 4.7.2 | 112.61 μs | 112.65 μs |  1.55 | 3.1738 |  19.95 KB |        1.18 |
