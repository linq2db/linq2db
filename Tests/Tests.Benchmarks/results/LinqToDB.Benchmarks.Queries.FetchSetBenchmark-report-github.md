``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-UZBSVL : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-AYZXIO : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-NXXYQT : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-HMCTKM : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|    Method |              Runtime |     Mean |   Median | Ratio |      Gen0 |     Gen1 |     Gen2 | Allocated | Alloc Ratio |
|---------- |--------------------- |---------:|---------:|------:|----------:|---------:|---------:|----------:|------------:|
|      Linq |             .NET 6.0 | 17.85 ms | 17.81 ms |  0.90 |  562.5000 | 312.5000 |  93.7500 |   7.97 MB |        1.00 |
|  Compiled |             .NET 6.0 | 17.11 ms | 17.17 ms |  0.86 |  562.5000 | 312.5000 |  93.7500 |   7.96 MB |        1.00 |
| RawAdoNet |             .NET 6.0 | 17.59 ms | 17.60 ms |  0.89 |  562.5000 | 312.5000 |  93.7500 |   7.94 MB |        1.00 |
|      Linq |             .NET 7.0 | 18.51 ms | 18.49 ms |  0.94 |  687.5000 | 656.2500 | 218.7500 |   7.97 MB |        1.00 |
|  Compiled |             .NET 7.0 | 18.21 ms | 18.75 ms |  0.85 |  687.5000 | 656.2500 | 218.7500 |   7.96 MB |        1.00 |
| RawAdoNet |             .NET 7.0 | 14.99 ms | 15.13 ms |  0.76 |  687.5000 | 656.2500 | 218.7500 |   7.94 MB |        1.00 |
|      Linq |        .NET Core 3.1 | 19.59 ms | 19.81 ms |  0.99 |  562.5000 | 312.5000 |  93.7500 |   7.97 MB |        1.00 |
|  Compiled |        .NET Core 3.1 | 19.34 ms | 19.31 ms |  0.98 |  562.5000 | 312.5000 |  93.7500 |   7.97 MB |        1.00 |
| RawAdoNet |        .NET Core 3.1 | 19.08 ms | 19.22 ms |  0.96 |  562.5000 | 312.5000 |  93.7500 |   7.94 MB |        1.00 |
|      Linq | .NET Framework 4.7.2 | 33.49 ms | 33.42 ms |  1.69 | 1437.5000 | 625.0000 | 187.5000 |   7.99 MB |        1.00 |
|  Compiled | .NET Framework 4.7.2 | 33.20 ms | 33.18 ms |  1.68 | 1437.5000 | 625.0000 | 187.5000 |   7.99 MB |        1.00 |
| RawAdoNet | .NET Framework 4.7.2 | 19.79 ms | 19.75 ms |  1.00 | 1437.5000 | 593.7500 | 187.5000 |   7.96 MB |        1.00 |
