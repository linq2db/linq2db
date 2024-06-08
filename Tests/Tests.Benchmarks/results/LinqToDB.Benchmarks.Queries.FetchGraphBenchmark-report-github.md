```

BenchmarkDotNet v0.13.9+228a464e8be6c580ad9408e98f18813f6407fb5a, Windows 10 (10.0.17763.5328/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK 8.0.101
  [Host]     : .NET 7.0.15 (7.0.1523.57226), X64 RyuJIT AVX2
  Job-KJWIMT : .NET 6.0.26 (6.0.2623.60508), X64 RyuJIT AVX2
  Job-GULBRG : .NET 7.0.15 (7.0.1523.57226), X64 RyuJIT AVX2
  Job-LRGNRQ : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  Job-SJROSW : .NET Framework 4.8 (4.8.4645.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method        | Runtime              | Mean     | Allocated |
|-------------- |--------------------- |---------:|----------:|
| Linq          | .NET 6.0             | 1.523 ms |   1.26 MB |
| LinqAsync     | .NET 6.0             | 2.464 ms |   1.27 MB |
| Compiled      | .NET 6.0             | 1.484 ms |   1.26 MB |
| CompiledAsync | .NET 6.0             | 2.208 ms |   1.26 MB |
| Linq          | .NET 7.0             | 1.235 ms |   1.26 MB |
| LinqAsync     | .NET 7.0             | 2.374 ms |   1.26 MB |
| Compiled      | .NET 7.0             | 1.452 ms |   1.26 MB |
| CompiledAsync | .NET 7.0             | 2.238 ms |   1.26 MB |
| Linq          | .NET Core 3.1        | 1.520 ms |   1.26 MB |
| LinqAsync     | .NET Core 3.1        | 2.605 ms |   1.27 MB |
| Compiled      | .NET Core 3.1        | 1.428 ms |   1.26 MB |
| CompiledAsync | .NET Core 3.1        | 1.886 ms |   1.26 MB |
| Linq          | .NET Framework 4.7.2 | 3.209 ms |   1.28 MB |
| LinqAsync     | .NET Framework 4.7.2 | 4.574 ms |   1.28 MB |
| Compiled      | .NET Framework 4.7.2 | 3.241 ms |   1.27 MB |
| CompiledAsync | .NET Framework 4.7.2 | 4.718 ms |   1.27 MB |
