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
| TypeMapper   | .NET 6.0             |  60.54 ns |      24 B |
| DirectAccess | .NET 6.0             |  64.94 ns |      24 B |
| TypeMapper   | .NET 7.0             |  54.17 ns |      24 B |
| DirectAccess | .NET 7.0             |  53.86 ns |      24 B |
| TypeMapper   | .NET Core 3.1        |  54.78 ns |      24 B |
| DirectAccess | .NET Core 3.1        |  57.82 ns |      24 B |
| TypeMapper   | .NET Framework 4.7.2 | 146.92 ns |      24 B |
| DirectAccess | .NET Framework 4.7.2 |  87.54 ns |      24 B |
