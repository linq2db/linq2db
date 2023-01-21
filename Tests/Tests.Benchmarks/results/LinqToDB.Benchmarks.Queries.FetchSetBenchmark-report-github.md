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
|    Method |              Runtime |     Mean | Ratio |      Gen0 |     Gen1 |     Gen2 | Allocated | Alloc Ratio |
|---------- |--------------------- |---------:|------:|----------:|---------:|---------:|----------:|------------:|
|      Linq |             .NET 6.0 | 29.81 ms |  1.03 |  562.5000 | 312.5000 |  93.7500 |   7.95 MB |        1.00 |
|  Compiled |             .NET 6.0 | 32.30 ms |  1.12 |  562.5000 | 312.5000 |  93.7500 |   7.94 MB |        1.00 |
| RawAdoNet |             .NET 6.0 | 30.07 ms |  1.05 |  562.5000 | 312.5000 |  93.7500 |   7.94 MB |        1.00 |
|      Linq |             .NET 7.0 | 26.25 ms |  0.91 |  687.5000 | 656.2500 | 218.7500 |   7.95 MB |        1.00 |
|  Compiled |             .NET 7.0 | 25.83 ms |  0.90 |  687.5000 | 656.2500 | 218.7500 |   7.94 MB |        1.00 |
| RawAdoNet |             .NET 7.0 | 21.93 ms |  0.76 |  687.5000 | 656.2500 | 218.7500 |   7.94 MB |        1.00 |
|      Linq |        .NET Core 3.1 | 26.65 ms |  0.93 |  562.5000 | 312.5000 |  93.7500 |   7.95 MB |        1.00 |
|  Compiled |        .NET Core 3.1 | 31.06 ms |  1.08 |  562.5000 | 312.5000 |  93.7500 |   7.94 MB |        1.00 |
| RawAdoNet |        .NET Core 3.1 | 31.33 ms |  1.09 |  562.5000 | 312.5000 |  93.7500 |   7.94 MB |        1.00 |
|      Linq | .NET Framework 4.7.2 | 41.44 ms |  1.44 | 1416.6667 | 583.3333 | 166.6667 |   7.97 MB |        1.00 |
|  Compiled | .NET Framework 4.7.2 | 44.74 ms |  1.55 | 1437.5000 | 625.0000 | 187.5000 |   7.97 MB |        1.00 |
| RawAdoNet | .NET Framework 4.7.2 | 29.11 ms |  1.00 | 1437.5000 | 593.7500 | 187.5000 |   7.96 MB |        1.00 |
