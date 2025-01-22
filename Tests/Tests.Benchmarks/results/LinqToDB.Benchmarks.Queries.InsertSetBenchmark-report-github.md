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
| Method | Runtime              | Mean     | Allocated  |
|------- |--------------------- |---------:|-----------:|
| Test   | .NET 6.0             | 1.467 ms |   835.9 KB |
| Test   | .NET 8.0             | 1.391 ms |   821.1 KB |
| Test   | .NET 9.0             | 1.270 ms |  737.23 KB |
| Test   | .NET Framework 4.6.2 | 3.434 ms | 1141.96 KB |
