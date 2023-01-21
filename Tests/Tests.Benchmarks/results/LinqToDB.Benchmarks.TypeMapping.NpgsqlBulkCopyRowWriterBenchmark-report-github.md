``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-RNZPMW : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-XCCWXF : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-WSMVMG : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-FMTKFQ : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|       Method |              Runtime |      Mean |    Median | Ratio |   Gen0 | Allocated | Alloc Ratio |
|------------- |--------------------- |----------:|----------:|------:|-------:|----------:|------------:|
|   TypeMapper |             .NET 6.0 |  67.01 ns |  69.67 ns |  0.72 | 0.0014 |      24 B |        1.00 |
| DirectAccess |             .NET 6.0 |  65.60 ns |  69.02 ns |  0.70 | 0.0014 |      24 B |        1.00 |
|   TypeMapper |             .NET 7.0 |  56.44 ns |  61.27 ns |  0.60 | 0.0014 |      24 B |        1.00 |
| DirectAccess |             .NET 7.0 |  59.78 ns |  63.30 ns |  0.63 | 0.0014 |      24 B |        1.00 |
|   TypeMapper |        .NET Core 3.1 |  64.52 ns |  67.44 ns |  0.69 | 0.0014 |      24 B |        1.00 |
| DirectAccess |        .NET Core 3.1 |  70.35 ns |  72.14 ns |  0.76 | 0.0014 |      24 B |        1.00 |
|   TypeMapper | .NET Framework 4.7.2 | 165.98 ns | 179.51 ns |  1.78 | 0.0038 |      24 B |        1.00 |
| DirectAccess | .NET Framework 4.7.2 |  97.08 ns | 100.29 ns |  1.00 | 0.0038 |      24 B |        1.00 |
