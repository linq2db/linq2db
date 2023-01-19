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
|       Method |              Runtime |      Mean |    Median | Ratio | Allocated | Alloc Ratio |
|------------- |--------------------- |----------:|----------:|------:|----------:|------------:|
|    BuildFunc |             .NET 6.0 |  3.747 ns |  4.467 ns |  1.19 |         - |          NA |
| DirectAccess |             .NET 6.0 |  2.853 ns |  2.844 ns |  0.98 |         - |          NA |
|    BuildFunc |             .NET 7.0 |  4.630 ns |  4.467 ns |  1.58 |         - |          NA |
| DirectAccess |             .NET 7.0 |  3.817 ns |  3.834 ns |  1.31 |         - |          NA |
|    BuildFunc |        .NET Core 3.1 |  5.696 ns |  5.737 ns |  1.95 |         - |          NA |
| DirectAccess |        .NET Core 3.1 |  2.918 ns |  2.953 ns |  1.00 |         - |          NA |
|    BuildFunc | .NET Framework 4.7.2 | 13.175 ns | 13.263 ns |  4.51 |         - |          NA |
| DirectAccess | .NET Framework 4.7.2 |  2.928 ns |  2.926 ns |  1.00 |         - |          NA |
