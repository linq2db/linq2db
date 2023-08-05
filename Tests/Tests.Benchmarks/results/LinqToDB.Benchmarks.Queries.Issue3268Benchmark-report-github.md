``` ini

BenchmarkDotNet=v0.13.5, OS=Windows 10 (10.0.17763.4010/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.201
  [Host]     : .NET 7.0.3 (7.0.323.6910), X64 RyuJIT AVX2
  Job-ZOLDKB : .NET 6.0.14 (6.0.1423.7309), X64 RyuJIT AVX2
  Job-EHWHZK : .NET 7.0.3 (7.0.323.6910), X64 RyuJIT AVX2
  Job-LWJRKG : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-AGOWOF : .NET Framework 4.8 (4.8.4614.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|                        Method |              Runtime |      Mean | Allocated |
|------------------------------ |--------------------- |----------:|----------:|
|               Update_Nullable |             .NET 6.0 | 252.65 μs |  45.21 KB |
|          Update_Nullable_Full |             .NET 6.0 | 273.07 μs |   47.9 KB |
|      Compiled_Update_Nullable |             .NET 6.0 |  59.12 μs |  16.44 KB |
| Compiled_Update_Nullable_Full |             .NET 6.0 |  72.81 μs |  19.13 KB |
|                        Update |             .NET 6.0 | 242.94 μs |  43.59 KB |
|                   Update_Full |             .NET 6.0 | 266.82 μs |  46.27 KB |
|               Compiled_Update |             .NET 6.0 |  63.01 μs |  16.42 KB |
|          Compiled_Update_Full |             .NET 6.0 |  73.56 μs |  19.11 KB |
|               Update_Nullable |             .NET 7.0 | 198.09 μs |   32.2 KB |
|          Update_Nullable_Full |             .NET 7.0 | 216.72 μs |  35.17 KB |
|      Compiled_Update_Nullable |             .NET 7.0 |  58.63 μs |  16.38 KB |
| Compiled_Update_Nullable_Full |             .NET 7.0 |  71.39 μs |  19.05 KB |
|                        Update |             .NET 7.0 | 195.78 μs |  32.09 KB |
|                   Update_Full |             .NET 7.0 | 197.03 μs |  34.76 KB |
|               Compiled_Update |             .NET 7.0 |  27.36 μs |  16.36 KB |
|          Compiled_Update_Full |             .NET 7.0 |  66.77 μs |  19.03 KB |
|               Update_Nullable |        .NET Core 3.1 | 325.04 μs |  44.95 KB |
|          Update_Nullable_Full |        .NET Core 3.1 | 303.93 μs |  47.63 KB |
|      Compiled_Update_Nullable |        .NET Core 3.1 |  82.66 μs |  16.36 KB |
| Compiled_Update_Nullable_Full |        .NET Core 3.1 |  96.71 μs |  19.05 KB |
|                        Update |        .NET Core 3.1 | 314.28 μs |  46.82 KB |
|                   Update_Full |        .NET Core 3.1 | 312.87 μs |  49.51 KB |
|               Compiled_Update |        .NET Core 3.1 |  83.92 μs |  16.34 KB |
|          Compiled_Update_Full |        .NET Core 3.1 |  96.28 μs |  19.03 KB |
|               Update_Nullable | .NET Framework 4.7.2 | 405.15 μs |  51.02 KB |
|          Update_Nullable_Full | .NET Framework 4.7.2 | 391.91 μs |  54.15 KB |
|      Compiled_Update_Nullable | .NET Framework 4.7.2 |  95.63 μs |  16.93 KB |
| Compiled_Update_Nullable_Full | .NET Framework 4.7.2 | 111.03 μs |  20.06 KB |
|                        Update | .NET Framework 4.7.2 | 390.43 μs |  49.12 KB |
|                   Update_Full | .NET Framework 4.7.2 | 421.86 μs |  52.25 KB |
|               Compiled_Update | .NET Framework 4.7.2 |  91.37 μs |  16.93 KB |
|          Compiled_Update_Full | .NET Framework 4.7.2 | 112.86 μs |  20.06 KB |
