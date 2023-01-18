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
|       Method |              Runtime |      Mean |     Median | Ratio | Allocated | Alloc Ratio |
|------------- |--------------------- |----------:|-----------:|------:|----------:|------------:|
|    BuildFunc |             .NET 6.0 |  4.110 ns |  4.1099 ns |  1.45 |         - |          NA |
| DirectAccess |             .NET 6.0 |  2.907 ns |  2.9046 ns |  1.03 |         - |          NA |
|    BuildFunc |             .NET 7.0 |  5.734 ns |  5.7337 ns |  2.02 |         - |          NA |
| DirectAccess |             .NET 7.0 |  3.656 ns |  3.6562 ns |  1.30 |         - |          NA |
|    BuildFunc |        .NET Core 3.1 |  5.687 ns |  5.7531 ns |  2.01 |         - |          NA |
| DirectAccess |        .NET Core 3.1 |  1.197 ns |  0.0000 ns |  0.70 |         - |          NA |
|    BuildFunc | .NET Framework 4.7.2 | 12.728 ns | 12.7281 ns |  4.47 |         - |          NA |
| DirectAccess | .NET Framework 4.7.2 |  2.835 ns |  2.9291 ns |  1.00 |         - |          NA |
