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
|    Method |              Runtime |     Mean |   Median | Ratio |      Gen0 |     Gen1 |     Gen2 | Allocated | Alloc Ratio |
|---------- |--------------------- |---------:|---------:|------:|----------:|---------:|---------:|----------:|------------:|
|      Linq |             .NET 6.0 | 17.25 ms | 17.23 ms |  0.80 |  562.5000 | 312.5000 |  93.7500 |   7.95 MB |        1.00 |
|  Compiled |             .NET 6.0 | 19.64 ms | 20.49 ms |  0.93 |  578.1250 | 343.7500 | 109.3750 |   7.94 MB |        1.00 |
| RawAdoNet |             .NET 6.0 | 18.04 ms | 18.74 ms |  0.83 |  562.5000 | 312.5000 |  93.7500 |   7.94 MB |        1.00 |
|      Linq |             .NET 7.0 | 19.11 ms | 19.21 ms |  0.89 |  687.5000 | 656.2500 | 218.7500 |   7.95 MB |        1.00 |
|  Compiled |             .NET 7.0 | 16.46 ms | 17.10 ms |  0.74 |  687.5000 | 656.2500 | 218.7500 |   7.94 MB |        1.00 |
| RawAdoNet |             .NET 7.0 | 17.30 ms | 17.32 ms |  0.79 |  687.5000 | 656.2500 | 218.7500 |   7.94 MB |        1.00 |
|      Linq |        .NET Core 3.1 | 21.38 ms | 21.29 ms |  0.99 |  562.5000 | 312.5000 |  93.7500 |   7.95 MB |        1.00 |
|  Compiled |        .NET Core 3.1 | 21.20 ms | 21.19 ms |  0.99 |  562.5000 | 312.5000 |  93.7500 |   7.94 MB |        1.00 |
| RawAdoNet |        .NET Core 3.1 | 19.19 ms | 19.17 ms |  0.89 |  562.5000 | 312.5000 |  93.7500 |   7.94 MB |        1.00 |
|      Linq | .NET Framework 4.7.2 | 28.29 ms | 28.60 ms |  1.32 | 1437.5000 | 593.7500 | 187.5000 |   7.97 MB |        1.00 |
|  Compiled | .NET Framework 4.7.2 | 35.07 ms | 35.21 ms |  1.63 | 1437.5000 | 625.0000 | 187.5000 |   7.97 MB |        1.00 |
| RawAdoNet | .NET Framework 4.7.2 | 21.57 ms | 21.62 ms |  1.00 | 1437.5000 | 593.7500 | 187.5000 |   7.96 MB |        1.00 |
