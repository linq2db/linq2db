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
|       Method |              Runtime |      Mean | Ratio |   Gen0 | Allocated | Alloc Ratio |
|------------- |--------------------- |----------:|------:|-------:|----------:|------------:|
|   TypeMapper |             .NET 6.0 |  57.56 ns |  0.64 | 0.0014 |      24 B |        1.00 |
| DirectAccess |             .NET 6.0 |  67.12 ns |  0.75 | 0.0014 |      24 B |        1.00 |
|   TypeMapper |             .NET 7.0 |  55.75 ns |  0.62 | 0.0014 |      24 B |        1.00 |
| DirectAccess |             .NET 7.0 |  55.68 ns |  0.62 | 0.0014 |      24 B |        1.00 |
|   TypeMapper |        .NET Core 3.1 |  55.90 ns |  0.63 | 0.0014 |      24 B |        1.00 |
| DirectAccess |        .NET Core 3.1 |  55.39 ns |  0.62 | 0.0014 |      24 B |        1.00 |
|   TypeMapper | .NET Framework 4.7.2 | 152.93 ns |  1.71 | 0.0038 |      24 B |        1.00 |
| DirectAccess | .NET Framework 4.7.2 |  89.52 ns |  1.00 | 0.0038 |      24 B |        1.00 |
