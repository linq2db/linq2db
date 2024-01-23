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
| Method        | Runtime              | Mean     | Allocated |
|-------------- |--------------------- |---------:|----------:|
| Linq          | .NET 6.0             | 1.020 ms |   1.26 MB |
| LinqAsync     | .NET 6.0             | 2.120 ms |   1.27 MB |
| Compiled      | .NET 6.0             | 1.459 ms |   1.25 MB |
| CompiledAsync | .NET 6.0             | 2.044 ms |   1.26 MB |
| Linq          | .NET 7.0             | 1.572 ms |   1.26 MB |
| LinqAsync     | .NET 7.0             | 1.978 ms |   1.26 MB |
| Compiled      | .NET 7.0             | 1.051 ms |   1.25 MB |
| CompiledAsync | .NET 7.0             | 1.877 ms |   1.26 MB |
| Linq          | .NET Core 3.1        | 1.287 ms |   1.26 MB |
| LinqAsync     | .NET Core 3.1        | 2.319 ms |   1.26 MB |
| Compiled      | .NET Core 3.1        | 1.452 ms |   1.25 MB |
| CompiledAsync | .NET Core 3.1        | 2.483 ms |   1.26 MB |
| Linq          | .NET Framework 4.7.2 | 3.186 ms |   1.28 MB |
| LinqAsync     | .NET Framework 4.7.2 | 2.129 ms |   1.28 MB |
| Compiled      | .NET Framework 4.7.2 | 2.958 ms |   1.27 MB |
| CompiledAsync | .NET Framework 4.7.2 | 4.386 ms |   1.27 MB |
