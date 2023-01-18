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
|       Method |              Runtime |      Mean |    Median | Ratio | Allocated | Alloc Ratio |
|------------- |--------------------- |----------:|----------:|------:|----------:|------------:|
|  BuildAction |             .NET 6.0 | 1.4202 ns | 1.3700 ns |  1.55 |         - |          NA |
| DirectAccess |             .NET 6.0 | 0.6649 ns | 0.9133 ns |  0.77 |         - |          NA |
|  BuildAction |             .NET 7.0 | 1.2352 ns | 1.2898 ns |  1.30 |         - |          NA |
| DirectAccess |             .NET 7.0 | 0.4402 ns | 0.4085 ns |  0.48 |         - |          NA |
|  BuildAction |        .NET Core 3.1 | 1.3676 ns | 1.3559 ns |  1.50 |         - |          NA |
| DirectAccess |        .NET Core 3.1 | 0.9146 ns | 0.8917 ns |  0.98 |         - |          NA |
|  BuildAction | .NET Framework 4.7.2 | 9.3055 ns | 9.2830 ns | 10.18 |         - |          NA |
| DirectAccess | .NET Framework 4.7.2 | 0.9182 ns | 0.9144 ns |  1.00 |         - |          NA |
