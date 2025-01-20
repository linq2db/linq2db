```

BenchmarkDotNet v0.14.0, Windows 10 (10.0.17763.6766/1809/October2018Update/Redstone5) (Hyper-V)
AMD Ryzen 9 5950X, 2 CPU, 32 logical and 16 physical cores
  [Host]     : .NET Framework 4.8 (4.8.4775.0), X64 RyuJIT VectorSize=256
  Job-GEKMDY : .NET 6.0.36 (6.0.3624.51421), X64 RyuJIT AVX2
  Job-WEIMGV : .NET 8.0.12 (8.0.1224.60305), X64 RyuJIT AVX2
  Job-ARZZBJ : .NET 9.0.1 (9.0.124.61010), X64 RyuJIT AVX2
  Job-HBTJES : .NET Framework 4.8 (4.8.4775.0), X64 RyuJIT VectorSize=256

Jit=RyuJit  Platform=X64  

```
| Method        | Runtime              | Mean       | Allocated |
|-------------- |--------------------- |-----------:|----------:|
| Linq          | .NET 6.0             | 1,818.1 μs |   1.26 MB |
| LinqAsync     | .NET 6.0             | 1,997.1 μs |   1.26 MB |
| Compiled      | .NET 6.0             | 1,477.8 μs |   1.26 MB |
| CompiledAsync | .NET 6.0             | 1,885.6 μs |   1.26 MB |
| Linq          | .NET 8.0             | 1,106.8 μs |   1.26 MB |
| LinqAsync     | .NET 8.0             | 1,350.4 μs |   1.26 MB |
| Compiled      | .NET 8.0             | 1,220.4 μs |   1.26 MB |
| CompiledAsync | .NET 8.0             | 1,570.9 μs |   1.26 MB |
| Linq          | .NET 9.0             | 1,281.1 μs |   1.26 MB |
| LinqAsync     | .NET 9.0             | 1,401.3 μs |   1.26 MB |
| Compiled      | .NET 9.0             |   994.4 μs |   1.26 MB |
| CompiledAsync | .NET 9.0             | 1,892.8 μs |   1.26 MB |
| Linq          | .NET Framework 4.6.2 | 3,051.2 μs |   1.28 MB |
| LinqAsync     | .NET Framework 4.6.2 | 4,318.8 μs |   1.28 MB |
| Compiled      | .NET Framework 4.6.2 | 2,847.3 μs |   1.27 MB |
| CompiledAsync | .NET Framework 4.6.2 | 4,241.3 μs |   1.27 MB |
