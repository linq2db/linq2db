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
| Method    | Runtime              | Mean      | Allocated |
|---------- |--------------------- |----------:|----------:|
| Linq      | .NET 6.0             | 17.054 ms |   7.95 MB |
| Compiled  | .NET 6.0             | 14.762 ms |   7.94 MB |
| RawAdoNet | .NET 6.0             | 14.680 ms |   7.94 MB |
| Linq      | .NET 8.0             | 11.477 ms |   7.94 MB |
| Compiled  | .NET 8.0             | 15.753 ms |   7.94 MB |
| RawAdoNet | .NET 8.0             |  9.461 ms |   7.94 MB |
| Linq      | .NET 9.0             | 15.063 ms |   7.94 MB |
| Compiled  | .NET 9.0             | 12.648 ms |   7.94 MB |
| RawAdoNet | .NET 9.0             |  8.952 ms |   7.94 MB |
| Linq      | .NET Framework 4.6.2 | 33.139 ms |   7.97 MB |
| Compiled  | .NET Framework 4.6.2 | 33.282 ms |   7.97 MB |
| RawAdoNet | .NET Framework 4.6.2 | 18.558 ms |   7.96 MB |
