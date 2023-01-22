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
|       Method |              Runtime |       Mean |     Median | Ratio |   Gen0 | Allocated | Alloc Ratio |
|------------- |--------------------- |-----------:|-----------:|------:|-------:|----------:|------------:|
|   TypeMapper |             .NET 6.0 | 41.3076 ns | 41.2981 ns |     ? | 0.0019 |      32 B |           ? |
| DirectAccess |             .NET 6.0 |  0.0178 ns |  0.0006 ns |     ? |      - |         - |           ? |
|   TypeMapper |             .NET 7.0 | 41.1514 ns | 41.1954 ns |     ? | 0.0019 |      32 B |           ? |
| DirectAccess |             .NET 7.0 |  1.3903 ns |  1.3784 ns |     ? |      - |         - |           ? |
|   TypeMapper |        .NET Core 3.1 | 49.8278 ns | 49.7120 ns |     ? | 0.0019 |      32 B |           ? |
| DirectAccess |        .NET Core 3.1 |  0.2679 ns |  0.2795 ns |     ? |      - |         - |           ? |
|   TypeMapper | .NET Framework 4.7.2 | 57.9688 ns | 58.0165 ns |     ? | 0.0050 |      32 B |           ? |
| DirectAccess | .NET Framework 4.7.2 |  0.0549 ns |  0.0548 ns |     ? |      - |         - |           ? |
