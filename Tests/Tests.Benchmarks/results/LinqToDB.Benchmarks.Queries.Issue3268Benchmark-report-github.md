``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-HCNGBR : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-XBFFOD : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-INBZNN : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-THZJXI : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                        Method |              Runtime |      Mean | Allocated |
|------------------------------ |--------------------- |----------:|----------:|
|               Update_Nullable |             .NET 6.0 | 240.80 μs |  47.26 KB |
|          Update_Nullable_Full |             .NET 6.0 | 260.19 μs |  49.84 KB |
|      Compiled_Update_Nullable |             .NET 6.0 |  60.59 μs |  16.42 KB |
| Compiled_Update_Nullable_Full |             .NET 6.0 |  69.03 μs |     19 KB |
|                        Update |             .NET 6.0 | 239.07 μs |  44.88 KB |
|                   Update_Full |             .NET 6.0 | 253.52 μs |  47.46 KB |
|               Compiled_Update |             .NET 6.0 |  54.71 μs |  16.41 KB |
|          Compiled_Update_Full |             .NET 6.0 |  66.94 μs |  18.98 KB |
|               Update_Nullable |             .NET 7.0 | 190.71 μs |  32.48 KB |
|          Update_Nullable_Full |             .NET 7.0 | 207.63 μs |  34.73 KB |
|      Compiled_Update_Nullable |             .NET 7.0 |  61.55 μs |  16.36 KB |
| Compiled_Update_Nullable_Full |             .NET 7.0 |  64.63 μs |  18.92 KB |
|                        Update |             .NET 7.0 | 191.31 μs |  32.07 KB |
|                   Update_Full |             .NET 7.0 | 236.76 μs |  34.85 KB |
|               Compiled_Update |             .NET 7.0 |  49.00 μs |  16.34 KB |
|          Compiled_Update_Full |             .NET 7.0 |  73.78 μs |  18.91 KB |
|               Update_Nullable |        .NET Core 3.1 | 317.40 μs |  48.02 KB |
|          Update_Nullable_Full |        .NET Core 3.1 | 356.76 μs |   50.6 KB |
|      Compiled_Update_Nullable |        .NET Core 3.1 |  80.33 μs |  16.34 KB |
| Compiled_Update_Nullable_Full |        .NET Core 3.1 |  86.74 μs |  18.92 KB |
|                        Update |        .NET Core 3.1 | 308.87 μs |  44.91 KB |
|                   Update_Full |        .NET Core 3.1 | 323.16 μs |  47.49 KB |
|               Compiled_Update |        .NET Core 3.1 |  83.14 μs |  16.33 KB |
|          Compiled_Update_Full |        .NET Core 3.1 |  92.60 μs |  18.91 KB |
|               Update_Nullable | .NET Framework 4.7.2 | 399.11 μs |  48.73 KB |
|          Update_Nullable_Full | .NET Framework 4.7.2 | 439.62 μs |  51.79 KB |
|      Compiled_Update_Nullable | .NET Framework 4.7.2 |  91.90 μs |  16.91 KB |
| Compiled_Update_Nullable_Full | .NET Framework 4.7.2 | 116.12 μs |  19.95 KB |
|                        Update | .NET Framework 4.7.2 | 409.47 μs |   52.1 KB |
|                   Update_Full | .NET Framework 4.7.2 | 431.95 μs |  55.15 KB |
|               Compiled_Update | .NET Framework 4.7.2 |  98.20 μs |  16.91 KB |
|          Compiled_Update_Full | .NET Framework 4.7.2 | 115.92 μs |  19.95 KB |
