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
|        Method |              Runtime |     Mean |   Median | Ratio |     Gen0 |     Gen1 | Allocated | Alloc Ratio |
|-------------- |--------------------- |---------:|---------:|------:|---------:|---------:|----------:|------------:|
|          Linq |             .NET 6.0 | 1.453 ms | 1.416 ms |  0.40 |  78.1250 |  39.0625 |   1.27 MB |        1.00 |
|     LinqAsync |             .NET 6.0 | 1.605 ms | 1.127 ms |  0.48 |  78.1250 |  39.0625 |   1.27 MB |        1.00 |
|      Compiled |             .NET 6.0 | 1.457 ms | 1.460 ms |  0.42 |  78.1250 |  39.0625 |   1.26 MB |        0.99 |
| CompiledAsync |             .NET 6.0 | 1.608 ms | 1.866 ms |  0.55 |  78.1250 |  39.0625 |   1.26 MB |        0.99 |
|          Linq |             .NET 7.0 | 1.319 ms | 1.310 ms |  0.38 |  78.1250 |  70.3125 |   1.26 MB |        1.00 |
|     LinqAsync |             .NET 7.0 | 2.101 ms | 2.120 ms |  0.60 |  78.1250 |  70.3125 |   1.27 MB |        1.00 |
|      Compiled |             .NET 7.0 | 1.260 ms | 1.266 ms |  0.36 |  78.1250 |  76.1719 |   1.26 MB |        0.99 |
| CompiledAsync |             .NET 7.0 | 1.502 ms | 1.846 ms |  0.46 |  78.1250 |  73.2422 |   1.26 MB |        0.99 |
|          Linq |        .NET Core 3.1 | 1.481 ms | 1.460 ms |  0.41 |  78.1250 |  39.0625 |   1.27 MB |        1.00 |
|     LinqAsync |        .NET Core 3.1 | 2.634 ms | 2.622 ms |  0.75 |  78.1250 |  39.0625 |   1.27 MB |        1.00 |
|      Compiled |        .NET Core 3.1 | 1.634 ms | 1.629 ms |  0.47 |  78.1250 |  39.0625 |   1.26 MB |        0.99 |
| CompiledAsync |        .NET Core 3.1 | 2.293 ms | 2.755 ms |  0.59 |  78.1250 |  39.0625 |   1.26 MB |        0.99 |
|          Linq | .NET Framework 4.7.2 | 3.529 ms | 3.627 ms |  0.98 | 210.9375 | 105.4688 |   1.28 MB |        1.01 |
|     LinqAsync | .NET Framework 4.7.2 | 4.817 ms | 4.812 ms |  1.38 | 210.9375 | 101.5625 |   1.28 MB |        1.01 |
|      Compiled | .NET Framework 4.7.2 | 3.604 ms | 3.618 ms |  1.00 | 210.9375 | 105.4688 |   1.27 MB |        1.00 |
| CompiledAsync | .NET Framework 4.7.2 | 4.258 ms | 4.213 ms |  1.22 | 210.9375 | 101.5625 |   1.27 MB |        1.00 |
