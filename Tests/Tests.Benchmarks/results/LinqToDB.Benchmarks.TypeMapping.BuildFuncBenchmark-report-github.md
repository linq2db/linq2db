``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-WUZRIO : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-EMBONI : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-HZWTXS : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-VIGHHX : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|       Method |              Runtime |      Mean |    Median | Ratio | Allocated | Alloc Ratio |
|------------- |--------------------- |----------:|----------:|------:|----------:|------------:|
|    BuildFunc |             .NET 6.0 |  3.865 ns |  3.672 ns |  1.38 |         - |          NA |
| DirectAccess |             .NET 6.0 |  2.819 ns |  2.744 ns |  1.00 |         - |          NA |
|    BuildFunc |             .NET 7.0 |  4.987 ns |  4.962 ns |  1.75 |         - |          NA |
| DirectAccess |             .NET 7.0 |  3.768 ns |  3.665 ns |  1.34 |         - |          NA |
|    BuildFunc |        .NET Core 3.1 |  3.309 ns |  5.516 ns |  1.49 |         - |          NA |
| DirectAccess |        .NET Core 3.1 |  2.871 ns |  2.779 ns |  1.02 |         - |          NA |
|    BuildFunc | .NET Framework 4.7.2 | 11.401 ns | 11.315 ns |  4.11 |         - |          NA |
| DirectAccess | .NET Framework 4.7.2 |  2.828 ns |  2.884 ns |  1.00 |         - |          NA |
