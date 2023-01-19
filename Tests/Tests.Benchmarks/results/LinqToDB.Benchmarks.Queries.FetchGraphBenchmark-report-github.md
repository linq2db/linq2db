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
|        Method |              Runtime |     Mean |   Median | Ratio |     Gen0 |     Gen1 | Allocated | Alloc Ratio |
|-------------- |--------------------- |---------:|---------:|------:|---------:|---------:|----------:|------------:|
|          Linq |             .NET 6.0 | 1.598 ms | 1.589 ms |  0.47 |  80.0781 |  39.0625 |    1.3 MB |        1.01 |
|     LinqAsync |             .NET 6.0 | 2.453 ms | 2.438 ms |  0.72 |  80.0781 |  39.0625 |    1.3 MB |        1.01 |
|      Compiled |             .NET 6.0 | 1.466 ms | 1.466 ms |  0.43 |  78.1250 |  39.0625 |   1.27 MB |        0.99 |
| CompiledAsync |             .NET 6.0 | 1.895 ms | 1.898 ms |  0.56 |  78.1250 |  39.0625 |   1.28 MB |        0.99 |
|          Linq |             .NET 7.0 | 1.377 ms | 1.380 ms |  0.41 |  80.0781 |  78.1250 |   1.29 MB |        1.00 |
|     LinqAsync |             .NET 7.0 | 2.291 ms | 2.304 ms |  0.68 |  78.1250 |  74.2188 |   1.29 MB |        1.01 |
|      Compiled |             .NET 7.0 | 1.252 ms | 1.263 ms |  0.37 |  78.1250 |  76.1719 |   1.27 MB |        0.99 |
| CompiledAsync |             .NET 7.0 | 1.016 ms | 1.016 ms |  0.30 |  78.1250 |  74.2188 |   1.28 MB |        0.99 |
|          Linq |        .NET Core 3.1 | 1.744 ms | 1.749 ms |  0.51 |  80.0781 |  39.0625 |    1.3 MB |        1.01 |
|     LinqAsync |        .NET Core 3.1 | 2.752 ms | 2.692 ms |  0.81 |  78.1250 |  39.0625 |    1.3 MB |        1.01 |
|      Compiled |        .NET Core 3.1 | 1.385 ms | 1.431 ms |  0.33 |  78.1250 |  39.0625 |   1.28 MB |        0.99 |
| CompiledAsync |        .NET Core 3.1 | 2.525 ms | 2.525 ms |  0.74 |  78.1250 |  39.0625 |   1.28 MB |        0.99 |
|          Linq | .NET Framework 4.7.2 | 3.658 ms | 3.657 ms |  1.08 | 218.7500 | 109.3750 |   1.32 MB |        1.02 |
|     LinqAsync | .NET Framework 4.7.2 | 4.690 ms | 4.688 ms |  1.38 | 218.7500 | 109.3750 |   1.32 MB |        1.03 |
|      Compiled | .NET Framework 4.7.2 | 3.390 ms | 3.389 ms |  1.00 | 210.9375 | 105.4688 |   1.29 MB |        1.00 |
| CompiledAsync | .NET Framework 4.7.2 | 4.139 ms | 4.694 ms |  1.26 | 210.9375 | 105.4688 |   1.29 MB |        1.00 |
