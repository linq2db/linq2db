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
|  BuildAction |             .NET 6.0 | 1.1630 ns | 1.1911 ns |  1.18 |         - |          NA |
| DirectAccess |             .NET 6.0 | 0.9092 ns | 0.9171 ns |  0.93 |         - |          NA |
|  BuildAction |             .NET 7.0 | 1.3445 ns | 1.2985 ns |  1.38 |         - |          NA |
| DirectAccess |             .NET 7.0 | 0.4839 ns | 0.5020 ns |  0.49 |         - |          NA |
|  BuildAction |        .NET Core 3.1 | 1.4097 ns | 1.3769 ns |  1.43 |         - |          NA |
| DirectAccess |        .NET Core 3.1 | 0.9549 ns | 0.9617 ns |  0.98 |         - |          NA |
|  BuildAction | .NET Framework 4.7.2 | 8.4323 ns | 8.1720 ns |  8.72 |         - |          NA |
| DirectAccess | .NET Framework 4.7.2 | 0.9753 ns | 0.9767 ns |  1.00 |         - |          NA |
