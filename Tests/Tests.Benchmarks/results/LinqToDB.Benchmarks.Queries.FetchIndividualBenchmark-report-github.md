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
| Method    | Runtime              | Mean         | Allocated |
|---------- |--------------------- |-------------:|----------:|
| Linq      | .NET 6.0             |  65,990.7 ns |  16.06 KB |
| Compiled  | .NET 6.0             |  36,075.5 ns |  11.38 KB |
| RawAdoNet | .NET 6.0             |     644.8 ns |   1.48 KB |
| Linq      | .NET 8.0             |  34,470.2 ns |  13.85 KB |
| Compiled  | .NET 8.0             |  20,191.9 ns |  11.34 KB |
| RawAdoNet | .NET 8.0             |     322.5 ns |   1.48 KB |
| Linq      | .NET 9.0             |  29,703.1 ns |  13.86 KB |
| Compiled  | .NET 9.0             |  18,614.7 ns |  11.34 KB |
| RawAdoNet | .NET 9.0             |     398.7 ns |   1.48 KB |
| Linq      | .NET Framework 4.6.2 | 124,552.5 ns |  18.95 KB |
| Compiled  | .NET Framework 4.6.2 |  26,507.1 ns |  13.12 KB |
| RawAdoNet | .NET Framework 4.6.2 |     889.7 ns |   1.54 KB |
