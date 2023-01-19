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
|  BuildAction |             .NET 6.0 | 1.4194 ns | 1.4122 ns |  1.47 |         - |          NA |
| DirectAccess |             .NET 6.0 | 0.8335 ns | 0.9290 ns |  0.78 |         - |          NA |
|  BuildAction |             .NET 7.0 | 0.9005 ns | 0.8979 ns |  0.92 |         - |          NA |
| DirectAccess |             .NET 7.0 | 0.4292 ns | 0.4497 ns |  0.44 |         - |          NA |
|  BuildAction |        .NET Core 3.1 | 1.3744 ns | 1.3691 ns |  1.41 |         - |          NA |
| DirectAccess |        .NET Core 3.1 | 1.5647 ns | 1.7684 ns |  1.47 |         - |          NA |
|  BuildAction | .NET Framework 4.7.2 | 9.3829 ns | 9.3896 ns |  9.66 |         - |          NA |
| DirectAccess | .NET Framework 4.7.2 | 0.9693 ns | 0.9628 ns |  1.00 |         - |          NA |
