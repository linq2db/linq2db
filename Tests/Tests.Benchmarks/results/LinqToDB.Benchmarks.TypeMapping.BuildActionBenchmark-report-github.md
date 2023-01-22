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
|       Method |              Runtime |      Mean | Ratio | Allocated | Alloc Ratio |
|------------- |--------------------- |----------:|------:|----------:|------------:|
|  BuildAction |             .NET 6.0 | 1.3014 ns |  1.44 |         - |          NA |
| DirectAccess |             .NET 6.0 | 0.9335 ns |  1.02 |         - |          NA |
|  BuildAction |             .NET 7.0 | 0.8828 ns |  0.98 |         - |          NA |
| DirectAccess |             .NET 7.0 | 0.4382 ns |  0.46 |         - |          NA |
|  BuildAction |        .NET Core 3.1 | 1.1092 ns |  1.20 |         - |          NA |
| DirectAccess |        .NET Core 3.1 | 0.9202 ns |  1.01 |         - |          NA |
|  BuildAction | .NET Framework 4.7.2 | 9.3268 ns | 10.22 |         - |          NA |
| DirectAccess | .NET Framework 4.7.2 | 0.9165 ns |  1.00 |         - |          NA |
