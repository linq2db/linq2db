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
| Method       | Runtime              | Mean      | Allocated |
|------------- |--------------------- |----------:|----------:|
| TypeMapper   | .NET 6.0             |  44.83 ns |      24 B |
| DirectAccess | .NET 6.0             |  57.14 ns |      24 B |
| TypeMapper   | .NET 8.0             |  49.27 ns |      24 B |
| DirectAccess | .NET 8.0             |  46.41 ns |      24 B |
| TypeMapper   | .NET 9.0             |  51.71 ns |      24 B |
| DirectAccess | .NET 9.0             |  46.21 ns |      24 B |
| TypeMapper   | .NET Framework 4.6.2 | 146.90 ns |      24 B |
| DirectAccess | .NET Framework 4.6.2 |  77.92 ns |      24 B |
