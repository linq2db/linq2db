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
|       Method |              Runtime |      Mean |    Median | Ratio | Allocated | Alloc Ratio |
|------------- |--------------------- |----------:|----------:|------:|----------:|------------:|
|    BuildFunc |             .NET 6.0 |  3.599 ns |  3.605 ns |  1.27 |         - |          NA |
| DirectAccess |             .NET 6.0 |  2.889 ns |  2.914 ns |  1.02 |         - |          NA |
|    BuildFunc |             .NET 7.0 |  5.083 ns |  5.340 ns |  1.38 |         - |          NA |
| DirectAccess |             .NET 7.0 |  3.044 ns |  3.174 ns |  1.19 |         - |          NA |
|    BuildFunc |        .NET Core 3.1 |  5.663 ns |  5.768 ns |  1.98 |         - |          NA |
| DirectAccess |        .NET Core 3.1 |  2.362 ns |  2.231 ns |  0.84 |         - |          NA |
|    BuildFunc | .NET Framework 4.7.2 | 13.410 ns | 13.419 ns |  4.74 |         - |          NA |
| DirectAccess | .NET Framework 4.7.2 |  2.837 ns |  2.880 ns |  1.00 |         - |          NA |
