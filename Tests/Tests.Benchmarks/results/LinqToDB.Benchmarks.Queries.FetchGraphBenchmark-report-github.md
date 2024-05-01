```

BenchmarkDotNet v0.13.12, Windows 10 (10.0.17763.5458/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK 8.0.200
  [Host]     : .NET 8.0.2 (8.0.224.6711), X64 RyuJIT AVX2
  Job-GXDOCB : .NET 6.0.27 (6.0.2724.6912), X64 RyuJIT AVX2
  Job-YDFVLV : .NET 8.0.2 (8.0.224.6711), X64 RyuJIT AVX2
  Job-SBTNYY : .NET Framework 4.8 (4.8.4645.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method        | Runtime              | Mean     | Allocated |
|-------------- |--------------------- |---------:|----------:|
| Linq          | .NET 6.0             | 1.368 ms |   1.26 MB |
| LinqAsync     | .NET 6.0             | 1.364 ms |   1.26 MB |
| Compiled      | .NET 6.0             | 1.411 ms |   1.26 MB |
| CompiledAsync | .NET 6.0             | 1.777 ms |   1.26 MB |
| Linq          | .NET 8.0             | 1.303 ms |   1.26 MB |
| LinqAsync     | .NET 8.0             | 1.506 ms |   1.26 MB |
| Compiled      | .NET 8.0             | 1.120 ms |   1.25 MB |
| CompiledAsync | .NET 8.0             | 1.620 ms |   1.26 MB |
| Linq          | .NET Framework 4.6.2 | 3.538 ms |   1.28 MB |
| LinqAsync     | .NET Framework 4.6.2 | 4.373 ms |   1.28 MB |
| Compiled      | .NET Framework 4.6.2 | 3.112 ms |   1.27 MB |
| CompiledAsync | .NET Framework 4.6.2 | 4.289 ms |   1.27 MB |
