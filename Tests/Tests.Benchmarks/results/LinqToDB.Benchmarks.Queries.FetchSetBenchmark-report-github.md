``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-XCPGVR : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-RHOQGE : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-WEVYVV : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-ORXRGX : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|    Method |              Runtime |     Mean | Ratio |      Gen0 |     Gen1 |     Gen2 | Allocated | Alloc Ratio |
|---------- |--------------------- |---------:|------:|----------:|---------:|---------:|----------:|------------:|
|      Linq |             .NET 6.0 | 17.75 ms |  0.88 |  562.5000 | 312.5000 |  93.7500 |   7.95 MB |        1.00 |
|  Compiled |             .NET 6.0 | 17.52 ms |  0.88 |  562.5000 | 312.5000 |  93.7500 |   7.95 MB |        1.00 |
| RawAdoNet |             .NET 6.0 | 17.13 ms |  0.86 |  562.5000 | 312.5000 |  93.7500 |   7.94 MB |        1.00 |
|      Linq |             .NET 7.0 | 17.30 ms |  0.87 |  687.5000 | 656.2500 | 218.7500 |   7.95 MB |        1.00 |
|  Compiled |             .NET 7.0 | 18.24 ms |  0.92 |  687.5000 | 656.2500 | 218.7500 |   7.95 MB |        1.00 |
| RawAdoNet |             .NET 7.0 | 16.47 ms |  0.84 |  687.5000 | 656.2500 | 218.7500 |   7.94 MB |        1.00 |
|      Linq |        .NET Core 3.1 | 20.06 ms |  1.00 |  562.5000 | 312.5000 |  93.7500 |   7.95 MB |        1.00 |
|  Compiled |        .NET Core 3.1 | 19.67 ms |  0.98 |  562.5000 | 312.5000 |  93.7500 |   7.95 MB |        1.00 |
| RawAdoNet |        .NET Core 3.1 | 18.56 ms |  0.94 |  562.5000 | 312.5000 |  93.7500 |   7.94 MB |        1.00 |
|      Linq | .NET Framework 4.7.2 | 33.58 ms |  1.67 | 1437.5000 | 625.0000 | 187.5000 |   7.97 MB |        1.00 |
|  Compiled | .NET Framework 4.7.2 | 20.58 ms |  1.02 | 1437.5000 | 593.7500 | 187.5000 |   7.97 MB |        1.00 |
| RawAdoNet | .NET Framework 4.7.2 | 19.90 ms |  1.00 | 1437.5000 | 593.7500 | 187.5000 |   7.96 MB |        1.00 |
