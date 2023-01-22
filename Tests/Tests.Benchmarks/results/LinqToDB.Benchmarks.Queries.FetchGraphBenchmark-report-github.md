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
|        Method |              Runtime |     Mean |   Median | Ratio |     Gen0 |     Gen1 | Allocated | Alloc Ratio |
|-------------- |--------------------- |---------:|---------:|------:|---------:|---------:|----------:|------------:|
|          Linq |             .NET 6.0 | 1.658 ms | 1.662 ms |  0.48 |  78.1250 |  39.0625 |   1.26 MB |        1.00 |
|     LinqAsync |             .NET 6.0 | 2.243 ms | 2.212 ms |  0.65 |  78.1250 |  39.0625 |   1.27 MB |        1.00 |
|      Compiled |             .NET 6.0 | 1.613 ms | 1.615 ms |  0.47 |  78.1250 |  39.0625 |   1.26 MB |        0.99 |
| CompiledAsync |             .NET 6.0 | 2.350 ms | 2.360 ms |  0.68 |  78.1250 |  39.0625 |   1.26 MB |        0.99 |
|          Linq |             .NET 7.0 | 1.782 ms | 1.792 ms |  0.52 |  78.1250 |  76.1719 |   1.26 MB |        1.00 |
|     LinqAsync |             .NET 7.0 | 2.162 ms | 2.367 ms |  0.64 |  78.1250 |  74.2188 |   1.26 MB |        1.00 |
|      Compiled |             .NET 7.0 | 1.596 ms | 1.587 ms |  0.46 |  78.1250 |  76.1719 |   1.26 MB |        0.99 |
| CompiledAsync |             .NET 7.0 | 1.939 ms | 2.033 ms |  0.58 |  78.1250 |  74.2188 |   1.26 MB |        0.99 |
|          Linq |        .NET Core 3.1 | 1.797 ms | 1.795 ms |  0.52 |  78.1250 |  39.0625 |   1.26 MB |        1.00 |
|     LinqAsync |        .NET Core 3.1 | 2.787 ms | 2.850 ms |  0.72 |  78.1250 |  39.0625 |   1.27 MB |        1.00 |
|      Compiled |        .NET Core 3.1 | 1.273 ms | 1.573 ms |  0.45 |  78.1250 |  39.0625 |   1.26 MB |        0.99 |
| CompiledAsync |        .NET Core 3.1 | 2.608 ms | 2.604 ms |  0.76 |  78.1250 |  39.0625 |   1.26 MB |        0.99 |
|          Linq | .NET Framework 4.7.2 | 3.286 ms | 3.556 ms |  0.84 | 212.8906 | 105.4688 |   1.28 MB |        1.01 |
|     LinqAsync | .NET Framework 4.7.2 | 4.982 ms | 4.970 ms |  1.45 | 210.9375 | 101.5625 |   1.28 MB |        1.01 |
|      Compiled | .NET Framework 4.7.2 | 3.448 ms | 3.444 ms |  1.00 | 210.9375 | 105.4688 |   1.27 MB |        1.00 |
| CompiledAsync | .NET Framework 4.7.2 | 4.897 ms | 4.939 ms |  1.42 | 210.9375 | 101.5625 |   1.27 MB |        1.00 |
