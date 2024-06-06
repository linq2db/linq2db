```

BenchmarkDotNet v0.13.12, Windows 10 (10.0.17763.5696/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
.NET SDK 8.0.204
  [Host]     : .NET 8.0.4 (8.0.424.16909), X64 RyuJIT AVX2
  Job-VZLGGZ : .NET 6.0.29 (6.0.2924.17105), X64 RyuJIT AVX2
  Job-AZKKUX : .NET 8.0.4 (8.0.424.16909), X64 RyuJIT AVX2
  Job-TQCFWV : .NET Framework 4.8 (4.8.4645.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method        | Runtime              | Mean       | Allocated |
|-------------- |--------------------- |-----------:|----------:|
| Linq          | .NET 6.0             | 1,454.7 μs |   1.26 MB |
| LinqAsync     | .NET 6.0             |   842.8 μs |   1.26 MB |
| Compiled      | .NET 6.0             | 1,416.3 μs |   1.26 MB |
| CompiledAsync | .NET 6.0             | 2,131.2 μs |   1.26 MB |
| Linq          | .NET 8.0             | 1,449.9 μs |   1.26 MB |
| LinqAsync     | .NET 8.0             | 1,496.0 μs |   1.26 MB |
| Compiled      | .NET 8.0             | 1,316.1 μs |   1.26 MB |
| CompiledAsync | .NET 8.0             | 1,309.8 μs |   1.26 MB |
| Linq          | .NET Framework 4.6.2 | 3,228.8 μs |   1.28 MB |
| LinqAsync     | .NET Framework 4.6.2 | 4,338.7 μs |   1.28 MB |
| Compiled      | .NET Framework 4.6.2 | 3,173.9 μs |   1.27 MB |
| CompiledAsync | .NET Framework 4.6.2 | 4,350.7 μs |   1.27 MB |
