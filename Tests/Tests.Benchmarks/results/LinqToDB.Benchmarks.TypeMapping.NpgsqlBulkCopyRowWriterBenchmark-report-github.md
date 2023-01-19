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
|       Method |              Runtime |      Mean |    Median | Ratio |   Gen0 | Allocated | Alloc Ratio |
|------------- |--------------------- |----------:|----------:|------:|-------:|----------:|------------:|
|   TypeMapper |             .NET 6.0 |  56.93 ns |  57.27 ns |  0.65 | 0.0014 |      24 B |        1.00 |
| DirectAccess |             .NET 6.0 |  67.17 ns |  66.45 ns |  0.77 | 0.0014 |      24 B |        1.00 |
|   TypeMapper |             .NET 7.0 |  54.21 ns |  54.43 ns |  0.62 | 0.0014 |      24 B |        1.00 |
| DirectAccess |             .NET 7.0 |  45.32 ns |  54.54 ns |  0.44 | 0.0014 |      24 B |        1.00 |
|   TypeMapper |        .NET Core 3.1 |  58.40 ns |  58.43 ns |  0.67 | 0.0014 |      24 B |        1.00 |
| DirectAccess |        .NET Core 3.1 |  55.74 ns |  55.77 ns |  0.64 | 0.0014 |      24 B |        1.00 |
|   TypeMapper | .NET Framework 4.7.2 | 137.94 ns | 137.35 ns |  1.57 | 0.0038 |      24 B |        1.00 |
| DirectAccess | .NET Framework 4.7.2 |  87.59 ns |  87.57 ns |  1.00 | 0.0038 |      24 B |        1.00 |
