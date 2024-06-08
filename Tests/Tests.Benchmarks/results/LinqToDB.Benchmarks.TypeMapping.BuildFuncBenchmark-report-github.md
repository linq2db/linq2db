```

BenchmarkDotNet v0.13.9+228a464e8be6c580ad9408e98f18813f6407fb5a, Windows 10 (10.0.17763.4644/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK 7.0.401
  [Host]     : .NET 7.0.11 (7.0.1123.42427), X64 RyuJIT AVX2
  Job-DAXXNM : .NET 6.0.22 (6.0.2223.42425), X64 RyuJIT AVX2
  Job-SLTPYD : .NET 7.0.11 (7.0.1123.42427), X64 RyuJIT AVX2
  Job-YOWJJJ : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-OZLLFF : .NET Framework 4.8 (4.8.4645.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method       | Runtime              | Mean      | Allocated |
|------------- |--------------------- |----------:|----------:|
| BuildFunc    | .NET 6.0             |  4.111 ns |         - |
| DirectAccess | .NET 6.0             |  2.139 ns |         - |
| BuildFunc    | .NET 7.0             |  5.082 ns |         - |
| DirectAccess | .NET 7.0             |  3.129 ns |         - |
| BuildFunc    | .NET Core 3.1        |  5.005 ns |         - |
| DirectAccess | .NET Core 3.1        |  1.719 ns |         - |
| BuildFunc    | .NET Framework 4.7.2 | 12.787 ns |         - |
| DirectAccess | .NET Framework 4.7.2 |  2.885 ns |         - |
