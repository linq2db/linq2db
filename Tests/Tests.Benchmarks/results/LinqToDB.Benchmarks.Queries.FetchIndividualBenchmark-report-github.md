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
| Method    | Runtime              | Mean         | Allocated |
|---------- |--------------------- |-------------:|----------:|
| Linq      | .NET 6.0             |  65,082.9 ns |  12.24 KB |
| Compiled  | .NET 6.0             |  31,959.3 ns |   7.48 KB |
| RawAdoNet | .NET 6.0             |     643.9 ns |   1.48 KB |
| Linq      | .NET 8.0             |  33,087.9 ns |  10.06 KB |
| Compiled  | .NET 8.0             |  17,720.4 ns |   7.47 KB |
| RawAdoNet | .NET 8.0             |     387.6 ns |   1.48 KB |
| Linq      | .NET Framework 4.6.2 | 113,530.0 ns |  15.02 KB |
| Compiled  | .NET Framework 4.6.2 |  51,313.1 ns |   8.92 KB |
| RawAdoNet | .NET Framework 4.6.2 |     946.2 ns |   1.54 KB |
