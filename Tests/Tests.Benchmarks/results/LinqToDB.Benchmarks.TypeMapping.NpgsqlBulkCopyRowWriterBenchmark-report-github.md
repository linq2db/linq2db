``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 10 (10.0.17763.3650/1809/October2018Update/Redstone5), VM=Hyper-V
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.102
  [Host]     : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-HCNGBR : .NET 6.0.13 (6.0.1322.58009), X64 RyuJIT AVX2
  Job-XBFFOD : .NET 7.0.2 (7.0.222.60605), X64 RyuJIT AVX2
  Job-INBZNN : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-THZJXI : .NET Framework 4.8 (4.8.4515.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
|       Method |              Runtime |      Mean | Allocated |
|------------- |--------------------- |----------:|----------:|
|   TypeMapper |             .NET 6.0 |  58.54 ns |      24 B |
| DirectAccess |             .NET 6.0 |  66.55 ns |      24 B |
|   TypeMapper |             .NET 7.0 |  55.43 ns |      24 B |
| DirectAccess |             .NET 7.0 |  55.35 ns |      24 B |
|   TypeMapper |        .NET Core 3.1 |  62.22 ns |      24 B |
| DirectAccess |        .NET Core 3.1 |  64.17 ns |      24 B |
|   TypeMapper | .NET Framework 4.7.2 | 148.81 ns |      24 B |
| DirectAccess | .NET Framework 4.7.2 |  91.50 ns |      24 B |
