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
| Method    | Runtime              | Mean        | Allocated |
|---------- |--------------------- |------------:|----------:|
| Linq      | .NET 6.0             | 59,252.1 ns |  11.57 KB |
| Compiled  | .NET 6.0             | 26,437.9 ns |   6.87 KB |
| RawAdoNet | .NET 6.0             |    563.9 ns |   1.48 KB |
| Linq      | .NET 8.0             | 26,506.2 ns |   9.48 KB |
| Compiled  | .NET 8.0             | 10,004.4 ns |   6.85 KB |
| RawAdoNet | .NET 8.0             |    384.4 ns |   1.48 KB |
| Linq      | .NET Framework 4.6.2 | 47,716.0 ns |  14.06 KB |
| Compiled  | .NET Framework 4.6.2 | 45,492.1 ns |   8.31 KB |
| RawAdoNet | .NET Framework 4.6.2 |    886.8 ns |   1.54 KB |
